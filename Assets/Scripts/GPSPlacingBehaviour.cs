using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using UnityEngine.XR.ARFoundation;
using System.Linq;
using TMPro;

[RequireComponent(typeof(WebSocketsBehaviour))]
public class GPSPlacingBehaviour : MonoBehaviour
{
    /// <summary>
    /// Prefab for geoobject type "text"
    /// </summary>
    public GameObject POI_object_text;

    /// <summary>
    /// Var for showing current location of user
    /// </summary>
    public TextMeshProUGUI currentLocationLog;

    private LocationData currentLocation = null;

    /// <summary>
    /// semaphore for sequental requests to server and trying placing objects to scene
    /// </summary>
    private static bool isSceneReadyToChange = true;

    private bool gpsLocationInitialized = false;

    private WebSocketsBehaviour webSockets;

    private static List<GeoObject> geoObjectsInScene = new List<GeoObject>();

    private void Start()
    {

        RunGPSTracking();
        webSockets = GetComponent<WebSocketsBehaviour>();

    }

    private void Update()
    {
        // force geoobjects type "text" look at camera
        List<POIObjectTextDisplay> foundObjectsInScene = new List<POIObjectTextDisplay>(FindObjectsOfType<POIObjectTextDisplay>());
        foundObjectsInScene.ForEach((POIObjectTextDisplay el) =>
        {
            el.transform.LookAt(Camera.main.transform);
        });
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

    void UpdateGeoObjectsPositions(float lat, float lng)
    {
        GPSEncoder.SetLocalOrigin(new Vector2(lat, lng));

        List<POIObjectTextDisplay> foundObjectsInScene = new List<POIObjectTextDisplay>(FindObjectsOfType<POIObjectTextDisplay>());
        geoObjectsInScene.ForEach((GeoObject el) =>
        {
            var searchObj = foundObjectsInScene.Find((foundEl) => foundEl.GetComponent<POIObjectTextDisplay>().name == el.name);
            if (searchObj)
            {
                searchObj.transform.position = GPSEncoder.GPSToUCS(el.position.lat, el.position.lng);
                Debug.Log($"Update object position of {el.name} at location lat: {el.position.lat}, lng: {el.position.lng}");
                Debug.Log($"Distance to {el.name} {DistanceBetween2Geoobjects(lat, lng, el.position.lat, el.position.lng)}");
            }
        });
    }

    async Task TestPlacingObjects()
    {
        LocationData lastKnownLocation = new LocationData(Input.location.lastData.latitude, Input.location.lastData.longitude);

        currentLocationLog.text = $"Current location is lat: {lastKnownLocation.lat}, lng: {lastKnownLocation.lng}";

        // if current location changed then update positions of all geoobjects in scene
        if (!(lastKnownLocation.lat == currentLocation.lat && lastKnownLocation.lng == currentLocation.lng))
        {
            currentLocation = lastKnownLocation;
            Debug.Log("UpdateGeoObjectsPositions");
            UpdateGeoObjectsPositions(lastKnownLocation.lat, lastKnownLocation.lng);
        }

        if (webSockets)
        {
            // !IMPORTANT>DO NOT CHANGE FORM OF REQUEST STRING !!!
            // string for request objects to place to scene
            string reqString = "{ \"lat\": " + lastKnownLocation.lat.ToString() + ", \"lng\": " + lastKnownLocation.lng.ToString() + "}";

            string responseData = await webSockets.ReceiveObjectsFromServer(reqString);

            if (responseData != "")
            {
                //Debug.Log($"Got response: {responseData}");

                // json response from server for user location request
                ResponseFromServerLocationData response = JsonUtility.FromJson<ResponseFromServerLocationData>(responseData);
                //Debug.Log(response);

                if (response != null && response.success)
                {
                    if (response.data.Count > 0)
                    {

                        List<GeoObject> packGeoObjectsFromServer = new List<GeoObject>(response.data);

                        List<GeoObject> geoObjectsInSceneClone = new List<GeoObject>(geoObjectsInScene);

                        List<POIObjectTextDisplay> foundObjectsInScene = new List<POIObjectTextDisplay>(FindObjectsOfType<POIObjectTextDisplay>());

                        // delete objects from scene, which not found in pack of geoobjects from server
                        geoObjectsInScene.ForEach((GeoObject el) =>
                        {
                            if (!packGeoObjectsFromServer.Any(g => g.name == el.name))
                            {
                                geoObjectsInSceneClone.Remove(el);
                                var delObj = foundObjectsInScene.Find((delEl) => delEl.GetComponent<POIObjectTextDisplay>().name == el.name);
                                if (delObj)
                                    Destroy(delObj.gameObject);
                            }
                        });

                        // determine geoobjects, which must be added to scene
                        List<GeoObject> geoObjectsToAdd = new List<GeoObject>();
                        packGeoObjectsFromServer.ForEach((GeoObject el) =>
                        {
                            if (!geoObjectsInScene.Any(g => g.name == el.name))
                            {
                                geoObjectsToAdd.Add(el);
                                geoObjectsInSceneClone.Add(el);
                            }
                        });

                        // replace previous version of list geoobjects in scene
                        geoObjectsInScene = geoObjectsInSceneClone;


                        if (geoObjectsToAdd.Count > 0)
                        {
                            Debug.Log(geoObjectsToAdd.Count.ToString() + " geoObjectsToAdd");
                            geoObjectsToAdd.ForEach((GeoObject el) =>
                            {
                                // adding geoobjects of type "text" to scene and init content of geoobject(point of interest)
                                if (el.type == "text")
                                {
                                    GPSEncoder.SetLocalOrigin(new Vector2(lastKnownLocation.lat, lastKnownLocation.lng));
                                    var objectPlace = GPSEncoder.GPSToUCS(el.position.lat, el.position.lng);

                                    GameObject newGameobject = Instantiate(POI_object_text, objectPlace, Quaternion.identity) as GameObject;

                                    newGameobject.transform.LookAt(Camera.main.transform);

                                    Debug.Log($"Distance to {el.name}" +
                                        $" {DistanceBetween2Geoobjects(lastKnownLocation.lat, lastKnownLocation.lng, el.position.lat, el.position.lng)}");

                                    // init content of geoobject(point of interest)
                                    newGameobject.GetComponent<POIObjectTextDisplay>().Initialize(el);
                                    Debug.Log($"Placed object {el.name} at location lat: {el.position.lat}, lng: {el.position.lng}");
                                }

                            });
                        }


                    }
                    else
                    {
                        //Debug.Log("Objects not found in this location");
                    }
                }
            }
        }
    }

    void RunGPSTracking()
    {
        if (!gpsLocationInitialized)
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

        // Start service before querying location with accuracy 1 meter
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
            gpsLocationInitialized = true;
        }

    }

    private void OnDisable()
    {
        Input.location.Stop();
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
