using System;
using Newtonsoft.Json;

namespace ServerModels
{
    [Serializable]
    public class User
    {
        [JsonProperty("_id")]
        public string _id;
        [JsonProperty("username")]
        public string username;
    }
    
    [Serializable]
    public class ResponseFromServerToLoginModel
    {
        [JsonProperty("success")]
        public bool success;
        [JsonProperty("message")]
        public string message;        
        [JsonProperty("token")]
        public string token;
        [JsonProperty("user")]
        public User user;
        

        public override string ToString()
        {
            return $"Response success: {success.ToString()} \n {message.ToString()}";
        }
    }
}