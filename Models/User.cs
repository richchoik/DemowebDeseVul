using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Demoweb.Models
{
    public class User
    {
        public int Id { get; set; }
        [JsonProperty(PropertyName = "body")]
        public string Username { get; set; }
        public string Password { get; set; }  // In production, use hashing
    }
}