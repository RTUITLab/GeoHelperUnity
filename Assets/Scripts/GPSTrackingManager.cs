using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using UnityEngine.XR.ARFoundation;
using System.Linq;

[RequireComponent(typeof(WebSocketsBehaviour))]
public class GPSTrackingManager : MonoBehaviour
{
    public GameObject POI_object_text;
    private LocationData currentLocation = null;
    private static bool isSceneReadyToChange = true;
    private bool gameRunned = false;
    private WebSocketsBehaviour webSockets;
    private string responseData;
    private System.Threading.Timer timer;
    private static List<GeoObject> geoObjectsInScene = new List<GeoObject>();

    private void Start()
    {

        RunGPSTracking();

        //GPSEncoder.SetLocalOrigin(new Vector2(55.5537f, 37.46738f));
        //var objectPlace = GPSEncoder.GPSToUCS(55.5537f, 37.46738f);
        //Debug.Log(DistanceBetween2Geoobjects(55.5537f, 37.46738f, 55.55346f, 37.46838f));
        //var rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        //GameObject newGameobject = Instantiate(POI_object_text, objectPlace, Quaternion.identity) as GameObject;
        //newGameobject.transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        //newGameobject.transform.Rotate(new Vector3(0,90,0));
        //Debug.Log($"Placed object {newGameobject}");

        //newGameobject.GetComponent<POIObjectTextDisplay>().Initialize(el);
        //Debug.Log($"Placed object {el.name} at location lat: {el.position.lat}, lon: {el.position.lng}");
        //TestPlacingObjects();
        //if (!gameRunned)
        //{
        //    StartCoroutine(FetchLocationData());
        //}
        webSockets = GetComponent<WebSocketsBehaviour>();



    }

    private void OnDestroy()
    {
        //timer.Dispose();
    }

    private async void LateUpdate()
    {
        if (webSockets.GetWSConnectionState() == "Open" && isSceneReadyToChange && currentLocation != null)
        {
            isSceneReadyToChange = false;
            await TestPlacingObjects();
            isSceneReadyToChange = true;
        }
    }

    async Task TestPlacingObjects()
    {
        //user location
        float lat = currentLocation.lat;
        float lon = currentLocation.lng;

        Debug.Log($"Current location is lat: {lat}, lon: {lon}");
        string reqString = "{ \"lat\": " + lat.ToString() + ", \"lng\": " + lon.ToString() + "}";
        if (webSockets)
        {
            string responseData = await webSockets.ReceiveObjectsFromServer(reqString);
            if (responseData != "")
            {
                Debug.Log($"Got response: {responseData}");

                // json response from server for user location request
                //string respStr = "{\"success\":true,\"data\":[{\"name\":\"Test 1\",\"type\":\"text\",\"description\":\"Text\",\"position\":{\"lat\":55.79167795691988,\"lng\":37.59902526440428}},{\"name\":\"Test 2\",\"type\":\"text\",\"description\":\"Texxt\",\"position\":{\"lat\":55.74068806200905,\"lng\":37.66356994213866}}]}";
                ResponseFromServerLocationData response = JsonUtility.FromJson<ResponseFromServerLocationData>(responseData);
                //Debug.Log(response);
                if (response != null && response.success)
                {
                    if (response.data.Count > 0)
                    {
                        GPSEncoder.SetLocalOrigin(new Vector2(lat, lon));

                        List<GeoObject> newPackGeoObjectsInScene = new List<GeoObject>();

                        response.data.ForEach((GeoObject el) =>
                        {

                            if (el.type == "text")
                            {
                                newPackGeoObjectsInScene.Add(el);
                            }
                        });
                        Debug.Log(newPackGeoObjectsInScene.Count.ToString() + " newPackGeoObjectsInScene");
                        List<GeoObject> geoObjectsInSceneClone = new List<GeoObject>(geoObjectsInScene);
                        List<POIObjectTextDisplay> foundObjectsInScene = new List<POIObjectTextDisplay>(FindObjectsOfType<POIObjectTextDisplay>());
                        geoObjectsInScene.ForEach((GeoObject el) =>
                        {
                            if (!newPackGeoObjectsInScene.Any(g => g.name == el.name))
                            {
                                geoObjectsInSceneClone.Remove(el);
                                var delObj = foundObjectsInScene.Find((delEl) => delEl.GetComponent<POIObjectTextDisplay>().name == el.name);
                                if (delObj)
                                    Destroy(delObj.gameObject);
                            }
                        });
                        List<GeoObject> geoObjectsToAdd = new List<GeoObject>();
                        newPackGeoObjectsInScene.ForEach((GeoObject el) =>
                        {
                            if (!geoObjectsInScene.Any(g => g.name == el.name))
                            {
                                geoObjectsToAdd.Add(el);
                                geoObjectsInSceneClone.Add(el);
                            }
                        });
                        Debug.Log(geoObjectsToAdd.Count.ToString() + " geoObjectsToAdd");
                        geoObjectsInScene = geoObjectsInSceneClone;
                        geoObjectsToAdd.ForEach((GeoObject el) =>
                        {
                            if (el.type == "text")
                            {

                                var objectPlace = GPSEncoder.GPSToUCS(el.position.lat, el.position.lng);
                                Debug.Log($"Distance to {el.name} {DistanceBetween2Geoobjects(lat, lon, el.position.lat, el.position.lat)}");
                                var rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
                                GameObject newGameobject = Instantiate(POI_object_text, objectPlace, Quaternion.identity) as GameObject;
                                newGameobject.transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);

                                newGameobject.GetComponent<POIObjectTextDisplay>().Initialize(el);
                                Debug.Log($"Placed object {el.name} at location lat: {el.position.lat}, lon: {el.position.lng}");
                            }

                        });


                    }
                    else
                    {
                        Debug.Log("Objects not found in this location");
                    }
                }
            }
        }
    }

    void RunGPSTracking()
    {
        if (!gameRunned)
        {
            StartCoroutine(FetchLocationData());

        }
    }

    private IEnumerator FetchLocationData()
    {
        // First, check if user has location service enabled
        if (!Input.location.isEnabledByUser)
        {
            Debug.Log("Location disabled");
            yield break;
        }

        gameRunned = true;

        // Start service before querying location
        Input.location.Start(1f);
        Debug.Log("Fetching Location..");
        // Wait until service initializes
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // Service didn't initialize in 20 seconds
        if (maxWait < 1)
        {
            Debug.Log("Location Timed out");
            yield break;
        }

        // Connection has failed
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.Log("Unable to determine device location");
            yield break;

        }
        else
        {
            var loc = Input.location.lastData;
            currentLocation = new LocationData(loc.latitude, loc.longitude);
        }

        Input.location.Stop();
        gameRunned = false;
    }


    private double DistanceBetween2Geoobjects(double lat1, double long1, double lat2, double long2)
    {
        double _eQuatorialEarthRadius = 6378.1370D;
        double _d2r = (Math.PI / 180D);

        double dlong = (long2 - long1) * _d2r;
        double dlat = (lat2 - lat1) * _d2r;
        double a = Math.Pow(Math.Sin(dlat / 2D), 2D) + Math.Cos(lat1 * _d2r) * Math.Cos(lat2 * _d2r) * Math.Pow(Math.Sin(dlong / 2D), 2D);
        double c = 2D * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1D - a));
        double d = _eQuatorialEarthRadius * c;

        return d * 1000;
    }
}
