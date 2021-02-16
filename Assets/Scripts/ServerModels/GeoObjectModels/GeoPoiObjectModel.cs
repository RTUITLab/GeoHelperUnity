using System;

namespace ServerModels
{
    // [Serializable]
    public class GeoPoiObjectModel : GeoObjectModel
    {
        public string description { get; set; }
    
        public override string ToString()
        {
            return  $"{nameof(GeoPoiObjectModel)} id: {id}, name: {name}, type: {type}, position: {position.ToString()}, description: {description}";
        }
    }
}