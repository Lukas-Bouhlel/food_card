using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class DishSwitcher : MonoBehaviour
{
    [Header("Configuration des Plats")]
    public GameObject[] dishPrefabs;

    [Header("Taille réelle des plats (en mètres)")]
    [Tooltip("Largeur cible par défaut si aucune valeur spécifique n'est définie.")]
    public float defaultDishWidthMeters = 0.35f;

    [Tooltip("Largeur cible pour chaque plat, dans le même ordre que Dish Prefabs. Ex: 0.32 = 32 cm")]
    public float[] dishTargetWidthsMeters;

    [Tooltip("Multiplicateur global final si tu veux tout agrandir/réduire un peu.")]
    public float globalScaleMultiplier = 1f;

    [Header("AR")]
    public ARRaycastManager raycastManager;

    [Header("Paramètres de Swipe")]
    public float swipeThreshold = 50f;

    [Header("Rotation")]
    public bool keepObjectUpright = true;

    private GameObject currentDishInstance;
    private int currentIndex = 0;
    private bool isDishPlaced = false;

    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;

    private Pose currentPlacementPose;
    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Update()
    {
        HandleTouchInput();
        HandleMouseInput();
    }

    void HandleTouchInput()
    {
        if (Input.touchCount == 0)
            return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            startTouchPosition = touch.position;
            endTouchPosition = touch.position;
        }

        if (touch.phase == TouchPhase.Ended)
        {
            endTouchPosition = touch.position;
            ProcessInput(endTouchPosition);
        }
    }

    void HandleMouseInput()
    {
        // Test dans l'éditeur
        if (Input.GetMouseButtonDown(0))
        {
            startTouchPosition = Input.mousePosition;
            endTouchPosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            endTouchPosition = Input.mousePosition;
            ProcessInput(endTouchPosition);
        }
    }

    void ProcessInput(Vector2 screenPosition)
    {
        float swipeDistance = endTouchPosition.x - startTouchPosition.x;

        // Swipe horizontal
        if (Mathf.Abs(swipeDistance) > swipeThreshold)
        {
            if (!isDishPlaced || dishPrefabs == null || dishPrefabs.Length == 0)
                return;

            if (swipeDistance > 0)
                ShowPrevious();
            else
                ShowNext();
        }
        else
        {
            // Tap/clic simple = placer ou déplacer
            TryPlaceOrMoveDish(screenPosition);
        }
    }

    void TryPlaceOrMoveDish(Vector2 screenPosition)
    {
        if (raycastManager == null)
        {
            Debug.LogError("ARRaycastManager non assigné dans l'Inspector.");
            return;
        }

        if (dishPrefabs == null || dishPrefabs.Length == 0)
        {
            Debug.LogError("Aucun prefab de plat assigné.");
            return;
        }

        if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;

            Quaternion finalRotation = keepObjectUpright ? GetUprightRotation() : hitPose.rotation;
            currentPlacementPose = new Pose(hitPose.position, finalRotation);

            if (!isDishPlaced)
            {
                ShowDish(currentIndex);
                isDishPlaced = true;
            }
            else
            {
                currentDishInstance.transform.SetPositionAndRotation(
                    currentPlacementPose.position,
                    currentPlacementPose.rotation
                );
            }
        }
        else
        {
            Debug.Log("Aucun plan détecté à cet endroit.");
        }
    }

    Quaternion GetUprightRotation()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return Quaternion.identity;

        Vector3 forward = cam.transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        return Quaternion.LookRotation(forward.normalized);
    }

    public void ShowNext()
    {
        if (dishPrefabs == null || dishPrefabs.Length == 0 || !isDishPlaced)
            return;

        currentIndex = (currentIndex + 1) % dishPrefabs.Length;
        ShowDish(currentIndex);
    }

    public void ShowPrevious()
    {
        if (dishPrefabs == null || dishPrefabs.Length == 0 || !isDishPlaced)
            return;

        currentIndex--;
        if (currentIndex < 0)
            currentIndex = dishPrefabs.Length - 1;

        ShowDish(currentIndex);
    }

    void ShowDish(int index)
    {
        if (currentDishInstance != null)
        {
            Destroy(currentDishInstance);
        }

        currentDishInstance = Instantiate(
            dishPrefabs[index],
            currentPlacementPose.position,
            currentPlacementPose.rotation
        );

        ApplyRealisticScale(currentDishInstance, index);
    }

    void ApplyRealisticScale(GameObject obj, int index)
    {
        float targetWidth = defaultDishWidthMeters;

        if (dishTargetWidthsMeters != null &&
            index >= 0 &&
            index < dishTargetWidthsMeters.Length &&
            dishTargetWidthsMeters[index] > 0.001f)
        {
            targetWidth = dishTargetWidthsMeters[index];
        }

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogWarning("Aucun Renderer trouvé sur le prefab : impossible d'ajuster automatiquement la taille.");
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        // Largeur horizontale réelle de l'objet posé sur la table
        float currentWidth = Mathf.Max(bounds.size.x, bounds.size.z);

        if (currentWidth <= 0.0001f)
        {
            Debug.LogWarning("Largeur du modèle trop petite ou invalide.");
            return;
        }

        float scaleFactor = (targetWidth / currentWidth) * globalScaleMultiplier;
        obj.transform.localScale *= scaleFactor;

        Debug.Log("Plat ajusté à environ " + targetWidth + " m de large.");
    }
}