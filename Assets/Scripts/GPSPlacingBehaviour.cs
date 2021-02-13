using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using UnityEngine.XR.ARFoundation;
using System.Linq;
using Newtonsoft.Json;
using ServerModels;
using TMPro;
using UnityModels;
using Object = UnityEngine.Object;

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


    const int maxDistanceToPOIGeoobject = 20;

    /// <summary>
    /// Accuracy used for checking of changed position of object
    /// </summary>
    private const int accuracyOfPlacingObjectToSceneInM = 5;

    private LocationDataModel currentLocation = null;

    /// <summary>
    /// semaphore for sequental requests to server and trying placing objects to scene
    /// </summary>
    private static bool isSceneReadyToChange = true;

    private bool gpsLocationInitialized = false;

    private bool isLocalOriginForGpsEncoderSet = false;

    private WebSocketsBehaviour webSockets;

    /// <summary>
    /// Contains objects in scene, where key - id of object from server, value - ref to unity object
    /// </summary>
    private static Dictionary<string, GameObject> geoObjectsInScene = new Dictionary<string, GameObject>();

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

    private GameObject _arSessionOrigin = null;

    private Camera _mainCamera = null;

    [Header("Debug mode")] [SerializeField]
    private bool isDebug = false;

    [SerializeField] private float fakeCompassTrueHeading;
    [SerializeField] private LocationDataModel fakeCurrentLocationDataModel = null;

    private void Start()
    {
        DetermineApplicationPlatform();

        _arSessionOrigin = GameObject.FindWithTag("ARSessionOrigin");
        _mainCamera = Camera.main;

        GPSEncoder.Init();

        //_arSessionOrigin.transform.rotation = Quaternion.Euler(0, -GetCompassTrueHeading(), 0);
        RunGpsTracking();

        webSockets = GetComponent<WebSocketsBehaviour>();
        offsetFromTrue = GetCompassTrueHeading();
        ToNorth.transform.rotation = Quaternion.Euler(0, GetCompassTrueHeading(), 0);
        Debug.Log($"offsetFromTrue = {offsetFromTrue}");
        _arSessionOrigin.transform.rotation = Quaternion.Euler(0, GetCompassTrueHeading(), 0);
    }

    /// <summary>
    /// Set debug mode to different platforms
    /// </summary>
    private void DetermineApplicationPlatform()
    {
#if UNITY_EDITOR
        isDebug = true;

#elif UNITY_IOS
            isDebug = false;

#else
            isDebug = false;
#endif
    }

    private void Update()
    {
        // force geoObjects look at camera
        foreach (GameObject gameObj in geoObjectsInScene.Values)
        {
            gameObj.transform.LookAt(_mainCamera.transform);
        }

        // _currentTimer += Time.deltaTime;
        // if (_currentTimer > 1f)
        // {
            LocationDataModel locInfo = GetUserLocationData();
            UpdateGeoObjectsPositions(locInfo.lat, locInfo.lng);
            // _currentTimer = 0;
        // }

        //if (Input.location.status == LocationServiceStatus.Running &&
        //    _currentTimer > LOCATION_PING)
        //{
        //    LocationData locInfo = GetUserLocationData();
        //    Vector3 locInfoPos = ConvertGpsLocationToUnityLocation(locInfo.latitude, locInfo.longitude);
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
        if (WebSocketsBehaviour.GetWsConnectionState() == "Open" && isSceneReadyToChange && currentLocation != null)
        {
            isSceneReadyToChange = false;
            await TestPlacingObjects();
            isSceneReadyToChange = true;
            if (firstIterFlag)
            {
                firstIterFlag = false;
                _arSessionOrigin.transform.rotation = Quaternion.Euler(0, GetCompassTrueHeading(), 0);
            }

            if (compasTimer == 0)
            {
                // Debug.Log(GetCompassTrueHeading());
                compasTimer = 15;
            }
            else
                compasTimer--;
        }
    }

    private void UpdateGeoObjectsPositions(float lat, float lng)
    {
        try
        {
            // GPSEncoder.SetLocalOrigin(new Vector2(lat, lng));
            // _arSessionOrigin.transform.position = GPSEncoder.GPSToUCS(lat, lng);

            // if (Input.compass.enabled)
            // {
            //     _arSessionOrigin.transform.rotation = Quaternion.Euler(0, -Input.compass.magneticHeading, 0);
            //     //Debug.Log($"True heading {((int)GetCompassTrueHeading()).ToString() + "° " + DegreesToCardinalDetailed(GetCompassTrueHeading())}");
            //     Debug.Log($"{DateTime.Now.ToString("HH:mm:ss tt")} Updated Magnetic heading {(-(int)Input.compass.magneticHeading).ToString() + "° " + DegreesToCardinalDetailed(Input.compass.magneticHeading)}");
            //     //Debug.Log($"Raw vector {Input.compass.rawVector.ToString()}");
            // }

            foreach (KeyValuePair<string, GameObject> geoObjectPair in geoObjectsInScene)
            {
                string geoObjectId = geoObjectPair.Key;
                GeoObject geoObject = geoObjectPair.Value.GetComponent<GeoObject>();

                Vector3 positionOfGeoObject = GPSEncoder.GPSToUCS(geoObject.gpsLocation.lat,
                    geoObject.gpsLocation.lng);

                if (geoObject is GeoPoiTextObject geoPoiTextObject)
                {
                    // if distance to POI greater then maxDistanceToPOIGeoObject units then normalize to maxDistanceToPOIGeoObject
                    double diffInMBetweenUserAndGpsOfObject = DistanceBetween2GeoobjectsInM(lat, lng,
                        geoObject.gpsLocation.lat, geoObject.gpsLocation.lng);
                    float diffInMBetweenPrevObjV3LocAndNewV3LocOfObject = Vector3
                        .Distance(geoPoiTextObject.transform.position, positionOfGeoObject);

                    if (accuracyOfPlacingObjectToSceneInM <= diffInMBetweenUserAndGpsOfObject &&
                        diffInMBetweenUserAndGpsOfObject <= maxDistanceToPOIGeoobject &&
                        accuracyOfPlacingObjectToSceneInM <= diffInMBetweenPrevObjV3LocAndNewV3LocOfObject)
                    {
                        geoPoiTextObject.transform.position = Vector3.Lerp(geoPoiTextObject.transform.position,
                            positionOfGeoObject, 1f * Time.deltaTime);
                    }

                    else if (diffInMBetweenUserAndGpsOfObject > maxDistanceToPOIGeoobject)
                    {
                        // TODO TEST: Checking influence of this repositioning of object

                        var newPosition = (positionOfGeoObject
                                           - _mainCamera.transform.position).normalized * maxDistanceToPOIGeoobject
                                          + _mainCamera.transform.position;

                        geoPoiTextObject.transform.position = Vector3.Lerp(geoPoiTextObject.transform.position,
                            newPosition, 1f * Time.deltaTime);
                    }
                    else
                    {
                        // TODO TEST: Checking influence of this repositioning of object
                        // geoPoiTextObject.transform.position = positionOfGeoObject;
                    }

                    double distanceToObject = DistanceBetween2GeoobjectsInM(lat, lng,
                        geoObject.gpsLocation.lat, geoObject.gpsLocation.lng);

                    geoPoiTextObject.distance.text = Convert.ToUInt32(distanceToObject).ToString() + " meters";
                }
                else if (geoObject is GeoAudioObject geoAudioObject)
                {
                    // TODO: Add update audio object
                }
                else if (geoObject is Geo3dObject geo3dObject)
                {
                    // TODO: Add update 3d object
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.ToString());
        }
    }


    private async Task TestPlacingObjects()
    {
        LocationDataModel lastKnownLocation = GetUserLocationData();

        currentLocationLog.text =
            $"Current location is lat: {lastKnownLocation.lat}, lng: {lastKnownLocation.lng} Compass: " +
            $"{((int) GetCompassTrueHeading()).ToString() + "° " + DegreesToCardinalDetailed(GetCompassTrueHeading())}";

        // if current location changed then update positions of all geoObjects in scene
        if (DistanceBetween2GeoobjectsInM(lastKnownLocation.lat,
            lastKnownLocation.lng, currentLocation.lat, currentLocation.lng) > 20)
        {
            currentLocation = lastKnownLocation;
            Debug.Log($" {DateTime.Now:HH: mm:ss tt} UpdateGeoObjectsPositions");
            UpdateGeoObjectsPositions(lastKnownLocation.lat, lastKnownLocation.lng);
        }

        if (!webSockets)
            return;

        // !IMPORTANT>DO NOT CHANGE FORM OF REQUEST STRING !!!
        // string for request objects to place to scene
        string reqString = "{ " + $"\"lat\": {currentLocation.lat}, \"lng\": {currentLocation.lng}" + "}";

        string responseData = await webSockets.ReceiveObjectsFromServer(reqString);

        // Debug.Log(responseData);

        if (responseData == "" || responseData.StartsWith("{\"success\":true,\"message\":\"Connection established\"}"))
            return;


        // Debug.Log($"Got response: {responseData}");

        // json response from server for user location request
        ResponseFromServerLocationDataModel response =
            JsonConvert.DeserializeObject<ResponseFromServerLocationDataModel>(responseData);

        if (response == null || !response.success)
        {
            Debug.Log("Failed to load object from server");
            return;
        }

        List<IGeoObjectModel> packGeoObjectsFromServer = new List<IGeoObjectModel>();

        if (response.geoAudioObjectModels?.Any() == true)
        {
            packGeoObjectsFromServer.AddRange(response.geoAudioObjectModels);
        }

        if (response.poiObjectModels?.Any() == true)
        {
            packGeoObjectsFromServer.AddRange(response.poiObjectModels);
        }

        if (response.geo3dObjectModels?.Any() == true)
        {
            packGeoObjectsFromServer.AddRange(response.geo3dObjectModels);
        }

        if (!(response.geoAudioObjectModels?.Any() == true
              || response.poiObjectModels?.Any() == true
              || response.geo3dObjectModels?.Any() == true))
        {
            Debug.Log("Objects not found in this location");
            await DeleteObjectsFromScene(packGeoObjectsFromServer);
            return;
        }


        await DeleteObjectsFromScene(packGeoObjectsFromServer);


        // GPSEncoder.SetLocalOrigin(new Vector2(currentLocation.lat, currentLocation.lng));
        // _arSessionOrigin.transform.position =
        //     GPSEncoder.GPSToUCS(currentLocation.lat, currentLocation.lng);

        // if (Input.compass.enabled)
        // {
        //     _arSessionOrigin.transform.rotation = Quaternion.Euler(0, -Input.compass.magneticHeading, 0);
        //     //Debug.Log($"True heading {((int)GetCompassTrueHeading()).ToString() + "° " + DegreesToCardinalDetailed(GetCompassTrueHeading())}");
        //     Debug.Log($" {DateTime.Now.ToString("HH:mm:ss tt")} Updated Magnetic heading {(-(int)Input.compass.magneticHeading).ToString() + "° " + DegreesToCardinalDetailed(Input.compass.magneticHeading)}");
        //     //Debug.Log($"Raw vector {Input.compass.rawVector.ToString()}");
        // }


        // 

        await AddNewObjectsToScene(packGeoObjectsFromServer);
    }


    /// <summary>
    /// Delete objects from scene, which not found in pack of geoObjects from server
    /// </summary>
    /// <param name="geoObjects"></param>
    /// <returns></returns>
    private static async Task DeleteObjectsFromScene(IEnumerable<IGeoObjectModel> geoObjects)
    {
        List<IGeoObjectModel> geoObjectModels = geoObjects.ToList();

        List<string> deleteObjectsIds = geoObjectsInScene.Keys
            .Where(objectId => geoObjectModels.All(geo => geo.id != objectId))
            .ToList();

        foreach (string geoObjectId in deleteObjectsIds)
        {
            Object delObj = geoObjectsInScene[geoObjectId];

            Destroy(delObj);

            geoObjectsInScene.Remove(geoObjectId);
        }
    }


    /// <summary>
    /// Determine and add geoObjects, which must be added to scene
    /// </summary>
    /// <param name="geoObjects"></param>
    /// <returns></returns>
    private async Task AddNewObjectsToScene(IEnumerable<IGeoObjectModel> geoObjects)
    {
        foreach (IGeoObjectModel geoObjectModel in geoObjects)
        {
            if (geoObjectsInScene.ContainsKey(geoObjectModel.id))
                continue;
            // adding geoObjects of type "text" to scene and init content of geoObject(point of interest)
            if (geoObjectModel is GeoPoiObjectModel geoPoiObjectModel)
            {
                Vector3 objectPlace =
                    GPSEncoder.GPSToUCS(geoPoiObjectModel.position.lat, geoPoiObjectModel.position.lng);

                // if distance to POI greater then maxDistanceToPOIGeoObject units then normalize to maxDistanceToPOIGeoObject
                double diffInMBetweenUserAndGpsOfObject = DistanceBetween2GeoobjectsInM(currentLocation.lat,
                    currentLocation.lng, geoPoiObjectModel.position.lat, geoPoiObjectModel.position.lng);

                if (diffInMBetweenUserAndGpsOfObject > maxDistanceToPOIGeoobject)
                {
                    objectPlace = objectPlace.normalized * maxDistanceToPOIGeoobject;
                }

                GameObject newGameObject =
                    Instantiate(POI_object_text, objectPlace, Quaternion.identity) as GameObject;

                newGameObject.transform.LookAt(_mainCamera.transform);
                newGameObject.transform.SetParent(ToNorth.transform);
                newGameObject.tag = nameof(GeoPoiTextObject);

                // init content of geoObject(point of interest)
                newGameObject.GetComponent<GeoPoiTextObject>()
                    .Initialize(geoPoiObjectModel, diffInMBetweenUserAndGpsOfObject);
                Debug.Log($" {DateTime.Now:HH:mm:ss tt} Placed object {geoPoiObjectModel.name} " +
                          $"at location lat: {geoPoiObjectModel.position.lat}, lng: {geoPoiObjectModel.position.lng}. " +
                          $"\n Updated Distance to {diffInMBetweenUserAndGpsOfObject}m");

                // add to dict of initedGeoObjects
                geoObjectsInScene.Add(geoObjectModel.id, newGameObject);
            }
            else if (geoObjectModel is GeoAudioObjectModel geoAudioObjectModel)
            {
                // TODO: Add adding audio object
            }
            else if (geoObjectModel is Geo3dObjectModel geo3dObjectModel)
            {
                // TODO: Add adding 3d object
            }
        }
    }

    private string DegreesToCardinalDetailed(float degrees)
    {
        string[] caridnals =
            {"N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW", "N"};
        return caridnals[(int) Math.Round(((double) degrees * 10 % 3600) / 225)];
    }

    private void RunGpsTracking()
    {
        if (isDebug)
        {
            currentLocation = GetUserLocationData();
            gpsLocationInitialized = true;
            return;
        }

        if (!gpsLocationInitialized)
        {
            StartCoroutine(FetchLocationData());
        }
    }

    private float GetCompassTrueHeading()
    {
        if (!isDebug)
            return Input.compass.trueHeading;
        return fakeCompassTrueHeading;
    }

    private LocationDataModel GetUserLocationData()
    {
        Vector2 filteredLocation;
        if (isDebug)
        {
            filteredLocation = new Vector2(fakeCurrentLocationDataModel.lat, fakeCurrentLocationDataModel.lng);
        }
        else
        {
            var lat = Input.location.lastData.latitude;
            var lng = Input.location.lastData.longitude;
            filteredLocation = GPSEncoder
                .GetFilteredVector2(lat, lng, Input.location.lastData.horizontalAccuracy,
                    Input.location.lastData.timestamp * 1000);
        }

        if (!isLocalOriginForGpsEncoderSet)
        {
            GPSEncoder.SetLocalOrigin(filteredLocation);
            isLocalOriginForGpsEncoderSet = true;
        }

        return new LocationDataModel(filteredLocation.x, filteredLocation.y);
    }


    private IEnumerator FetchLocationData()
    {
        // First, check if user has location service enabled
        if (!Input.location.isEnabledByUser)
        {
            Debug.Log($" {DateTime.Now:HH: mm:ss tt} Location disabled");
            yield break;
        }

        // Start service before querying location with accuracy 1 meter
        Input.location.Start(.1f, .1f);
        Debug.Log($"{DateTime.Now:HH: mm:ss tt} Fetching Location..");
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
            Debug.Log($"{DateTime.Now:HH:mm:ss tt} Location Timed out");
            yield break;
        }

        // Connection has failed
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.Log($"{DateTime.Now:HH:mm:ss tt} Unable to determine device location");
            yield break;
        }
        else
        {
            currentLocation = GetUserLocationData();
            gpsLocationInitialized = true;
            Input.compass.enabled = true;
        }
    }


    private void OnDisable()
    {
        if (!isDebug)
        {
            Input.location.Stop();
            Debug.Log($"{DateTime.Now:HH:mm:ss tt} Location tracking stopped");
        }
    }

    private double DistanceBetween2GeoobjectsInM(double lat1, double long1, double lat2, double long2)
    {
        double _eQuatorialEarthRadius = 6378.1370D;
        double _d2r = (Math.PI / 180D);

        double dlong = (long2 - long1) * _d2r;
        double dlat = (lat2 - lat1) * _d2r;
        double a = Math.Pow(Math.Sin(dlat / 2D), 2D) +
                   Math.Cos(lat1 * _d2r) * Math.Cos(lat2 * _d2r) * Math.Pow(Math.Sin(dlong / 2D), 2D);
        double c = 2D * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1D - a));
        double d = _eQuatorialEarthRadius * c;

        return d * 1000;
    }
}