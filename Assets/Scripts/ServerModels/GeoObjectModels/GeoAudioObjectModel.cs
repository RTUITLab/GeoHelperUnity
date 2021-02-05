using System;

namespace ServerModels
{
    [Serializable]
    public class GeoAudioObjectModel : IGeoObjectModel
    {
        public string url;
    
        public override string ToString()
        {
            return $"{typeof(GeoAudioObjectModel)} id: {id}, name: {name}, type: {type}, position: {position.ToString()}, url: {url}";
        }

        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public LocationDataModel position { get; set; }
    }
}