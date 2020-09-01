using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

//[RequireComponent(typeof(ARSessionOrigin))]
public class ObjectPlacingAnchorBehaviour : MonoBehaviour
{
    //public ARSessionOrigin aRSessionOrigin;
    public void setLocalOrigin(Vector2 vector)
    {
        GPSEncoder.SetLocalOrigin(vector);

    }

    public void PlaceObjectToUnityScene(ref GameObject gameObject, LocationData objectLocation)
    {
        //var arOrig = GameObject.FindObjectOfType<ARSessionOrigin>();
        //var userPlace = GPSEncoder.GPSToUCS(userLocation.latitude, userLocation.longitude);
        //Debug.Log(GPSEncoder.GPSToUCS(objectLocation.lat, objectLocation.lng).ToString());

        var objPlace = GPSEncoder.GPSToUCS(objectLocation.lat, objectLocation.lng);
        gameObject.transform.position = objPlace;
        //Instantiate(gameObject, objPlace, Quaternion.identity);
        //gameObject.transform.position = objPlace;
        //arOrig.MakeContentAppearAt(gameObject.transform, objPlace);
    }
}
