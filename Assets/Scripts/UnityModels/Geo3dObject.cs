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
        ARPlaneManager m_PlaneManager;
        
        void Start()
        {
            m_RaycastManager = GameObject.FindObjectOfType<ARRaycastManager>();
            m_PlaneManager = GameObject.FindObjectOfType<ARPlaneManager>();
        }

        private void OnTriggerEnter(Collider other)
        {
            try
            {
                Debug.Log("Got trigger enter");
                if (other.CompareTag("MainCamera"))
                {
                    Debug.Log("Collision with MainCamera");
                    var touchPosition = new Vector2(Screen.width / 2, Screen.height / 2);
                    if (!m_RaycastManager)
                    {
                        Debug.Log("m_RaycastManager not defined");
                        return;
                    }
                    if (m_RaycastManager && m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.Planes))
                    {
                        // Raycast hits are sorted by distance, so the first one
                        // will be the closest hit.
                        if (s_Hits.Count == 0)
                        {
                            Debug.Log("s_Hits is 0");
                            return;
                        }
                        var hit = s_Hits[0];
                        // Determine if it is a plane
                        if ((hit.hitType & TrackableType.Planes) != 0)
                        {
                            // Look up the plane by id
                            Debug.Log("Before planeManager");
                            if (!m_PlaneManager)
                            {
                                Debug.Log("m_PlaneManager not defined");
                                return;
                            }
                                
                            var plane = m_PlaneManager.GetPlane(hit.trackableId);
                            if (!plane)
                            {
                                Debug.Log("m_PlaneManager not defined");
                                return;
                            }
                            
                            // Do something with 'plane':
                            Debug.Log($"Hit a plane with alignment {plane.alignment} and y {plane.center.y}");
                            var hitPose_y  = plane.center.y;
                            var currentPosition = GetComponent<Transform>().position;
                            Debug.Log($"Previous pos ${currentPosition.y}");
                            currentPosition.y = hitPose_y;
                            Debug.Log($"Current pos ${currentPosition.y}");
                        }
                        else
                        {
                            // What type of thing did we hit?
                            // Debug.Log($"Raycast hit a {hit.hitType}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                Debug.LogError(e.Message);
            }

        }        
        
        private void OnTriggerStay(Collider other)
        {
            try
            {
                if (other.CompareTag("MainCamera"))
                {
                    var currentPosition = GetComponent<Transform>().position;
                    if(currentPosition.y != 0)
                        return;
                    var touchPosition = new Vector2(Screen.width / 2, Screen.height / 2);
                    if (!m_RaycastManager)
                    {
                        Debug.Log("m_RaycastManager not defined");
                        return;
                    }
                    if (m_RaycastManager && m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.Planes))
                    {
                        // Raycast hits are sorted by distance, so the first one
                        // will be the closest hit.
                        if (s_Hits.Count == 0)
                        {
                            Debug.Log("s_Hits is 0");
                            return;
                        }
                        var hit = s_Hits[0];
                        // Determine if it is a plane
                        if ((hit.hitType & TrackableType.Planes) != 0)
                        {
                            // Look up the plane by id
                            Debug.Log("Before planeManager");
                            if (!m_PlaneManager)
                            {
                                Debug.Log("m_PlaneManager not defined");
                                return;
                            }
                                
                            var plane = m_PlaneManager.GetPlane(hit.trackableId);
                            if (!plane)
                            {
                                Debug.Log("m_PlaneManager not defined");
                                return;
                            }
                            
                            // Do something with 'plane':
                            Debug.Log($"Hit a plane with alignment {plane.alignment} and y {plane.center.y}");
                            var hitPose_y  = plane.center.y;
                            
                            Debug.Log($"Previous pos ${currentPosition.y}");
                            currentPosition.y = hitPose_y;
                            Debug.Log($"Current pos ${currentPosition.y}");
                        }
                        else
                        {
                            // What type of thing did we hit?
                            // Debug.Log($"Raycast hit a {hit.hitType}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                Debug.LogError(e.Message);
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