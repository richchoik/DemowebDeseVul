using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Serialization;
using Demoweb.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Data; 
using System.Data.Services.Internal;
using System.Diagnostics;

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
        public ActionResult Login(string unused = null) // Tham số giả để tránh trùng tên
        {
            try
            {
                // Đọc JSON thô từ request
                string jsonPayload;
                Request.InputStream.Position = 0; // Reset vị trí stream về đầu
                using (var reader = new StreamReader(Request.InputStream))
                {
                    jsonPayload = reader.ReadToEnd();
                }
                System.Diagnostics.Debug.WriteLine("Raw JSON: " + jsonPayload);

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
                    // RCE sẽ xảy ra ở đây nếu payload hợp lệ
                }
                else
                {
                    // Nếu không, lấy Username như chuỗi bình thường
                    username = usernameToken?.ToString();
                }

                // Lấy Password như chuỗi
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
                System.Diagnostics.Debug.WriteLine("Login Error: " + ex.Message);
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
        public ActionResult Signup(SignupModel model)
        {
            string payload;
            if (model == null || string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
            {
                return Json(new { success = false, message = "Invalid input" });
            }

            if (users.Any(u => u.Username == model.Username))
            {
                return Json(new { success = false, message = "Username already exists" });
            }

            var newUser = new User
            {
                Id = users.Max(u => u.Id) + 1,
                Username = model.Username,
                Password = model.Password
            };
            users.Add(newUser);
            return Json(new { success = true, message = "Signup successful" });
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }
    }
}