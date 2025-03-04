using Demoweb.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Demoweb.Controllers
{
    public class AccountController : Controller
    {
        // In-memory storage for demo (replace with database in production)
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
            public string Username { get; set; }
            public string Password { get; set; }
        }

        [HttpPost]
        public ActionResult Login(LoginModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
            {
                return Json(new { success = false, message = "Invalid input" });
            }

            var user = users.FirstOrDefault(u => u.Username == model.Username && u.Password == model.Password);
            if (user != null)
            {
                Session["UserId"] = user.Id;
                Session["Username"] = user.Username;
                return Json(new { success = true, message = "Login successful" });
            }
            return Json(new { success = false, message = "Invalid credentials" });
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