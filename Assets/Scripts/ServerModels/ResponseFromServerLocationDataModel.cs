using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityModels;

namespace ServerModels
{
    [Serializable]
    public class ResponseFromServerLocationDataModel
    {
        [JsonProperty("success")]
        public bool success;
        [JsonProperty("poiObjectModels")]
        public List<GeoPoiObjectModel> poiObjectModels;
        [JsonProperty("geo3dObjectModels")]
        public List<Geo3dObjectModel> geo3dObjectModels;
        [JsonProperty("geoAudioObjectModels")]
        public List<GeoAudioObjectModel> geoAudioObjectModels;

        public override string ToString()
        {
            return $"Response success: {success} \n {poiObjectModels.ToString()}" +
                   $" \n {geoAudioObjectModels} \n {geo3dObjectModels}";
        }
    }
}