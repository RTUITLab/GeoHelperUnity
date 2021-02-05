using System;
using System.Collections;
using System.Collections.Generic;
using ServerModels;
using TMPro;
using UnityEngine;
using UnityModels;

namespace UnityModels
{
    /// <summary>
    /// Class for initialize prefab POIObjectTextCanvas
    /// </summary>
    public class GeoPoiTextObject : GeoObject
    {
        public GeoPoiTextObject():base()
        {

        }
        [SerializeField]
        public TextMeshProUGUI title;
        [SerializeField]
        public TextMeshProUGUI description;
        [SerializeField]
        public TextMeshProUGUI distance;
        public void Initialize(GeoPoiObjectModel geoObjectModel, double distance)
        {
            this.id = geoObjectModel.id;
            this.title.text = geoObjectModel.name;
            this.description.text = geoObjectModel.description;
            this.gpsLocation.lat = geoObjectModel.position.lat;
            this.gpsLocation.lng = geoObjectModel.position.lng;
            this.distance.text = Convert.ToUInt32(distance).ToString() + " meters";
            Debug.Log("initialized " + geoObjectModel.name);
        }
        

    }
}

