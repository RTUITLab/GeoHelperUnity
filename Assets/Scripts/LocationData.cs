using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LocationData
{
    public LocationData(float latitude, float longitude)
    {
        lat = latitude;
        lng = longitude;
    }

    public float lat;
    public float lng;
}

[Serializable]
public class ResponseFromServerLocationData
{
    public bool success;
    public List<GeoObject> data;
}

[Serializable]
public class GeoObject
{
    public string id;
    public string name;
    public string type;
    public string description;
    public LocationData position;
}