using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class DishSwitcher : MonoBehaviour
{
    [Header("Configuration des Plats")]
    public GameObject[] dishPrefabs;

    [Header("Taille r�elle des plats (en m�tres)")]
    [Tooltip("Largeur cible par d�faut si aucune valeur sp�cifique n'est d�finie.")]
    public float defaultDishWidthMeters = 0.35f;

    [Tooltip("Largeur cible pour chaque plat, dans le m�me ordre que Dish Prefabs. Ex: 0.32 = 32 cm")]
    public float[] dishTargetWidthsMeters;

    [Tooltip("Multiplicateur global final si tu veux tout agrandir/r�duire un peu.")]
    public float globalScaleMultiplier = 1f;

    [Header("AR")]
    public ARRaycastManager raycastManager;

    [Header("Param�tres de Swipe")]
    public float swipeThreshold = 50f;

    [Header("Rotation")]
    public bool keepObjectUpright = true;
    [Tooltip("Sensibilit� de rotation du plat. Plus la valeur est grande, plus �a tourne vite.")]
    public float rotationSpeed = 0.2f;

    [Tooltip("Petite zone morte pour �viter les micro-mouvements involontaires.")]
    public float rotationDeadZone = 3f;

    private GameObject currentDishInstance;
    private int currentIndex = 0;
    private bool isDishPlaced = false;

    private Vector2 startTouchPosition;
    private Vector2 lastTouchPosition;

    // Le geste a commenc� sur le plat ?
    private bool gestureStartedOnDish = false;

    // On est en train de tourner le plat ?
    private bool isRotatingDish = false;

    // Le plat a r�ellement tourn� pendant ce geste ?
    private bool hasRotatedDuringGesture = false;

    private Pose currentPlacementPose;
    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Update()
    {
        HandleTouchInput();

#if UNITY_EDITOR || UNITY_STANDALONE
        // Souris uniquement dans l'�diteur / PC
        if (Input.touchCount == 0)
        {
            HandleMouseInput();
        }
#endif
    }

    void HandleTouchInput()
    {
        if (Input.touchCount == 0)
            return;

        Touch touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                BeginGesture(touch.position);
                break;

            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                ContinueGesture(touch.position);
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                EndGesture(touch.position);
                break;
        }
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            BeginGesture(Input.mousePosition);
        }

        if (Input.GetMouseButton(0))
        {
            ContinueGesture(Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0))
        {
            EndGesture(Input.mousePosition);
        }
    }

    void BeginGesture(Vector2 screenPosition)
    {
        startTouchPosition = screenPosition;
        lastTouchPosition = screenPosition;

        gestureStartedOnDish = IsTouchOnCurrentDish(screenPosition);
        isRotatingDish = gestureStartedOnDish && isDishPlaced && currentDishInstance != null;
        hasRotatedDuringGesture = false;
    }

    void ContinueGesture(Vector2 screenPosition)
    {
        if (!isRotatingDish || currentDishInstance == null)
        {
            lastTouchPosition = screenPosition;
            return;
        }

        float deltaX = screenPosition.x - lastTouchPosition.x;

        if (Mathf.Abs(deltaX) >= rotationDeadZone)
        {
            RotateDish(deltaX);
            hasRotatedDuringGesture = true;
        }

        lastTouchPosition = screenPosition;
    }

    void EndGesture(Vector2 screenPosition)
    {
        float swipeDistance = screenPosition.x - startTouchPosition.x;

        // CAS 1 : le geste a commenc� sur le plat
        // => on ne doit JAMAIS changer de prefab
        if (gestureStartedOnDish)
        {
            // Si on a tourn�, on garde juste la rotation.
            // Si c'�tait juste un tap sur le plat, on ne fait rien.
            ResetGestureState();
            return;
        }

        // CAS 2 : geste commenc� hors du plat
        if (Mathf.Abs(swipeDistance) > swipeThreshold)
        {
            if (!isDishPlaced || dishPrefabs == null || dishPrefabs.Length == 0)
            {
                ResetGestureState();
                return;
            }

            if (swipeDistance > 0)
                ShowPrevious();
            else
                ShowNext();
        }
        else
        {
            // Tap simple hors du plat = placer ou d�placer
            TryPlaceOrMoveDish(screenPosition);
        }

        ResetGestureState();
    }

    void ResetGestureState()
    {
        gestureStartedOnDish = false;
        isRotatingDish = false;
        hasRotatedDuringGesture = false;
    }

    void TryPlaceOrMoveDish(Vector2 screenPosition)
    {
        if (raycastManager == null)
        {
            Debug.LogError("ARRaycastManager non assign� dans l'Inspector.");
            return;
        }

        if (dishPrefabs == null || dishPrefabs.Length == 0)
        {
            Debug.LogError("Aucun prefab de plat assign�.");
            return;
        }

        if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            Quaternion finalRotation;

            if (!isDishPlaced)
            {
                finalRotation = keepObjectUpright ? GetUprightRotation() : hitPose.rotation;
            }
            else
            {
                if (currentDishInstance != null)
                    finalRotation = currentDishInstance.transform.rotation;
                else
                    finalRotation = keepObjectUpright ? GetUprightRotation() : hitPose.rotation;
            }

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
            Debug.Log("Aucun plan d�tect� � cet endroit.");
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

    bool IsTouchOnCurrentDish(Vector2 screenPosition)
    {
        if (currentDishInstance == null)
            return false;

        Camera cam = Camera.main;
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.transform == currentDishInstance.transform ||
                   hit.transform.IsChildOf(currentDishInstance.transform);
        }

        return false;
    }

    void RotateDish(float deltaX)
    {
        if (currentDishInstance == null)
            return;

        float angle = -deltaX * rotationSpeed;

        currentDishInstance.transform.Rotate(Vector3.up, angle, Space.World);

        // On garde cette rotation en m�moire
        currentPlacementPose.rotation = currentDishInstance.transform.rotation;
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
            Debug.LogWarning("Aucun Renderer trouv� sur le prefab : impossible d'ajuster automatiquement la taille.");
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float currentWidth = Mathf.Max(bounds.size.x, bounds.size.z);

        if (currentWidth <= 0.0001f)
        {
            Debug.LogWarning("Largeur du mod�le trop petite ou invalide.");
            return;
        }

        float scaleFactor = (targetWidth / currentWidth) * globalScaleMultiplier;
        obj.transform.localScale *= scaleFactor;

        Debug.Log("Plat ajust� � environ " + targetWidth + " m de large.");
    }
}