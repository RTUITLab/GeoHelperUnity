using System;
using ServerModels;
using UnityEngine;

namespace UnityModels
{
    [Serializable]
    public class GeoAudioObject: GeoObject
    {
        public GeoAudioObject():base()
        {
            
        }
        
        [SerializeField]
        public AudioClip clip;

        public void Initialize(GeoAudioObjectModel geoAudioObjectModel, AudioClip clip)
        {
            this.id = geoAudioObjectModel.id;
            this.clip = clip;
            this.gpsLocation.lat = geoAudioObjectModel.position.lat;
            this.gpsLocation.lng = geoAudioObjectModel.position.lng;
            Debug.Log("initialized " + geoAudioObjectModel.name);
            this.GetComponent<AudioSource>().clip = clip;
        }

    }
}