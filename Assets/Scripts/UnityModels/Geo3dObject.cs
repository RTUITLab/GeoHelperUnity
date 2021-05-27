using System;
using ServerModels;
using UnityEngine;


namespace UnityModels
{
    [Serializable]
    public class Geo3dObject: GeoObject
    {
        public Geo3dObject(): base()
        {
            
        }
        [SerializeField]
        public String url;

        public void Initialize(Geo3dObjectModel geo3dObjectModel)
        {
            id = geo3dObjectModel.id;
            url = geo3dObjectModel.url;
            gpsLocation = new LocationDataModel(geo3dObjectModel.position.lat, geo3dObjectModel.position.lng);
            Debug.Log("initialized " + geo3dObjectModel.name);
        }
    }
}