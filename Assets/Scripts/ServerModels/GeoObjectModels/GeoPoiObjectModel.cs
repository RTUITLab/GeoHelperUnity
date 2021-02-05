using System;

namespace ServerModels
{
    [Serializable]
    public class GeoPoiObjectModel : IGeoObjectModel
    {
        public string description { get; set; }
    
        public override string ToString()
        {
            return  $"{typeof(GeoPoiObjectModel)} id: {id}, name: {name}, type: {type}, position: {position.ToString()}, description: {description}";
        }
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public LocationDataModel position { get; set; }
    }
}