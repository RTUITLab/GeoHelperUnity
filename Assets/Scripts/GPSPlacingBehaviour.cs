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

    /// <summary>
    /// Variable defines max lenght of vector to POI from (0,0,0) in Unity units
    /// </summary>
    const int maxDistanceToPOIGeoobject = 15;

    private LocationData currentLocation = null;

    /// <summary>
    /// semaphore for sequental requests to server and trying placing objects to scene
    /// </summary>
    private static bool isSceneReadyToChange = true;

    private bool gpsLocationInitialized = false;

    private WebSocketsBehaviour webSockets;

    private static List<GeoObject> geoObjectsInScene = new List<GeoObject>();

    private int compasTimer = 0;
    /// <summary>
    ///     Current value of ping location timer.
    /// </summary>
    private float _currentTimer = 0;

    private float offsetFromTrue;

    private bool firstIterFlag = true;

    /// <summary>
    ///     Frequency at which we check our device location (to save battery).
    /// </summary>
    private const float LOCATION_PING = 10f; // 10 seconds

    public GameObject ToNorth;

    private void Start()
    {
        //GameObject.FindWithTag("ARSessionOrigin").transform.rotation = Quaternion.Euler(0, -Input.compass.trueHeading, 0);
        RunGPSTracking();
        webSockets = GetComponent<WebSocketsBehaviour>();
        offsetFromTrue = Input.compass.trueHeading;
        ToNorth.transform.rotation = Quaternion.Euler(0, Input.compass.trueHeading, 0);
        Debug.LogError("offsetFromTrue = " + offsetFromTrue.ToString());
        GameObject.FindWithTag("ARSessionOrigin").transform.rotation = Quaternion.Euler(0, Input.compass.trueHeading, 0);
    }

    private void Update()
    {
        // force geoobjects type "text" look at camera
        List<POIObjectTextDisplay> foundObjectsInScene = new List<POIObjectTextDisplay>(FindObjectsOfType<POIObjectTextDisplay>());
        foundObjectsInScene.ForEach((POIObjectTextDisplay el) =>
        {
            el.transform.LookAt(Camera.main.transform);
        });

        _currentTimer += Time.deltaTime;
        if (_currentTimer > 6f)
        {
            LocationInfo locInfo = Input.location.lastData;
            UpdateGeoObjectsPositions(locInfo.latitude, locInfo.longitude);
            _currentTimer = 0;
        }
        //if (Input.location.status == LocationServiceStatus.Running &&
        //    _currentTimer > LOCATION_PING)
        //{
        //    LocationInfo locInfo = Input.location.lastData;
        //    Vector3 locInfoPos = GPSEncoder.GPSToUCS(new Vector2(locInfo.latitude, locInfo.longitude));
        //    float dist = Vector3.Distance(locInfoPos, Vector3.zero);
        //    if (dist > 10f)
        //    {
        //        UpdateGeoObjectsPositions(locInfo.latitude, locInfo.longitude);
        //    }
        //    _currentTimer = 0f;
        //}


    }

    private async void LateUpdate()
    {
        GameObject.FindWithTag("ARSessionOrigin").transform.rotation = Quaternion.Euler(0, Input.compass.trueHeading, 0);
        if (webSockets.GetWSConnectionState() == "Open" && isSceneReadyToChange && currentLocation != null)
        {
            //GameObject.FindWithTag("ARSessionOrigin").transform.rotation = Quaternion.Euler(0, 0, 0);
            //ToNorth.transform.rotation = Quaternion.Euler(0, 0, 0);
            isSceneReadyToChange = false;
            await TestPlacingObjects();
            isSceneReadyToChange = true;
            GameObject.FindWithTag("ARSessionOrigin").transform.rotation = Quaternion.Euler(0, Input.compass.trueHeading, 0);
            //if (Input.compass.trueHeading>180)
            //    ToNorth.transform.rotation = Quaternion.Euler(0, Input.compass.trueHeading, 0);
            //else
            
            if (compasTimer == 0)
            {
                Debug.LogError(Input.compass.trueHeading);
                compasTimer = 15;
            }
            else
                compasTimer--;
        }
    }
    
    void UpdateGeoObjectsPositions(float lat, float lng)
    {
        try
        {
            //ToNorth.transform.rotation = Quaternion.Euler(0, -Input.compass.trueHeading, 0);
            //GameObject.FindWithTag("ARSessionOrigin").transform.rotation = Quaternion.Euler(0, -Input.compass.trueHeading, 0);
            //ToNorth.transform.rotation = Quaternion.Euler(0, 0, 0);
            UpdatePOIGeoobjects(lat, lng);
            Debug.LogError("ARSessionOrigin offset" + (-offsetFromTrue).ToString());
            ///GameObject.FindWithTag("ARSessionOrigin").transform.eulerAngles += new Vector3(0.0f, -offsetFromTrue, 0.0f);
            ToNorth.transform.eulerAngles += new Vector3(0.0f, Input.compass.trueHeading, 0.0f);
            //ToNorth.transform.rotation = Quaternion.Euler(0, 0, 0);
            //GameObject.FindWithTag("ARSessionOrigin").transform.rotation = Quaternion.Euler(0, Input.compass.trueHeading, 0);
            //ToNorth.transform.rotation = Quaternion.Euler(0, Input.compass.trueHeading-offsetFromTrue, 0);

        }
        catch (Exception ex)
        {
            Debug.LogError(ex.ToString());
        }
    }

    void UpdatePOIGeoobjects(float lat, float lng)
    {
        try
        {
            List<POIObjectTextDisplay> foundObjectsInScene = new List<POIObjectTextDisplay>(FindObjectsOfType<POIObjectTextDisplay>());
            if (foundObjectsInScene.Count > 0)
            {
                GPSEncoder.SetLocalOrigin(new Vector2(lat, lng));
                GameObject.FindWithTag("ARSessionOrigin").transform.position = GPSEncoder.GPSToUCS(new Vector2(lat, lng));

                // if (Input.compass.enabled)
                // {
                //     GameObject.FindWithTag("ARSessionOrigin").transform.rotation = Quaternion.Euler(0, -Input.compass.magneticHeading, 0);
                //     //Debug.Log($"True heading {((int)Input.compass.trueHeading).ToString() + "° " + DegreesToCardinalDetailed(Input.compass.trueHeading)}");
                //     Debug.Log($"{DateTime.Now.ToString("HH:mm:ss tt")} Updated Magnetic heading {(-(int)Input.compass.magneticHeading).ToString() + "° " + DegreesToCardinalDetailed(Input.compass.magneticHeading)}");
                //     //Debug.Log($"Raw vector {Input.compass.rawVector.ToString()}");
                // }

                geoObjectsInScene.ForEach((GeoObject el) =>
                {
                    var searchObj = foundObjectsInScene.Find((foundEl) => foundEl.GetComponent<POIObjectTextDisplay>().getId() == el.id);
                    if (searchObj)
                    {

                        Vector3 positionOfGeoObject = GPSEncoder.GPSToUCS(el.position.lat, el.position.lng);

                        // if distance to POI greater then maxDistanceToPOIGeoobject units then normalize to maxDistanceToPOIGeoobject
                        if (positionOfGeoObject.magnitude > maxDistanceToPOIGeoobject)
                        {
                            searchObj.transform.position = positionOfGeoObject.normalized * maxDistanceToPOIGeoobject;
                        }
                        else
                        {
                            searchObj.transform.position = positionOfGeoObject;
                        }

                        Debug.Log($" {DateTime.Now.ToString("HH:mm:ss tt")} Update object position of {el.name} at location lat: {el.position.lat}, lng: {el.position.lng}");
                        double distanceToObject = DistanceBetween2GeoobjectsInM(lat, lng, el.position.lat, el.position.lng);

                        searchObj.distance.text = Convert.ToUInt32(distanceToObject).ToString() + " meters";

                        Debug.Log($" {DateTime.Now.ToString("HH:mm:ss tt")} Updated Distance to {el.name} {distanceToObject}m");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.ToString());
        }
    }

    async Task TestPlacingObjects()
    {
        LocationData lastKnownLocation = new LocationData(Input.location.lastData.latitude, Input.location.lastData.longitude);

        currentLocationLog.text = $"Current location is lat: {lastKnownLocation.lat}, lng: {lastKnownLocation.lng} Compass: " +
            $"{((int)Input.compass.magneticHeading).ToString() + "° " + DegreesToCardinalDetailed(Input.compass.magneticHeading)}";

        // if current location changed then update positions of all geoobjects in scene
        if (DistanceBetween2GeoobjectsInM(lastKnownLocation.lat, lastKnownLocation.lng, currentLocation.lat, currentLocation.lng) > 20)
        {
            currentLocation = lastKnownLocation;
            Debug.Log($" {DateTime.Now.ToString("HH: mm:ss tt")} UpdateGeoObjectsPositions");
            UpdateGeoObjectsPositions(lastKnownLocation.lat, lastKnownLocation.lng);
        }

        if (webSockets)
        {
            // !IMPORTANT>DO NOT CHANGE FORM OF REQUEST STRING !!!
            // string for request objects to place to scene
            string reqString = "{ \"lat\": " + currentLocation.lat.ToString() + ", \"lng\": " + currentLocation.lng.ToString() + "}";

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
                            if (!packGeoObjectsFromServer.Any(g => g.id == el.id))
                            {
                                geoObjectsInSceneClone.Remove(el);
                                var delObj = foundObjectsInScene.Find((delEl) => delEl.GetComponent<POIObjectTextDisplay>().getId() == el.id);
                                if (delObj)
                                    Destroy(delObj.gameObject);
                            }
                        });

                        // determine geoobjects, which must be added to scene
                        List<GeoObject> geoObjectsToAdd = new List<GeoObject>();
                        packGeoObjectsFromServer.ForEach((GeoObject el) =>
                        {
                            if (!geoObjectsInScene.Any(g => g.id == el.id))
                            {
                                geoObjectsToAdd.Add(el);
                                geoObjectsInSceneClone.Add(el);
                            }
                        });

                        // replace previous version of list geoobjects in scene
                        geoObjectsInScene = geoObjectsInSceneClone;


                        if (geoObjectsToAdd.Count > 0)
                        {
                            GPSEncoder.SetLocalOrigin(new Vector2(currentLocation.lat, currentLocation.lng));
                            GameObject.FindWithTag("ARSessionOrigin").transform.position = GPSEncoder.GPSToUCS(new Vector2(currentLocation.lat, currentLocation.lng));
                            // if (Input.compass.enabled)
                            // {
                            //     GameObject.FindWithTag("ARSessionOrigin").transform.rotation = Quaternion.Euler(0, -Input.compass.magneticHeading, 0);
                            //     //Debug.Log($"True heading {((int)Input.compass.trueHeading).ToString() + "° " + DegreesToCardinalDetailed(Input.compass.trueHeading)}");
                            //     Debug.Log($" {DateTime.Now.ToString("HH:mm:ss tt")} Updated Magnetic heading {(-(int)Input.compass.magneticHeading).ToString() + "° " + DegreesToCardinalDetailed(Input.compass.magneticHeading)}");
                            //     //Debug.Log($"Raw vector {Input.compass.rawVector.ToString()}");
                            // }

                            Debug.Log($"{ DateTime.Now.ToString("HH:mm:ss tt")}{ geoObjectsToAdd.Count.ToString()} geoObjectsToAdd");
                            geoObjectsToAdd.ForEach((GeoObject el) =>
                            {
                                // adding geoobjects of type "text" to scene and init content of geoobject(point of interest)
                                if (el.type == "text")
                                {
                                    Vector3 objectPlace = GPSEncoder.GPSToUCS(el.position.lat, el.position.lng);

                                    // if distance to POI greater then maxDistanceToPOIGeoobject units then normalize to maxDistanceToPOIGeoobject
                                    if (objectPlace.magnitude > maxDistanceToPOIGeoobject)
                                    {
                                        objectPlace = objectPlace.normalized * maxDistanceToPOIGeoobject;
                                    }

                                    GameObject newGameobject = Instantiate(POI_object_text, objectPlace, Quaternion.identity) as GameObject;

                                    newGameobject.transform.LookAt(Camera.main.transform);
                                    newGameobject.transform.SetParent(ToNorth.transform);
                                    double distanceToObject = DistanceBetween2GeoobjectsInM(currentLocation.lat, currentLocation.lng, el.position.lat, el.position.lng);

                                    Debug.Log($"{DateTime.Now.ToString("HH:mm:ss tt")} Distance to {el.name} {distanceToObject}m");


                                    // init content of geoobject(point of interest)
                                    newGameobject.GetComponent<POIObjectTextDisplay>().Initialize(el, distanceToObject);
                                    Debug.Log($" {DateTime.Now.ToString("HH:mm:ss tt")} Placed object {el.name} at location lat: {el.position.lat}, lng: {el.position.lng}");
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

    private string DegreesToCardinalDetailed(float degrees)
    {

        string[] caridnals = { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW", "N" };
        return caridnals[(int)Math.Round(((double)degrees * 10 % 3600) / 225)];
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
            Debug.Log($" {DateTime.Now.ToString("HH: mm:ss tt")} Location disabled");
            yield break;
        }

        // Start service before querying location with accuracy 1 meter
        Input.location.Start(0.5f, 0.1f);
        Debug.Log($"{DateTime.Now.ToString("HH: mm:ss tt")} Fetching Location..");
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
            Debug.Log($"{DateTime.Now.ToString("HH:mm:ss tt")} Location Timed out");
            yield break;
        }

        // Connection has failed
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.Log($"{DateTime.Now.ToString("HH:mm:ss tt")} Unable to determine device location");
            yield break;

        }
        else
        {
            var loc = Input.location.lastData;
            currentLocation = new LocationData(loc.latitude, loc.longitude);
            gpsLocationInitialized = true;
            Input.compass.enabled = true;
        }

    }

    private void OnDisable()
    {
        Input.location.Stop();
        Debug.Log($"{DateTime.Now.ToString("HH:mm:ss tt")} Location tracking stopped");
    }

    private double DistanceBetween2GeoobjectsInM(double lat1, double long1, double lat2, double long2)
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
