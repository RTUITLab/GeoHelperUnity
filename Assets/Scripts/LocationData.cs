using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LocationData 
{
    public LocationData(float lat, float lng)
    {
        this.lat = lat;
        this.lng = lng;
    }

    public float lat;
    public float lng;

    public override string ToString()
    {
        return base.ToString() + $" lat: {lat}, long: {lng}";
    }
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

    public override string ToString()
    {
        return base.ToString() + $" id: {id}, name: {name}, type: {type}, description: {description}, position: {position.ToString()}";
    }
}