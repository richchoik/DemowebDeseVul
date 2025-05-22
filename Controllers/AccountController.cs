﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web.Mvc;
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
    }
}