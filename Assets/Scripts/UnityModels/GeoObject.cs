using System;
using ServerModels;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityModels
{
    public abstract class GeoObject : MonoBehaviour
    {
        protected string id;
        protected string geoObjectName;
        public LocationDataModel gpsLocation;
    }
}