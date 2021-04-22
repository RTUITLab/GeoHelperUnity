using System;
using ServerModels;
using UnityEngine;


namespace UnityModels
{
    [Serializable]
    public class Geo3dObject: GeoObject
    {
        public Geo3dObject():base()
        {
            
        }
        [SerializeField]
        public String url;

        public void Initialize(GeoAudioObjectModel geoAudioObjectModel, String url)
        {
            this.id = geoAudioObjectModel.id;
            this.url = url;
            this.gpsLocation.lat = geoAudioObjectModel.position.lat;
            this.gpsLocation.lng = geoAudioObjectModel.position.lng;
            Debug.Log("initialized " + geoAudioObjectModel.name);
        }
    }
}