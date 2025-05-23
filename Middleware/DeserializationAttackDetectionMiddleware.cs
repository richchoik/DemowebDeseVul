using System;
using System.IO;
using System.Web;
using Newtonsoft.Json.Linq;
using System.Linq;
using Newtonsoft.Json;
using System.Security;
using System.Security.Policy;
using System.Security.Permissions;
using System.Reflection;

namespace Demoweb.Middleware
{
    // public class DeserializationAttackDetectionMiddleware : IHttpModule
    // {
    //     private AppDomain _sandboxDomain;
    //
    //     public void Init(HttpApplication context)
    //     {
    //         context.BeginRequest += (sender, e) => CheckRequest(context);
    //         Console.WriteLine($"[{DateTime.Now}] Middleware initialized");
    //     }
    //
    //     public void Dispose()
    //     {
    //         if (_sandboxDomain != null && _sandboxDomain != AppDomain.CurrentDomain)
    //         {
    //             AppDomain.Unload(_sandboxDomain);
    //         }
    //     }
    //
    //     private void CheckRequest(HttpApplication context)
    //     {
    //         Console.WriteLine($"[{DateTime.Now}] Checking request: {context.Request.Url}");
    //         var request = context.Request;
    //
    //         // Chỉ xử lý các yêu cầu POST gửi đến Login, Signup, hoặc TestSandbox
    //         if (request.HttpMethod == "POST" &&
    //             (request.Url.AbsolutePath.EndsWith("Login", StringComparison.OrdinalIgnoreCase) ||
    //              request.Url.AbsolutePath.EndsWith("Signup", StringComparison.OrdinalIgnoreCase) ||
    //              request.Url.AbsolutePath.EndsWith("TestSandbox", StringComparison.OrdinalIgnoreCase)))
    //         {
    //             try
    //             {
    //                 // Đọc dữ liệu thô từ request
    //                 string jsonPayload;
    //                 request.InputStream.Position = 0;
    //                 using (var reader = new StreamReader(request.InputStream))
    //                 {
    //                     jsonPayload = reader.ReadToEnd();
    //                 }
    //
    //                 Console.WriteLine($"[{DateTime.Now}] Raw Payload: {jsonPayload}");
    //
    //                 if (!string.IsNullOrEmpty(jsonPayload))
    //                 {
    //                     // Thử parse thành JSON
    //                     JObject jsonObject;
    //                     try
    //                     {
    //                         jsonObject = JObject.Parse(jsonPayload);
    //                     }
    //                     catch (JsonException ex)
    //                     {
    //                         Console.WriteLine($"[{DateTime.Now}] JSON Parse Error: {ex.Message}");
    //                         context.Response.StatusCode = 400;
    //                         context.Response.ContentType = "application/json";
    //                         context.Response.Write(JsonConvert.SerializeObject(new { success = false, message = "Invalid JSON format" }));
    //                         context.Response.Flush();
    //                         context.CompleteRequest();
    //                         return;
    //                     }
    //
    //                     // Kiểm tra $type ngay từ đầu
    //                     if (jsonObject.Descendants().Any(token => token.Type == JTokenType.Property && token.Path.Contains("$type")))
    //                     {
    //                         LogSuspiciousActivity("Phát hiện $type trong payload: " + jsonPayload);
    //                         context.Response.StatusCode = 400;
    //                         context.Response.ContentType = "application/json";
    //                         context.Response.Write(JsonConvert.SerializeObject(new { success = false, message = "Phát hiện payload nguy hiểm. Yêu cầu bị chặn (type).", sandboxResult = "Initial $type detection" }));
    //                         context.Response.Flush();
    //                         context.CompleteRequest();
    //                         return;
    //                     }
    //
    //                     // Đưa payload vào sandbox để phân tích
    //                     string sandboxResult = ProcessInSandbox(jsonObject.ToString());
    //                     Console.WriteLine($"[{DateTime.Now}] Sandbox Result: {sandboxResult}");
    //
    //                     // Chỉ chặn nếu sandbox phát hiện $type, RCE, hoặc lỗi deserialization rõ ràng
    //                     if (sandboxResult.Contains("potential RCE") || sandboxResult.Contains("$type detected") || sandboxResult.Contains("Deserialization failed"))
    //                     {
    //                         LogSuspiciousActivity("Phát hiện payload nguy hiểm: " + jsonPayload);
    //                         context.Response.StatusCode = 400;
    //                         context.Response.ContentType = "application/json";
    //                         context.Response.Write(JsonConvert.SerializeObject(new { success = false, message = "Phát hiện payload nguy hiểm. Yêu cầu bị chặn.", sandboxResult = sandboxResult }));
    //                         context.Response.Flush();
    //                         context.CompleteRequest();
    //                         return;
    //                     }
    //                     else if (sandboxResult.Contains("Sandbox failed"))
    //                     {
    //                         Console.WriteLine($"[{DateTime.Now}] Sandbox failed but no deserialization threat detected, continuing with caution: {jsonPayload}");
    //                         context.Context.Items["SanitizedPayload"] = jsonPayload; // Tiếp tục nếu chỉ lỗi tải assembly
    //                     }
    //                     else
    //                     {
    //                         Console.WriteLine($"[{DateTime.Now}] Payload an toàn, tiếp tục xử lý.");
    //                         context.Context.Items["SanitizedPayload"] = jsonPayload;
    //                     }
    //                 }
    //             }
    //             catch (Exception ex)
    //             {
    //                 LogSuspiciousActivity($"Lỗi khi kiểm tra yêu cầu: {ex.Message}");
    //                 context.Response.StatusCode = 500;
    //                 context.Response.ContentType = "application/json";
    //                 context.Response.Write(JsonConvert.SerializeObject(new { success = false, message = $"Lỗi xử lý yêu cầu: {ex.Message}" }));
    //                 context.Response.Flush();
    //                 context.CompleteRequest();
    //             }
    //         }
    //     }
    //
    //     private string ProcessInSandbox(string payload)
    //     {
    //         if (_sandboxDomain == null)
    //         {
    //             var currentDomain = AppDomain.CurrentDomain;
    //             var setup = new AppDomainSetup
    //             {
    //                 ApplicationBase = currentDomain.BaseDirectory,
    //                 PrivateBinPath = currentDomain.BaseDirectory,
    //                 DisallowBindingRedirects = false,
    //                 DisallowCodeDownload = true,
    //                 ConfigurationFile = currentDomain.SetupInformation.ConfigurationFile
    //             };
    //
    //             var permissions = new PermissionSet(PermissionState.None);
    //             permissions.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
    //             permissions.AddPermission(new ReflectionPermission(ReflectionPermissionFlag.RestrictedMemberAccess));
    //             permissions.AddPermission(new FileIOPermission(FileIOPermissionAccess.Read | FileIOPermissionAccess.PathDiscovery, currentDomain.BaseDirectory));
    //
    //             _sandboxDomain = AppDomain.CreateDomain("SandboxDomain", null, setup, permissions);
    //             Console.WriteLine($"[{DateTime.Now}] Sandbox domain created with base: {currentDomain.BaseDirectory}");
    //         }
    //
    //         try
    //         {
    //             var sandbox = (SandboxProxy)_sandboxDomain.CreateInstanceAndUnwrap(
    //                 Assembly.GetExecutingAssembly().GetName().Name,
    //                 typeof(SandboxProxy).FullName);
    //
    //             return sandbox.AnalyzeAndBlock(payload);
    //         }
    //         catch (Exception ex)
    //         {
    //             Console.WriteLine($"[{DateTime.Now}] Sandbox Error: {ex.Message}");
    //             return $"Sandbox failed: {ex.Message}";
    //         }
    //     }
    //
    //     private void LogSuspiciousActivity(string message)
    //     {
    //         Console.WriteLine($"[{DateTime.Now}] Suspicious Activity: {message}");
    //     }
    // }

    public class SandboxProxy : MarshalByRefObject
    {
        public string AnalyzeAndBlock(string payload)
        {
            try
            {
                JObject jsonObject = JObject.Parse(payload);
                JToken usernameToken = jsonObject["username"] ?? jsonObject["Username"];
                JToken passwordToken = jsonObject["password"] ?? jsonObject["Password"];

                // Kiểm tra $type trong toàn bộ payload
                if (jsonObject.Descendants().Any(token => token.Type == JTokenType.Property && token.Path.Contains("$type")))
                {
                    Console.WriteLine($"[{DateTime.Now}] Sandbox: $type detected in payload");
                    return "$type detected, potential deserialization attack";
                }

                // Kiểm tra username và password
                if (usernameToken != null && usernameToken.Type == JTokenType.String && IsBase64(usernameToken.ToString()))
                {
                    string base64String = usernameToken.ToString();
                    byte[] bytes = Convert.FromBase64String(base64String);
                    using (var ms = new MemoryStream(bytes))
                    {
                        var bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                        object deserializedObject = null;
                        try
                        {
                            deserializedObject = bf.Deserialize(ms);
                            Console.WriteLine($"[{DateTime.Now}] Sandbox: Payload deserialized to {deserializedObject?.GetType().FullName}");

                            // Kiểm tra hành vi nguy hiểm
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
                else if (passwordToken != null && passwordToken.Type == JTokenType.String && IsBase64(passwordToken.ToString()))
                {
                    string base64String = passwordToken.ToString();
                    byte[] bytes = Convert.FromBase64String(base64String);
                    using (var ms = new MemoryStream(bytes))
                    {
                        var bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                        object deserializedObject = null;
                        try
                        {
                            deserializedObject = bf.Deserialize(ms);
                            Console.WriteLine($"[{DateTime.Now}] Sandbox: Payload deserialized to {deserializedObject?.GetType().FullName}");

                            // Kiểm tra hành vi nguy hiểm
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
                return "No deserialization performed, payload appears safe";
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