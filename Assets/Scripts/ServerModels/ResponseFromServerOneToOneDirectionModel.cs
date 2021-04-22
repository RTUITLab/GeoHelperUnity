using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ServerModels
{
    [Serializable]
    public class Entity
    {
        [JsonProperty("_id")]
        public string geoObjectId;
        [JsonProperty("position")]
        public LocationDataModel position;
        [JsonProperty("type")]
        public string type;
        public override string ToString()
        {
            return $"geoObjectId: {geoObjectId}; pos: {position.ToString()}; type: {type.ToString()}";
        }
    }
    
    [Serializable]
    public class EndLocation
    {
        [JsonProperty("entity")] 
        public Entity entity;

        public override string ToString()
        {
            return $" {entity.ToString()}";
        }
    }

    [Serializable]
    public class Step : LocationDataModel
    {
        public Step(float lat, float lng, int stepId) : base(lat, lng)
        {
            _stepId = stepId;
        }
        
        [JsonProperty("id")]
        public int _stepId;

        public override string ToString()
        {
            return base.ToString() + $" stepId{_stepId.ToString()}";
        }
    }
    
    [Serializable]
    public class Message
    {
        [JsonProperty("end_location")]
        public EndLocation _endLocation;        
        [JsonProperty("steps")]
        public List<Step> _steps;
        public override string ToString()
        {
            return $"{_endLocation.ToString()} \n {string.Join("; ", _steps)}";
        }
    }
    
    [Serializable]
    public class ResponseFromServerOneToOneDirectionModel
    {
        [JsonProperty("success")]
        public bool success;
        [JsonProperty("message")]
        public Message message;

        public override string ToString()
        {
            return $"Response success: {success.ToString()} \n {message.ToString()}";
        }
    }
}