using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Class for initialize prefab POIObjectTextCanvas
/// </summary>
public class POIObjectTextDisplay : MonoBehaviour
{
    public TextMeshProUGUI title;
    public TextMeshProUGUI description;
    public TextMeshProUGUI distance;

    public void Initialize(GeoObject geoObject, double distance)
    {
        this.title.text = geoObject.name;
        this.description.text = geoObject.description;
        this.distance.text = distance.ToString().Substring(0, 6) + " meters";
        Debug.Log("initialized " + geoObject.name);
    }
}
