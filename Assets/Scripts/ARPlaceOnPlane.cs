using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;


public class ARPlaceOnPlane : MonoBehaviour
{
    [SerializeField]
    private ARRaycastManager arRaycastManager;
    [SerializeField]
    private GameObject objectToPlace;

    GameObject spawnObject;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void PlaceObjectByTouch()
    {
        if(Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            if(arRaycastManager.Raycast(touch.position, hits, TrackableType.Planes))
            {
                Pose placementPose = hits[0].pose;
                if (spawnObject == null)
                {
                    spawnObject = Instantiate(objectToPlace, placementPose.position, placementPose.rotation);
                }
                else
                {
                    spawnObject.transform.SetPositionAndRotation(placementPose.position, placementPose.rotation);
                }
            }
        }
    }

    private void UpdateCenterObject()
    {
        Vector3 screenCenter = Camera.current.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
        
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        arRaycastManager.Raycast(screenCenter, hits, TrackableType.Planes);

        if(hits.Count > 0)
        {
            Pose placementPose = hits[0].pose;
            objectToPlace.SetActive(true);
            objectToPlace.transform.SetPositionAndRotation(placementPose.position, placementPose.rotation);
        }
        else
        {
            objectToPlace.SetActive(false);
        }
    }
}
