using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class DishSwitcher : MonoBehaviour
{
    [Header("Configuration des Plats")]
    public GameObject[] dishPrefabs;
    public Transform spawnPoint;

    [Header("Configuration AR")]
    public ARRaycastManager raycastManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private GameObject currentDishInstance;
    private int currentIndex = 0;
    private bool isFirstDishPlaced = false;

    [Header("Paramètres de Swipe")]
    public float swipeThreshold = 50f;
    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;

    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                startTouchPosition = touch.position;
                endTouchPosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                endTouchPosition = touch.position;
                ProcessInteraction(touch.position);
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            startTouchPosition = Input.mousePosition;
            endTouchPosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            endTouchPosition = Input.mousePosition;
            ProcessInteraction(Input.mousePosition);
        }
    }

    void ProcessInteraction(Vector2 interactionPosition)
    {
        float swipeDistance = endTouchPosition.x - startTouchPosition.x;

        if (Mathf.Abs(swipeDistance) > swipeThreshold)
        {
            if (isFirstDishPlaced)
            {
                if (swipeDistance > 0) ShowPrevious();
                else ShowNext();
            }
        }
        else
        {
            if (raycastManager != null && raycastManager.Raycast(interactionPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;

                spawnPoint.position = hitPose.position;

                if (!isFirstDishPlaced)
                {
                    ShowDish(currentIndex);
                    isFirstDishPlaced = true;
                }
            }
        }
    }

    public void ShowNext()
    {
        if (dishPrefabs.Length == 0) return;
        currentIndex = (currentIndex + 1) % dishPrefabs.Length;
        ShowDish(currentIndex);
    }

    public void ShowPrevious()
    {
        if (dishPrefabs.Length == 0) return;
        currentIndex--;
        if (currentIndex < 0) currentIndex = dishPrefabs.Length - 1;
        ShowDish(currentIndex);
    }

    void ShowDish(int index)
    {
        if (currentDishInstance != null) Destroy(currentDishInstance);

        currentDishInstance = Instantiate(dishPrefabs[index], spawnPoint.position, spawnPoint.rotation);

        currentDishInstance.transform.SetParent(spawnPoint, false);
        currentDishInstance.transform.localPosition = Vector3.zero;
        currentDishInstance.transform.localRotation = Quaternion.identity;
    }
}