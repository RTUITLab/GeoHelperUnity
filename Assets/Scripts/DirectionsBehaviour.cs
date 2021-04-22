using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using EnvironmentConstants;
using Newtonsoft.Json;
using ServerModels;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[RequireComponent(typeof(GPSPlacingBehaviour))]
public class DirectionsBehaviour : MonoBehaviour
{
    private string _serverApiString = null;
    private Camera _mainCamera = null;
    public GameObject lineRenderPrefab;
    private string _authToken = null;
    private string _authLogin = null;
    private string _authPassword = null;
    private LocationDataModel _currentGpsLocation = null;
    private List<Step> _directionSteps = null;
    private LineRenderer _lineRenderer = null;
    private bool _userArrivedToPlace = false;
    public Dropdown objectToNavigateDropdown;
    public TMP_Text displayDropdownMessage;
    private GPSPlacingBehaviour _gpsPlacingBehaviour;
    private Dictionary<string, string> _geoObjectsNamesInScene = null;
    private string _selectedGeoObjectIdInNavigationDropDown = null;
    public GameObject createDirectionLineBtn;
    private const int _maxDistanceToClosestObjectInDirectionPoint = 3;
    
    private void Start()
    {
        _serverApiString = LocalEnvironment.SERVER_API;
        _authLogin = LocalEnvironment.AUTH_USERNAME;
        _authPassword = LocalEnvironment.AUTH_PASSWORD;
        _gpsPlacingBehaviour = GameObject.FindObjectOfType<GPSPlacingBehaviour>();
        _mainCamera = Camera.main;
        
        StartCoroutine(GetAuthToken());
    }

    private void InitNavigationDropdown()
    {
        _geoObjectsNamesInScene = _gpsPlacingBehaviour.GetGeoObjectsNamesInScene();
        _selectedGeoObjectIdInNavigationDropDown = null;
        
        objectToNavigateDropdown.options.Clear();
        var FirstName = new List<string>();
        FirstName.Add("Please select object");
        objectToNavigateDropdown.AddOptions(FirstName);
        if (_geoObjectsNamesInScene != null)
        {
            objectToNavigateDropdown.AddOptions(_geoObjectsNamesInScene.Values.ToList());
            displayDropdownMessage.text = "Succeed loading names";
            createDirectionLineBtn.SetActive(true);
        }

    }

    public void Dropdown_IndexChanged(int index)
    {
        if (index == 0)
        {
            displayDropdownMessage.text = "Please select object";
            createDirectionLineBtn.SetActive(false);
            return;
        }

        if (_geoObjectsNamesInScene != null)
        {
            createDirectionLineBtn.SetActive(true);
            _selectedGeoObjectIdInNavigationDropDown = 
                _geoObjectsNamesInScene.Keys.ToList().ElementAt(index - 1);
        }
            
    }

    private void Update()
    {
        if (_geoObjectsNamesInScene == null)
        {
            InitNavigationDropdown();
        }
        
        if (_lineRenderer != null 
            && _lineRenderer.positionCount > 0)
        {
            if (_lineRenderer.positionCount == 1)
            {
                _userArrivedToPlace = true;
                _lineRenderer.positionCount--;
                displayDropdownMessage.text = "Вы успешно дошли до пункта назначения";
            }
            else if (_lineRenderer.positionCount >= 2)
            {
                var positions = new Vector3[_lineRenderer.positionCount];
                _lineRenderer.GetPositions(positions);
                var distanceToClosestPoint = Vector3.Distance(_mainCamera.transform.position, positions[1]);
                if (distanceToClosestPoint < _maxDistanceToClosestObjectInDirectionPoint)
                { 
                    var positionsList = positions.ToList();
                    positionsList.RemoveAt(0);
                    _lineRenderer.positionCount--;
                    _lineRenderer.SetPositions(positionsList.ToArray());
                }
            }
        }

    }

    public void StartGettingDirection()
    {
        string geoObjectId = _selectedGeoObjectIdInNavigationDropDown;

        _userArrivedToPlace = false;

        _currentGpsLocation = _gpsPlacingBehaviour.GetCurrentLocationData();
        
        StartCoroutine(GetDirection(geoObjectId, _currentGpsLocation));
    }

    IEnumerator GetAuthToken()
    {
        var jsonData = "{\"username\": \"" + _authLogin + "\", \"password\": \"" + _authPassword + "\" }";
        string requestString = _serverApiString + "auth";

        var www = new UnityWebRequest(requestString, "POST");
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
        www.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
        www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();
        
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(www.error);
            yield break; 
        }
        string responseText = www.downloadHandler.text;
        ResponseFromServerToLoginModel response = null;
        try
        {
            response = JsonConvert
                .DeserializeObject<ResponseFromServerToLoginModel>(responseText);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            yield break; 
        }
        
        _authToken = response.token;
    }
    IEnumerator GetDirection(string geoObjectId, LocationDataModel location)
    {
        if (_authToken == null)
        {
            yield return new WaitForSeconds(1);
        }
         
         string requestString = _serverApiString + "direction";
         var uriBuilder = new UriBuilder(requestString);
         NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
         queryString.Add("lat", location.lat.ToString());
         queryString.Add("lng", location.lng.ToString());
         queryString.Add("objectId", geoObjectId);
         uriBuilder.Query = queryString.ToString();
         // requestString = uriBuilder.ToString();
         requestString += "?" + queryString.ToString();
         UnityWebRequest www = UnityWebRequest.Get(requestString);
         www.SetRequestHeader("Authorization", "Bearer " + _authToken);
         
         yield return www.SendWebRequest();
         
         if (www.result != UnityWebRequest.Result.Success) {
             Debug.LogError(www.error);
             displayDropdownMessage.text = "До данного объекта не получается проложить маршрут";
             yield break;
         }

        string responseText = www.downloadHandler.text;

        ResponseFromServerOneToOneDirectionModel response = null;
        try
        {
            response = JsonConvert
                .DeserializeObject<ResponseFromServerOneToOneDirectionModel>(responseText);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            yield break; 
        }
        
        Debug.Log(response);

        
        List<Step> steps = response.message._steps;
        
        SetupDirectionObject(steps);
    }

    void SetupDirectionObject(List<Step> steps)
    {
        GameObject directionGameObject = Instantiate(lineRenderPrefab);
        _lineRenderer = directionGameObject.GetComponent<LineRenderer>();
        _lineRenderer.positionCount = steps.Count;
        
        _lineRenderer.SetPosition(0, _mainCamera.transform.position);

        steps.RemoveAt(0);
        
        Vector3 positionOfLineCorner;
        foreach (var step in steps)
        {
            positionOfLineCorner = GPSEncoder.GPSToUCS(step.lat,
                step.lng);
            _lineRenderer.SetPosition(step._stepId - 1, positionOfLineCorner);
        }

        displayDropdownMessage.text = "Маршрут успешно проложен";
    }
}
