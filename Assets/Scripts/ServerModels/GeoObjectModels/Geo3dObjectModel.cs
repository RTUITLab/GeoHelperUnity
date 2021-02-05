using System;

namespace ServerModels
{
    public class Geo3dObjectModel : GeoObjectModel
    {
        public string url { get; set; }
        
        public override string ToString()
        {
            return $"{nameof(Geo3dObjectModel)}  id: {id}, name: {name}, type: {type}, position: {position.ToString()}, url: {url}";
        }
    }
}