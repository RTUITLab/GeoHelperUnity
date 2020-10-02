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
    public TextMeshProUGUI location;

    public void Initialize(GeoObject geoObject)
    {
        this.title.text = geoObject.name;
        this.description.text = geoObject.description;
        this.location.text = "Location: " + geoObject.position.lat.ToString() + ", " + geoObject.position.lng.ToString();
        Debug.Log("initialized " + geoObject.name);
    }
}
