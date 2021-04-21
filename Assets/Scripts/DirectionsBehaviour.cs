using System;
using System.Collections;
using Newtonsoft.Json;
using ServerModels;
using UnityEngine;
using UnityEngine.Networking;

public class DirectionsBehaviour : MonoBehaviour
{
    private const string _serverApiString = "https://geo-helper.ga/api/v1/";
    private const string _token = "token";
    

    void StartGettingDirection()
    {
        StartCoroutine(GetDirection());
    }
    
    IEnumerator GetDirection()
    {
        string requestString = _serverApiString + "direction";
        UnityWebRequest www = UnityWebRequest.Get(_serverApiString);
        www.SetRequestHeader("Authorization", "Bearer " + _token);
        
        yield return www.SendWebRequest();
 
        if (www.result == UnityWebRequest.Result.ConnectionError) {
            Debug.LogError(www.error);
            yield break; 
        }

        ResponseFromServerOneToOneDirectionModel response = null;
        try
        {
            response = JsonConvert
                .DeserializeObject<ResponseFromServerOneToOneDirectionModel>(www.downloadHandler.text);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        
        Debug.Log(response);
    }
}
