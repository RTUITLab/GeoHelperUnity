using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ServerModels
{
    [Serializable]
    class EndLocation
    {
        [JsonProperty("_id")]
        private string geoObjectId;
        [JsonProperty("position")]
        private LocationDataModel position;
        
    }

    [Serializable]
    class Step : LocationDataModel
    {
        public Step(float lat, float lng) : base(lat, lng)
        {
        }
        
        [JsonProperty("id")]
        private int _stepId;
        
    }
    
    [Serializable]
    public class Message
    {
        [JsonProperty("end_location")]
        private EndLocation _endLocation;        
        [JsonProperty("steps")]
        private List<Step> _steps;
        
    }
    
    [Serializable]
    public class ResponseFromServerOneToOneDirectionModel
    {
        [JsonProperty("success")]
        public bool success;
        [JsonProperty("message")]
        public Message message;

        /*public override string ToString()
        {
            return $"Response success: {success} \n {poiObjectModels.ToString()}" +
                   $" \n {geoAudioObjectModels} \n {geo3dObjectModels}";
        }*/
    }
}