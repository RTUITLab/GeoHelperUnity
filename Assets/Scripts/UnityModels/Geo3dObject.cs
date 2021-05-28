using System;
using System.Collections.Generic;
using ServerModels;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;


namespace UnityModels
{
    [Serializable]
    public class Geo3dObject: GeoObject
    {
        public Geo3dObject(): base()
        {
            
        }
        [SerializeField]
        public String url;
        
        
        private static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

        private ARRaycastManager m_RaycastManager;
        
        void Awake()
        {
            m_RaycastManager = GameObject.FindObjectOfType<ARRaycastManager>();
        }

        private void OnTriggerEnter(Collider other)
        {
            try
            {
                if (other.CompareTag("MainCamera"))
                {
                    var touchPosition = new Vector2(Screen.width / 2, Screen.height / 2);
                    if (m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneWithinPolygon))
                    {
                        // Raycast hits are sorted by distance, so the first one
                        // will be the closest hit.
                        var hitPose = s_Hits[0].pose;

                        var hitPose_y = hitPose.position.y;

                        var currentPosition = GetComponent<Transform>().position;
                        currentPosition.y = hitPose_y;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

        }        
        
        private void OnTriggerStay(Collider other)
        {
            try
            {
                var currentPosition = GetComponent<Transform>().position;
                if (other.CompareTag("MainCamera") && currentPosition.y.Equals(0))
                {
                    var touchPosition = new Vector2(Screen.width / 2, Screen.height / 2);
                    if (m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneWithinPolygon))
                    {
                        // Raycast hits are sorted by distance, so the first one
                        // will be the closest hit.
                        var hitPose = s_Hits[0].pose;

                        var hitPose_y = hitPose.position.y;
                        
                        currentPosition.y = hitPose_y;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

        }

        public void Initialize(Geo3dObjectModel geo3dObjectModel)
        {
            id = geo3dObjectModel.id;
            url = geo3dObjectModel.url;
            gpsLocation = new LocationDataModel(geo3dObjectModel.position.lat, geo3dObjectModel.position.lng);
            Debug.Log("initialized " + geo3dObjectModel.name);
        }
    }
}