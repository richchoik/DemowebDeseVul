using System;
using System.IO;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Demoweb.Modules
{
    public class DeserializationProtectionModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.BeginRequest += OnBeginRequest;
        }

        private void OnBeginRequest(object sender, EventArgs e)
        {
            var context = ((HttpApplication)sender).Context;

            // Kiểm tra nếu yêu cầu là POST và có ContentType là application/json
            if (context.Request.HttpMethod == "POST" && context.Request.ContentType == "application/json")
            {
                // Đọc dữ liệu từ InputStream
                string jsonPayload;
                using (var reader = new StreamReader(context.Request.InputStream))
                {
                    jsonPayload = reader.ReadToEnd();
                }

                // Kiểm tra xem payload có độc hại hay không -
                if (IsMaliciousPayload(jsonPayload))
                {
                    System.Diagnostics.Debug.WriteLine("Malicious payload detected: " + jsonPayload);
                    context.Response.StatusCode = 400;
                    context.Response.Write("Bad Request: Malicious payload detected");
                    context.Response.End();
                    return;
                }

                // Lưu dữ liệu an toàn vào HttpContext.Items để controller sử dụng
                context.Items["JsonPayload"] = jsonPayload;
            }
        }

        private bool IsMaliciousPayload(string jsonPayload)
        {
            try
            {
                var jsonObject = JObject.Parse(jsonPayload);

                // Kiểm tra các trường trong JSON
                foreach (var property in jsonObject.Properties())
                {
                    // Phát hiện `$type` - dấu hiệu của deserialization độc hại trong JSON
                    if (property.Value.Type == JTokenType.Object && property.Value["$type"] != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Detected $type in payload: " + property.Value["$type"]);
                        return true;
                    }

                    // Phát hiện chuỗi Base64 dài (có thể chứa payload độc hại)
                    if (property.Value.Type == JTokenType.String && IsBase64String(property.Value.ToString()) && property.Value.ToString().Length > 100)
                    {
                        System.Diagnostics.Debug.WriteLine("Detected long Base64 string: " + property.Value.ToString());
                        return true;
                    }
                }
            }
            catch (JsonException)
            {
                // JSON không hợp lệ, coi như payload bất thường
                System.Diagnostics.Debug.WriteLine("Invalid JSON detected: " + jsonPayload);
                return true;
            }

            return false;
        }

        private bool IsBase64String(string base64)
        {
            if (string.IsNullOrEmpty(base64) || base64.Length % 4 != 0)
            {
                return false;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(base64, @"^[A-Za-z0-9\+/]*={0,2}$"))
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

        public void Dispose() { }
    }
}