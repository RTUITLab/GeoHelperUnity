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
using UnityEngine.Networking;
using UnityModels;
using Object = UnityEngine.Object;

[RequireComponent(typeof(WebSocketsBehaviour))]
public class GPSPlacingBehaviour : MonoBehaviour
{
    /// <summary>
    /// Prefab for geoobject type "text"
    /// </summary>
    public GameObject POI_object_text;

    public GameObject audioPrefabGameObject;

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

    /// <summary>
    ///     Frequency at which we check our device location (to save battery).
    /// </summary>
    private const float LOCATION_PING = 10f; // 10 seconds

    public GameObject ToNorth;

    private GameObject _arSessionOrigin = null;

    private Camera _mainCamera = null;

    private float maxAudioDistance = 20f;
    
    private bool notInitToNorth = true;

    /// <summary>
    /// Pack of audio on scene
    /// </summary>
    List<String> downloadedAudio;


    [Header("Debug mode")] [SerializeField]
    private bool isDebug = true;

    [SerializeField] private float fakeCompassTrueHeading;
    [SerializeField] private LocationDataModel fakeCurrentLocationDataModel = null;


    private async Task Start()
    {
        DetermineApplicationPlatform();
        //
        downloadedAudio = new List<String>();

        _arSessionOrigin = GameObject.FindWithTag("ARSessionOrigin");
        _mainCamera = Camera.main;

        GPSEncoder.Init();

        //_arSessionOrigin.transform.rotation = Quaternion.Euler(0, -GetCompassTrueHeading(), 0);
        await RunGpsTracking();

        webSockets = GetComponent<WebSocketsBehaviour>();
        // offsetFromTrue = GetCompassTrueHeading();
        // ToNorth.transform.rotation = Quaternion.Euler(0, GetCompassTrueHeading(), 0);
        // Debug.Log($"offsetFromTrue = {offsetFromTrue}");
        // _arSessionOrigin.transform.rotation = Quaternion.Euler(0, GetCompassTrueHeading(), 0);
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

        if (!IsInputLocationRunning())
            return;

        LocationDataModel locInfo = GetUserLocationData();
        UpdateGeoObjectsPositions(locInfo.lat, locInfo.lng);

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
        try
        {
            if (WebSocketsBehaviour.GetWsConnectionState() != "Open" || !isSceneReadyToChange ||
                currentLocation == null)
                return;

            isSceneReadyToChange = false;
            await TestPlacingObjects();
            isSceneReadyToChange = true;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
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

                double diffInMBetweenUserAndGpsOfObject = DistanceBetween2GeoobjectsInM(lat, lng,
                    geoObject.gpsLocation.lat, geoObject.gpsLocation.lng);
                float diffInMBetweenPrevObjV3LocAndNewV3LocOfObject = Vector3
                    .Distance(geoObject.transform.localPosition, positionOfGeoObject);

                if (geoObject is GeoPoiTextObject geoPoiTextObject)
                {
                    // if distance to POI greater then maxDistanceToPOIGeoObject units then normalize to maxDistanceToPOIGeoObject


                    if (accuracyOfPlacingObjectToSceneInM <= diffInMBetweenUserAndGpsOfObject &&
                        diffInMBetweenUserAndGpsOfObject <= maxDistanceToPOIGeoobject &&
                        accuracyOfPlacingObjectToSceneInM <= diffInMBetweenPrevObjV3LocAndNewV3LocOfObject)
                    {
                        geoPoiTextObject.transform.localPosition = Vector3.Lerp(
                            geoPoiTextObject.transform.localPosition,
                            positionOfGeoObject, 1f * Time.deltaTime);
                        // Debug.Log($" {DateTime.Now:HH:mm:ss tt} Updated object localPosition of" +
                        //           $" \"{geoObject.GetComponent<GeoPoiTextObject>().title.text}\"" +
                        //           $" at location lat: {geoObject.gpsLocation.lat}, lng: {geoObject.gpsLocation.lng}. " +
                        //           $"\n Updated V3 to {geoPoiTextObject.transform.localPosition}m");
                    }
                    else if (diffInMBetweenUserAndGpsOfObject > maxDistanceToPOIGeoobject)
                    {
                        // TODO TEST: Checking influence of this repositioning of object

                        Vector3 cameraLocalPosition = _mainCamera.transform.position;
                        Vector3 newPosition = (positionOfGeoObject
                                               - cameraLocalPosition).normalized * maxDistanceToPOIGeoobject
                                              + cameraLocalPosition;

                        geoPoiTextObject.transform.localPosition = Vector3.Lerp(
                            geoPoiTextObject.transform.localPosition,
                            newPosition, 1f * Time.deltaTime);
                        // Debug.Log($" {DateTime.Now:HH:mm:ss tt} Updated object localPosition of" +
                        //           $" \"{geoObject.GetComponent<GeoPoiTextObject>().title.text}\"" +
                        //           $" at location lat: {geoObject.gpsLocation.lat}, lng: {geoObject.gpsLocation.lng}. " +
                        //           $"\n Updated V3 to {geoPoiTextObject.transform.localPosition}m");
                    }
                    else
                    {
                        // TODO TEST: Checking influence of this repositioning of object
                        // geoPoiTextObject.transform.localPosition = positionOfGeoObject;
                    }

                    double distanceToObject = DistanceBetween2GeoobjectsInM(lat, lng,
                        geoObject.gpsLocation.lat, geoObject.gpsLocation.lng);

                    geoPoiTextObject.distance.text = Convert.ToUInt32(distanceToObject).ToString() + " meters";
                }
                else if (geoObject is GeoAudioObject geoAudioObject)
                {
                    if (accuracyOfPlacingObjectToSceneInM <= diffInMBetweenUserAndGpsOfObject &&
                        diffInMBetweenUserAndGpsOfObject <= maxDistanceToPOIGeoobject &&
                        accuracyOfPlacingObjectToSceneInM <= diffInMBetweenPrevObjV3LocAndNewV3LocOfObject)
                    {
                        geoAudioObject.transform.localPosition = Vector3.Lerp(geoAudioObject.transform.localPosition,
                            positionOfGeoObject, 1f * Time.deltaTime);
                        // Debug.Log($" {DateTime.Now:HH:mm:ss tt} Updated object localPosition of" +
                        //           $" \"{geoObject.GetComponent<GeoPoiTextObject>().title.text}\"" +
                        //           $" at location lat: {geoObject.gpsLocation.lat}, lng: {geoObject.gpsLocation.lng}. " +
                        //           $"\n Updated V3 to {geoPoiTextObject.transform.localPosition}m");
                    }

                    //float distance = (float)DistanceBetween2GeoobjectsInM(lat, lng,
                    //    geoAudioObject.gpsLocation.lat, geoAudioObject.gpsLocation.lng);
                    float distance =
                        Vector3.Distance(_mainCamera.transform.position, geoAudioObject.transform.localPosition);

                    // Debug.Log("distance = " + distance + "volume = " + geoAudioObject.GetComponent<AudioSource>().volume);
                    if (distance <= maxAudioDistance && distance != 0)
                    {
                        geoAudioObject.GetComponent<MeshRenderer>().enabled = true;
                        geoAudioObject.GetComponent<AudioSource>().volume = 1 / distance;
                    }

                    else if (distance == 0)
                    {
                        geoAudioObject.GetComponent<AudioSource>().volume = 1;
                        geoAudioObject.GetComponent<MeshRenderer>().enabled = true;
                    }
                    else
                    {
                        geoAudioObject.GetComponent<AudioSource>().volume = 0;
                        geoAudioObject.GetComponent<MeshRenderer>().enabled = false;
                    }
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
        if (!IsInputLocationRunning())
            return;

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
        ResponseFromServerLocationDataModel response = null;
        try
        {
            response =
                JsonConvert.DeserializeObject<ResponseFromServerLocationDataModel>(responseData);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }


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
        // _arSessionOrigin.transform.localPosition =
        //     GPSEncoder.GPSToUCS(currentLocation.lat, currentLocation.lng);

        // if (Input.compass.enabled)
        // {
        //     _arSessionOrigin.transform.rotation = Quaternion.Euler(0, -Input.compass.magneticHeading, 0);
        //     //Debug.Log($"True heading {((int)GetCompassTrueHeading()).ToString() + "° " + DegreesToCardinalDetailed(GetCompassTrueHeading())}");
        //     Debug.Log($" {DateTime.Now.ToString("HH:mm:ss tt")} Updated Magnetic heading {(-(int)Input.compass.magneticHeading).ToString() + "° " + DegreesToCardinalDetailed(Input.compass.magneticHeading)}");
        //     //Debug.Log($"Raw vector {Input.compass.rawVector.ToString()}");
        // }


        // 
        if(notInitToNorth)
        {
            _arSessionOrigin.transform.rotation = Quaternion.Euler(0, GetCompassTrueHeading(), 0);
            notInitToNorth = false;
        }


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

        List<string> deleteObjectsIds = null;
        if (geoObjectModels.Any() == true)
        {
            deleteObjectsIds = geoObjectsInScene.Keys
                .Where(objectId => geoObjectModels.All(geo => geo.id != objectId))
                .ToList();
        }
        else
        {
            deleteObjectsIds = geoObjectsInScene.Keys.ToList();
        }

        foreach (string geoObjectId in deleteObjectsIds)
        {
            GameObject delObj = geoObjectsInScene[geoObjectId];

            Destroy(delObj.gameObject);

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
                    // TODO TEST: Checking influence of this repositioning of object

                    Vector3 mainCameraLocalPosition = _mainCamera.transform.position;
                    objectPlace = (objectPlace
                                   - mainCameraLocalPosition).normalized * maxDistanceToPOIGeoobject
                                  + mainCameraLocalPosition;
                }

                GameObject newGameObject =
                    Instantiate(POI_object_text, objectPlace, Quaternion.identity) as GameObject;

                newGameObject.transform.LookAt(_mainCamera.transform);
                newGameObject.transform.SetParent(ToNorth.transform);
                newGameObject.tag = nameof(GeoPoiTextObject);

                // init content of geoObject(point of interest)
                // Debug.Log("!!!!!"+ newGameObject.GetComponent<GeoPoiTextObject>());

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
                if (!downloadedAudio.Contains(geoAudioObjectModel.id))
                {
                    // Debug.Log("url = " + geoAudioObjectModel.url);
                    downloadedAudio.Add(geoAudioObjectModel.id);
                    ////////////////////////
                    StartCoroutine(LoadAudioFromServer(geoAudioObjectModel.url, AudioType.MPEG, geoAudioObjectModel));
                }
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

    private async Task RunGpsTracking()
    {
        if (isDebug)
        {
            currentLocation = GetUserLocationData();
            _arSessionOrigin.transform.rotation = Quaternion.Euler(0, GetCompassTrueHeading(), 0);
            return;
        }

        if (!IsInputLocationRunning())
        {
            StartCoroutine(FetchLocationData());
        }
    }

    private bool IsInputLocationRunning()
    {
        if (isDebug)
            return true;
        return Input.location.status == LocationServiceStatus.Running;
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
            if (!IsInputLocationRunning())
                throw new Exception("Input location not initialized for getting location");
            float lat = Input.location.lastData.latitude;
            float lng = Input.location.lastData.longitude;
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

        switch (Input.location.status)
        {
            case LocationServiceStatus.Failed:
                Debug.Log($"{DateTime.Now:HH:mm:ss tt} Unable to determine device location");
                break;
            case LocationServiceStatus.Running:
                Input.compass.enabled = true;
                currentLocation = GetUserLocationData();
                break;
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

    IEnumerator LoadAudioFromServer(string url,
        AudioType audioType,
        GeoAudioObjectModel audioObj)
    {
        var request = UnityWebRequestMultimedia.GetAudioClip(url, audioType);
        // Debug.Log("1!!!");
        yield return request.SendWebRequest();


        if (!request.isHttpError && !request.isNetworkError)
        {
            //////////////////////
            Vector3 objectPlace =
                GPSEncoder.GPSToUCS(audioObj.position.lat, audioObj.position.lng);

            // if distance to POI greater then maxDistanceToPOIGeoObject units then normalize to maxDistanceToPOIGeoObject
            double diffInMBetweenUserAndGpsOfObject = DistanceBetween2GeoobjectsInM(currentLocation.lat,
                currentLocation.lng, audioObj.position.lat, audioObj.position.lng);


            GameObject newGameObject =
                Instantiate(audioPrefabGameObject, objectPlace, Quaternion.identity) as GameObject;

            newGameObject.transform.SetParent(ToNorth.transform);
            newGameObject.tag = nameof(GeoAudioObject);
            // Debug.Log("2!!!");
            AudioClip audio = DownloadHandlerAudioClip.GetContent(request);
            // Debug.Log(newGameObject.GetComponent<GeoAudioObject>() + "       ");
            // init content of geoObject(point of interest)
            newGameObject.GetComponent<GeoAudioObject>()
                .Initialize(audioObj, audio);
            // Debug.Log("3!!!");
            Debug.Log($" {DateTime.Now:HH:mm:ss tt} Placed object {audioObj.name} " +
                      $"at location lat: {audioObj.position.lat}, lng: {audioObj.position.lng}. " +
                      $"\n Updated Distance to {diffInMBetweenUserAndGpsOfObject}m");

            // add to dict of initedGeoObjects
            geoObjectsInScene.Add(audioObj.id, newGameObject);
            newGameObject.GetComponent<AudioSource>().volume = 0;
            newGameObject.GetComponent<AudioSource>().Play();

            ///////////////////////
            ///audioSource.Play();
        }
        else
        {
            Debug.LogErrorFormat("error request [{0}, {1}]", url, request.error);
        }

        request.Dispose();
    }
}