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
        [SerializeField]
        public TextMeshProUGUI title;
        [SerializeField]
        public TextMeshProUGUI description;
        [SerializeField]
        public TextMeshProUGUI distance;
        public void Initialize(GeoPoiObjectModel geoObjectModel, double distance)
        {
            id = geoObjectModel.id;
            title.text = geoObjectModel.name;
            description.text = geoObjectModel.description;
            gpsLocation.lat = geoObjectModel.position.lat;
            gpsLocation.lng = geoObjectModel.position.lng;
            this.distance.text = Convert.ToUInt32(distance).ToString() + " meters";
            Debug.Log("initialized " + geoObjectModel.name);
        }
    }
}

