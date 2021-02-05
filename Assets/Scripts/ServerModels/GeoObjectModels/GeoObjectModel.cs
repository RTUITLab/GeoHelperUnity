using System;

namespace ServerModels
{
    // [Serializable]
    public class GeoObjectModel : IGeoObjectModel
    {
        public string id { get; set; }
        public string name { get;  set; }
        public string type { get; set; }
        public LocationDataModel position { get; set; }

        public override string ToString()
        {
            return $"{nameof(GeoObjectModel)} id: {id}, name: {name}, type: {type}, position: {position.ToString()}";
        }
    }
}