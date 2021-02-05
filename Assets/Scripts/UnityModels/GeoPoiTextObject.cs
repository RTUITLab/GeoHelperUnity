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
    [Serializable]
    public class GeoPoiTextObject : GeoObject
    {
        public GeoPoiTextObject():base()
        {

        }

        public TextMeshProUGUI title;
        public TextMeshProUGUI description;
        public TextMeshProUGUI distance;
        public void Initialize(GeoPoiObjectModel geoObjectModel, double distance)
        {
            this.id = geoObjectModel.id;
            this.title.text = geoObjectModel.name;
            this.description.text = geoObjectModel.description;
            this.distance.text = Convert.ToUInt32(distance).ToString() + " meters";
            Debug.Log("initialized " + geoObjectModel.name);
        }
        

    }
}

