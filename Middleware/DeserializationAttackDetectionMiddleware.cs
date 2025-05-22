using System;
using System.IO;
using System.Web;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Security;
using System.Security.Policy;
using System.Security.Permissions;
using System.Reflection;

namespace Demoweb.Middleware
{
    public class DeserializationAttackDetectionMiddleware : IHttpModule
    {
        private AppDomain _sandboxDomain;

        public void Init(HttpApplication context)
        {
            context.BeginRequest += (sender, e) => CheckRequest(context);
        }

        public void Dispose()
        {
            if (_sandboxDomain != null && _sandboxDomain != AppDomain.CurrentDomain)
            {
                AppDomain.Unload(_sandboxDomain);
            }
        }

        private void CheckRequest(HttpApplication context)
        {
            var request = context.Request;

            // Chỉ xử lý các yêu cầu POST gửi đến Login hoặc Signup
            if (request.HttpMethod == "POST" &&
                (request.Url.AbsolutePath.EndsWith("Login", StringComparison.OrdinalIgnoreCase) ||
                 request.Url.AbsolutePath.EndsWith("Signup", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    // Đọc dữ liệu thô từ request
                    string jsonPayload;
                    request.InputStream.Position = 0;
                    using (var reader = new StreamReader(request.InputStream))
                    {
                        jsonPayload = reader.ReadToEnd();
                    }

                    // Log payload thô để debug
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Raw Payload: {jsonPayload}");

                    if (!string.IsNullOrEmpty(jsonPayload))
                    {
                        // Thử parse thành JSON
                        JObject jsonObject;
                        try
                        {
                            jsonObject = JObject.Parse(jsonPayload);
                        }
                        catch (JsonException ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] JSON Parse Error: {ex.Message}");
                            context.Context.Items["SanitizedPayload"] = jsonPayload;
                            return;
                        }

                        // Kiểm tra $type (dấu hiệu deserialization nguy hiểm)
                        if (jsonObject.Descendants().Any(token => token.Type == JTokenType.Property && token.Path.Contains("$type")))
                        {
                            LogSuspiciousActivity("Phát hiện payload chứa $type: " + jsonPayload);
                            context.Response.StatusCode = 400;
                            context.Response.ContentType = "application/json";
                            context.Response.Write(JsonConvert.SerializeObject(new { success = false, message = "Phát hiện payload độc hại. Yêu cầu bị chặn (type)." }));
                            context.Response.Flush();
                            context.CompleteRequest();
                            return;
                        }

                        // Kiểm tra chuỗi base64 dài trong username hoặc password
                        JToken usernameToken = jsonObject["username"] ?? jsonObject["Username"];
                        JToken passwordToken = jsonObject["password"] ?? jsonObject["Password"];

                        if (usernameToken != null && IsPotentialBase64Attack(usernameToken.ToString()) ||
                            passwordToken != null && IsPotentialBase64Attack(passwordToken.ToString()))
                        {
                            LogSuspiciousActivity("Phát hiện chuỗi base64 dài hoặc nguy hiểm: " + jsonPayload);
                            string sandboxResult = ProcessInSandbox(jsonObject.ToString());
                            context.Response.StatusCode = 400;
                            context.Response.ContentType = "application/json";
                            context.Response.Write(JsonConvert.SerializeObject(new { success = false, message = "Phát hiện payload không hợp lệ. Yêu cầu bị chặn (base64).", sandboxResult = sandboxResult }));
                            context.Response.Flush();
                            context.CompleteRequest();
                            return;
                        }

                        // Nếu không có vấn đề, lưu payload đã kiểm tra
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Payload hợp lệ, tiếp tục xử lý.");
                        context.Context.Items["SanitizedPayload"] = jsonPayload;
                    }
                }
                catch (Exception ex)
                {
                    LogSuspiciousActivity($"Lỗi khi kiểm tra yêu cầu: {ex.Message}");
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "application/json";
                    context.Response.Write(JsonConvert.SerializeObject(new { success = false, message = $"Lỗi xử lý yêu cầu: {ex.Message}" }));
                    context.Response.Flush();
                    context.CompleteRequest();
                }
            }
        }

        private bool IsPotentialBase64Attack(string input)
        {
            if (string.IsNullOrEmpty(input) || input.Length <= 100) return false; //ngưỡng

            // Kiểm tra định dạng base64
            if (!System.Text.RegularExpressions.Regex.IsMatch(input, @"^[A-Za-z0-9\+/]+={0,2}$"))
            {
                System.Diagnostics.Debug.WriteLine("Base64 check failed: Invalid characters");
                return false;
            }

            try
            {
                Convert.FromBase64String(input);
                System.Diagnostics.Debug.WriteLine("Base64 check passed: Valid base64 string with length > 100");
                return true; // Chuỗi base64 dài được coi là nguy hiểm
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Base64 check failed: " + ex.Message);
                return false;
            }
        }

        private string ProcessInSandbox(string payload)
        {
            if (_sandboxDomain == null)
            {
                var setup = new AppDomainSetup
                {
                    ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                    PrivateBinPath = AppDomain.CurrentDomain.BaseDirectory,
                    DisallowBindingRedirects = false, // Cho phép tải assembly cần thiết
                    DisallowCodeDownload = true
                };

                var permissions = new PermissionSet(PermissionState.None);
                permissions.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
                permissions.AddPermission(new ReflectionPermission(ReflectionPermissionFlag.RestrictedMemberAccess));
                permissions.AddPermission(new FileIOPermission(FileIOPermissionAccess.Read, AppDomain.CurrentDomain.BaseDirectory)); // Quyền đọc file cơ bản

                _sandboxDomain = AppDomain.CreateDomain("SandboxDomain", null, setup, permissions);
            }

            try
            {
                var sandbox = (SandboxProxy)_sandboxDomain.CreateInstanceAndUnwrap(
                    Assembly.GetExecutingAssembly().FullName,
                    typeof(SandboxProxy).FullName);

                return sandbox.AnalyzeAndBlock(payload);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] Sandbox Error: {ex.Message}");
                return $"Sandbox failed: {ex.Message}";
            }
        }

        private void LogSuspiciousActivity(string message)
        {
            Console.WriteLine($"[{DateTime.Now}] Suspicious Activity: {message}");
        }
    }

    internal class SandboxProxy : MarshalByRefObject
    {
        public string AnalyzeAndBlock(string payload)
        {
            try
            {
                JObject jsonObject = JObject.Parse(payload);
                JToken usernameToken = jsonObject["username"] ?? jsonObject["Username"];
                if (usernameToken != null && usernameToken.Type == JTokenType.String)
                {
                    string base64String = usernameToken.ToString();
                    if (IsBase64(base64String))
                    {
                        byte[] bytes = Convert.FromBase64String(base64String);
                        using (var ms = new MemoryStream(bytes))
                        {
                            var bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                            object deserializedObject = null;
                            try
                            {
                                deserializedObject = bf.Deserialize(ms);
                                Console.WriteLine($"[{DateTime.Now}] Sandbox: Payload deserialized to {deserializedObject?.GetType().FullName}");

                                // Kiểm tra nếu đối tượng chứa hành vi nguy hiểm (ví dụ: Process.Start)
                                if (deserializedObject != null && CheckForDangerousMethods(deserializedObject))
                                {
                                    Console.WriteLine($"[{DateTime.Now}] Sandbox: Detected potential RCE with method calls");
                                    return "Deserialized successfully, but blocked due to potential RCE (e.g., Process.Start)";
                                }
                                return $"Deserialized successfully to {deserializedObject?.GetType().FullName}, no dangerous methods detected";
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[{DateTime.Now}] Sandbox Deserialization Error: {ex.Message}");
                                return $"Deserialization failed: {ex.Message}";
                            }
                        }
                    }
                }
                return "No deserialization performed";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] Sandbox Exception: {ex.Message}");
                return $"Analysis failed: {ex.Message}";
            }
        }

        private bool IsBase64(string base64)
        {
            if (string.IsNullOrEmpty(base64) || base64.Length % 4 != 0)
            {
                return false;
            }
            try
            {
                Convert.FromBase64String(base64);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool CheckForDangerousMethods(object obj)
        {
            if (obj == null) return false;

            Type type = obj.GetType();
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                if (method.DeclaringType == typeof(System.Diagnostics.Process) &&
                    (method.Name == "Start" || method.Name == "Kill"))
                {
                    Console.WriteLine($"[{DateTime.Now}] Sandbox: Detected dangerous method {method.Name} in {type.FullName}");
                    return true;
                }
            }
            return false;
        }
    }
}