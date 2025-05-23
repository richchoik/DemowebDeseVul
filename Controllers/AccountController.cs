﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Security.Permissions;
using System.Web.Mvc;
using Demoweb.Middleware;
using Demoweb.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Demoweb.Controllers
{
    public class AccountController : Controller
    {
        //dùng tạm
        private static List<User> users = new List<User>
        {
            new User { Id = 1, Username = "admin", Password = "admin123" }
        };

        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        public class LoginModel
        {
            public dynamic Username { get; set; }
            public dynamic Password { get; set; }
        }

        [HttpPost]
        public ActionResult Login(string unused = null)
        {
            try
            {
                // Ưu tiên đọc từ SanitizedPayload nếu có
                string jsonPayload = HttpContext.Items["SanitizedPayload"] as string;
                if (string.IsNullOrEmpty(jsonPayload))
                {
                    // Fallback đọc từ Request.InputStream nếu không có SanitizedPayload
                    Request.InputStream.Position = 0;
                    using (var reader = new StreamReader(Request.InputStream))
                    {
                        jsonPayload = reader.ReadToEnd();
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Raw JSON: {jsonPayload}");

                // Kiểm tra xem jsonPayload có rỗng không
                if (string.IsNullOrEmpty(jsonPayload))
                {
                    return Json(new { success = false, message = "No data received" });
                }

                // Parse JSON để kiểm tra cấu trúc
                JObject jsonObject = JObject.Parse(jsonPayload);
                JToken usernameToken = jsonObject["Username"] ?? jsonObject["username"];
                JToken passwordToken = jsonObject["Password"] ?? jsonObject["password"];

                System.Diagnostics.Debug.WriteLine("usernameToken: " + usernameToken?.ToString());
                System.Diagnostics.Debug.WriteLine("passwordToken: " + passwordToken?.ToString());

                string username = null;
                string password = null;

                if (usernameToken != null && usernameToken.Type == JTokenType.Object && usernameToken["$type"] != null)
                {
                    // Nếu Username là object và chứa $type, deserialize để kích hoạt RCE
                    object dangerousObject = JsonConvert.DeserializeObject<object>(usernameToken.ToString(), new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All
                    });
                    System.Diagnostics.Debug.WriteLine("Deserialized dangerous object: " + dangerousObject);
                }
                else
                {
                    // Nếu không, lấy Username như chuỗi bình thường
                    username = usernameToken?.ToString();
                }

                password = passwordToken?.ToString();

                System.Diagnostics.Debug.WriteLine("username after processing: " + username);
                System.Diagnostics.Debug.WriteLine("password after processing: " + password);

                // Nếu không có username hoặc password, trả về lỗi
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    return Json(new { success = false, message = "Invalid input" });
                }

                // Logic đăng nhập bình thường
                var user = users.FirstOrDefault(u => u.Username == username && u.Password == password);
                if (user != null)
                {
                    Session["UserId"] = user.Id;
                    Session["Username"] = user.Username;
                    return Json(new { success = true, message = "Login successful" });
                }
                return Json(new { success = false, message = "Invalid credentials" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Login Error: {ex.Message}");
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public ActionResult Signup()
        {
            return View();
        }

        public class SignupModel
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        [HttpPost]
        public ActionResult Signup(string unused = null)
        {
            try
            {
                // Ưu tiên đọc từ SanitizedPayload nếu có
                string jsonPayload = HttpContext.Items["SanitizedPayload"] as string;
                if (string.IsNullOrEmpty(jsonPayload))
                {
                    // Fallback đọc từ Request.InputStream nếu không có SanitizedPayload
                    Request.InputStream.Position = 0;
                    using (var reader = new StreamReader(Request.InputStream))
                    {
                        jsonPayload = reader.ReadToEnd();
                    }
                }

                Console.WriteLine($"[{DateTime.Now}] Raw JSON received: {jsonPayload}");

                if (string.IsNullOrEmpty(jsonPayload))
                {
                    System.Diagnostics.Debug.WriteLine("No data received in request");
                    return Json(new { success = false, message = "No data received" });
                }

                // Parse JSON
                JObject jsonObject;
                try
                {
                    jsonObject = JObject.Parse(jsonPayload);
                }
                catch (JsonException ex)
                {
                    System.Diagnostics.Debug.WriteLine("JSON parse error: " + ex.Message);
                    return Json(new { success = false, message = "Invalid JSON format" });
                }

                JToken usernameToken = jsonObject["Username"] ?? jsonObject["username"];
                JToken passwordToken = jsonObject["Password"] ?? jsonObject["password"];

                System.Diagnostics.Debug.WriteLine("usernameToken: " + usernameToken?.ToString());
                System.Diagnostics.Debug.WriteLine("passwordToken: " + passwordToken?.ToString());

                string username = null;
                string password = null;

                if (usernameToken != null && usernameToken.Type == JTokenType.String)
                {
                    string usernameStr = usernameToken.ToString();
                    Console.WriteLine("usernameStr extracted: " + usernameStr);

                    // Chỉ chấp nhận username là chuỗi văn bản đơn giản
                    if (IsBase64(usernameStr) && usernameStr.Length > 100) //ngưỡng
                    {
                        return Json(new { success = false, message = "Username contains invalid base64 data" });
                    }
                    username = usernameStr;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("usernameToken is null or not a string");
                }

                password = passwordToken?.ToString();
                Console.WriteLine("Password extracted: " + password);

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    System.Diagnostics.Debug.WriteLine("Username or password is empty");
                    return Json(new { success = false, message = "Invalid input" });
                }

                if (users.Any(u => u.Username == username))
                {
                    System.Diagnostics.Debug.WriteLine("Username already exists: " + username);
                    return Json(new { success = false, message = "Username already exists" });
                }

                var newUser = new User
                {
                    Id = users.Max(u => u.Id) + 1,
                    Username = username,
                    Password = password
                };
                users.Add(newUser);
                Console.WriteLine("New user added: " + username + " with ID: " + newUser.Id);
                return Json(new { success = true, message = "Signup successful" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Signup Error: {ex.Message}");
                return Json(new { success = false, message = "Error: " + ex.Message });
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

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }
        
        [HttpPost]
        public ActionResult TestSandbox()
        {
            try
            {
                // Đọc payload từ request
                string jsonPayload;
                Request.InputStream.Position = 0;
                using (var reader = new StreamReader(Request.InputStream))
                {
                    jsonPayload = reader.ReadToEnd();
                }

                Console.WriteLine($"[{DateTime.Now}] TestSandbox Raw Payload: {jsonPayload}");

                if (string.IsNullOrEmpty(jsonPayload))
                {
                    Console.WriteLine("No data received in test");
                    return Json(new { success = false, message = "No data received" });
                }

                // Parse JSON
                JObject jsonObject;
                try
                {
                    jsonObject = JObject.Parse(jsonPayload);
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] TestSandbox JSON Parse Error: {ex.Message}");
                    return Json(new { success = false, message = "Invalid JSON format" });
                }

                // Thêm payload kiểm tra (ví dụ: base64 với Process.Start)
                string testPayload = @"{
                    ""username"": ""AAEAAAD/////AQAAAAAAAAAMAgAAAElTeXN0ZW0sIFZlcnNpb249NC4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1iNzdhNWM1NjE5MzRlMDg5BQEAAACEAVN5c3RlbS5Db2xsZWN0aW9ucy5HZW5lcmljLlNvcnRlZFNldGAxW1tTeXN0ZW0uU3RyaW5nLCBtc2NvcmxpYiwgVmVyc2lvbj00LjAuMC4wLCBDdWx0dXJlPW5ldXRyYWwsIFB1YmxpY0tleVRva2VuPWI3N2E1YzU2MTkzNGUwODldXQQAAAAFQ291bnQIQ29tcGFyZXIHVmVyc2lvbgVJdGVtcwADAAYIjQFTeXN0ZW0uQ29sbGVjdGlvbnMuR2VuZXJpYy5Db21wYXJpc29uQ29tcGFyZXJgMVtbU3lzdGVtLlN0cmluZywgbXNjb3JsaWIsIFZlcnNpb249NC4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1iNzdhNWM1NjE5MzRlMDg5XV0IAgAAAAIAAAAJAwAAAAIAAAAJBAAAAAQDAAAAjQFTeXN0ZW0uQ29sbGVjdGlvbnMuR2VuZXJpYy5Db21wYXJpc29uQ29tcGFyZXJgMVtbU3lzdGVtLlN0cmluZywgbXNjb3JsaWIsIFZlcnNpb249NC4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1iNzdhNWM1NjE5MzRlMDg5XV0BAAAAC19jb21wYXJpc29uAyJTeXN0ZW0uRGVsZWdhdGVTZXJpYWxpemF0aW9uSG9sZGVyCQUAAAARBAAAAAIAAAAGBgAAAAsvYyBub3RlLml4ZQYFAAAAA2NtZAQFAAAAIlN5c3RlbS5EZWxlZ2F0ZVNlcmlhbGl6YXRpb25Ib2xkZXIDAAAACERlbGVnYXRlB21ldGhvZDAHbWV0aG9kMQMDAzBTeXN0ZW0uRGVsZWdhdGVTZXJpYWxpemF0aW9uSG9sZGVyK0RlbGVnYXRlRW50cnkvU3lzdGVtLlJlZmxlY3Rpb24uTWVtYmVySW5mb1NlcmlhbGl6YXRpb25Ib2xkZXIvU3lzdGVtLlJlZmxlY3Rpb24uTWVtYmVySW5mb1NlcmlhbGl6YXRpb25Ib2xkZXIJCAAAAAkJAAAACQoAAAAECAAAADBTeXN0ZW0uRGVsZWdhdGVTZXJpYWxpemF0aW9uSG9sZGVyK0RlbGVnYXRlRW50cnkHAAAABHR5cGUIYXNzZW1ibHkGdGFyZ2V0EnRhcmdldFR5cGVBc3NlbWJseQ50YXJnZXRUeXBlTmFtZQptZXRob2ROYW1lDWRlbGVnYXRlRW50cnkBAQIBAQEDMFN5c3RlbS5EZWxlZ2F0ZVNlcmlhbGl6YXRpb25Ib2xkZXIrRGVsZWdhdGVFbnRyeQYLAAAAsAJTeXN0ZW0uRnVuY2AzW1tTeXN0ZW0uU3RyaW5nLCBtc2NvcmxpYiwgVmVyc2lvbj00LjAuMC4wLCBDdWx0dXJlPW5ldXRyYWwsIFB1YmxpY0tleVRva2VuPWI3N2E1YzU2MTkzNGUwODldLFtTeXN0ZW0uU3RyaW5nLCBtc2NvcmxpYiwgVmVyc2lvbj00LjAuMC4wLCBDdWx0dXJlPW5ldXRyYWwsIFB1YmxpY0tleVRva2VuPWI3N2E1YzU2MTkzNGUwODldLFtTeXN0ZW0uRGlhZ25vc3RpY3MuUHJvY2VzcywgU3lzdGVtLCBWZXJzaW9uPTQuMC4wLjAsIEN1bHR1cmU9bmV1dHJhbCwgUHVibGljS2V5VG9rZW49Yjc3YTVjNTYxOTM0ZTA4OV1dBgwAAABLbXNjb3JsaWIsIFZlcnNpb249NC4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1iNzdhNWM1NjE5MzRlMDg5CgYNAAAASVN5c3RlbSwgVmVyc2lvbj00LjAuMC4wLCBDdWx0dXJlPW5ldXRyYWwsIFB1YmxpY0tleVRva2tlbj1iNzdhNWM1NjE5MzRlMDg5GDgAAABpTeXN0ZW0uRGlhZ25vc3RpY3MuUHJvY2VzcwYPAAAABVN0YXJ0CRAAAAAECQAAAC9TeXN0ZW0uUmVmbGVjdGlvbi5NZW1iZXJJbmZvU2VyaWFsaXphdGlvbkhvbGRlcgcAAAAETmFtZQxBc3NlbWJseU5hbWUJQ2xhc3NOYW1lCVNpZ25hdHVyZQpTaWduYXR1cmUyCk1lbWJlclR5cGUQR2VuZXJpY0FyZ3VtZW50cwEBAQEBAAMIDVN5c3RlbS5UeXBlW10JDwAAAAkNAAAACQ4AAAAGFAAAAD5TeXN0ZW0uRGlhZ25vc3RpY3MuUHJvY2VzcyBTdGFydChTeXN0ZW0uU3RyaW5nLCBTeXN0ZW0uU3RyaW5nKQYVAAAAPlN5c3RlbS5EaWFnbm9zdGljcy5Qcm9jZXNzIFN0YXJ0KFN5c3RlbS5TdHJpbmcsIFN5c3RlbS5TdHJpbmcpCAAAAAoBCgAAAAkAAAAGFgAAAAdDb21wYXJlCQwAAAAGGAAAAA1TeXN0ZW0uU3RyaW5nBhkAAAArSW50MzIgQ29tcGFyZShTeXN0ZW0uU3RyaW5nLCBTeXN0ZW0uU3RyaW5nKQYaAAAAMlN5c3RlbS5JbnQzMiBDb21wYXJlKFN5c3RlbS5TdHJpbmcsIFN5c3RlbS5TdHJpbmcpCAAAAAoBEAAAAAgAAAAGGwAAAHFTeXN0ZW0uQ29tcGFyaXNvbmExW1tTeXN0ZW0uU3RyaW5nLCBtc2NvcmxpYiwgVmVyc2lvbj00LjAuMC4wLCBDdWx0dXJlPW5ldXRyYWwsIFB1YmxpY0tleVRva2VuPWI3N2E1YzU2MTkzNGUwODldXQkMAAAACgkMAAAACRgAAAAJFgAAAAoL"",
                    ""password"": ""test""
                }";

                JObject testJson = JObject.Parse(testPayload);
                string sandboxResult = ProcessInSandboxManually(testJson.ToString());
                Console.WriteLine($"[{DateTime.Now}] TestSandbox Sandbox Result: {sandboxResult}");

                return Json(new { success = true, message = "Sandbox test completed", sandboxResult = sandboxResult });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] TestSandbox Error: {ex.Message}");
                return Json(new { success = false, message = "Test failed: " + ex.Message });
            }
        }

        private string ProcessInSandboxManually(string payload)
        {
            var currentDomain = AppDomain.CurrentDomain;
            var setup = new AppDomainSetup
            {
                ApplicationBase = currentDomain.BaseDirectory,
                PrivateBinPath = currentDomain.BaseDirectory,
                DisallowBindingRedirects = false,
                DisallowCodeDownload = true,
                ConfigurationFile = currentDomain.SetupInformation.ConfigurationFile
            };

            var permissions = new PermissionSet(PermissionState.None);
            permissions.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
            permissions.AddPermission(new ReflectionPermission(ReflectionPermissionFlag.RestrictedMemberAccess));
            permissions.AddPermission(new FileIOPermission(FileIOPermissionAccess.Read | FileIOPermissionAccess.PathDiscovery, currentDomain.BaseDirectory));

            var sandboxDomain = AppDomain.CreateDomain("TestSandboxDomain", null, setup, permissions);
            {
                try
                {
                    var sandbox = (SandboxProxy)sandboxDomain.CreateInstanceAndUnwrap(
                        Assembly.GetExecutingAssembly().GetName().Name,
                        typeof(SandboxProxy).FullName);

                    return sandbox.AnalyzeAndBlock(payload);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] TestSandbox Sandbox Error: {ex.Message}");
                    return $"Sandbox failed: {ex.Message}";
                }
                finally
                {
                    AppDomain.Unload(sandboxDomain);
                }
            }
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}