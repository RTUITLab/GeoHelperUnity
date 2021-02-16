using System;

namespace ServerModels
{
    // [Serializable]
    public class GeoAudioObjectModel : GeoObjectModel
    {
        public string url { get; set; }
    
        public override string ToString()
        {
            return $"{nameof(GeoAudioObjectModel)} id: {id}, name: {name}, type: {type}, position: {position.ToString()}, url: {url}";
        }
    }
}