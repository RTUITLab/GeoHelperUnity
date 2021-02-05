using System;
using ServerModels;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityModels
{
    [Serializable]
    public abstract class GeoObject : MonoBehaviour
    {
        public string id;
        public string geoObjectName;
        public LocationDataModel locationData;
    }    

    // public interface IGeoObject
    // {
    //     public string id { get; set; }
    //     public string geoObjectName { get; set; }
    //     public LocationDataModel locationData { get; set; }
    // }
}