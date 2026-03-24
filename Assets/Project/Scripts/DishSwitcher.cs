using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class DishSwitcher : MonoBehaviour
{
    [Header("Plats")]
    public GameObject[] dishPrefabs;

    [Header("Scale")]
    public float defaultDishWidthMeters = 0.35f;
    public float[] dishTargetWidthsMeters;
    public float globalScaleMultiplier = 1f;

    [Header("AR")]
    public ARRaycastManager raycastManager;

    [Header("Gestes")]
    public float swipeThreshold = 50f;
    public float rotationSpeed = 0.2f;
    public float rotationDeadZone = 3f;
    public bool keepObjectUpright = true;

    // -- État --
    private GameObject _dish;
    private int _index = 0;
    private bool _placed = false;
    private Pose _pose;

    // -- Geste en cours --
    private Vector2 _startPos;
    private Vector2 _lastPos;
    private bool _onDish;
    private bool _rotating;

    private static readonly List<ARRaycastHit> _hits = new();

    // -- Lifecycle --
    void Update()
    {
        HandleTouch();
        #if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.touchCount == 0) HandleMouse();
        #endif
    }

    // -- Entrées --
    void HandleTouch()
    {
        if (Input.touchCount == 0) return;
        Touch t = Input.GetTouch(0);
        if (t.phase == TouchPhase.Began) OnBegin(t.position);
        else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) OnMove(t.position);
        else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) OnEnd(t.position);
    }

    void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0)) OnBegin(Input.mousePosition);
        if (Input.GetMouseButton(0)) OnMove(Input.mousePosition);
        if (Input.GetMouseButtonUp(0)) OnEnd(Input.mousePosition);
    }

    // -- Gestes --
    void OnBegin(Vector2 pos)
    {
        _startPos = _lastPos = pos;
        _onDish = HitsDish(pos);// le doigt est-il posé sur le plat ?
        _rotating = _onDish && _placed && _dish != null;// si oui on peule rotate 
    }

    void OnMove(Vector2 pos)
    {
        if (_rotating && _dish != null)
        {
            float dx = pos.x - _lastPos.x;// déplacement horizontal du doigt
            if (Mathf.Abs(dx) >= rotationDeadZone)// ignore les micro-mouvements
            {
                // rotation du plat sur l'axe Y
                _dish.transform.Rotate(Vector3.up, -dx * rotationSpeed, Space.World);
                _pose.rotation = _dish.transform.rotation;// mémorise la nouvelle rotation
            }
        }
        _lastPos = pos;
    }

    void OnEnd(Vector2 pos)
    {
        float swipe = pos.x - _startPos.x;// distance horizontale totale

        if (!_onDish)// geste hors du plat uniquement
        {
            if (Mathf.Abs(swipe) > swipeThreshold)
                Navigate(swipe > 0 ? -1 : 1);// swipe : changer de plat
            else
                TryPlace(pos);// tap : placer / déplacer
        }
        // si le geste était sur le plat, on ne fait rien (rotation déjà appliquée dans OnMove)

        _onDish = _rotating = false;
    }

    // -- Navigation --
    void Navigate(int dir)
    {
        if (!_placed || dishPrefabs == null || dishPrefabs.Length == 0) return;
        _index = (_index + dir + dishPrefabs.Length) % dishPrefabs.Length;
        SpawnDish();
    }

    public void ShowNext() => Navigate(+1);
    public void ShowPrevious() => Navigate(-1);

    // -- Placement AR --
    void TryPlace(Vector2 screenPos)
    {
        if (dishPrefabs == null || dishPrefabs.Length == 0 || raycastManager == null) return;
        if (!raycastManager.Raycast(screenPos, _hits, TrackableType.PlaneWithinPolygon)) return;// aucun plan détecté donc euh false :)

        Pose hit = _hits[0].pose;

        // conserve la rotation actuelle si le plat est déjà placé, sinon oriente vers la caméra
        Quaternion rot = (_placed && _dish != null)
            ? _dish.transform.rotation
            : keepObjectUpright ? CameraForwardRotation() : hit.rotation;

        _pose = new Pose(hit.position, rot);

        if (!_placed) { SpawnDish(); _placed = true; }// premier placement du plat
        else _dish.transform.SetPositionAndRotation(_pose.position, _pose.rotation);// déplacement du plat
    }

    // -- Spawn & Scale des plats --
    void SpawnDish()
    {
        if (_dish != null) Destroy(_dish);// supprime l'ancien plat
        _dish = Instantiate(dishPrefabs[_index], _pose.position, _pose.rotation);
        ScaleDish(_dish);
    }

    void ScaleDish(GameObject obj)
    {
        // largeur cible : valeur spécifique au plat, ou valeur par défaut
        float target = (dishTargetWidthsMeters != null &&
                        _index < dishTargetWidthsMeters.Length &&
                        dishTargetWidthsMeters[_index] > 0.001f)
            ? dishTargetWidthsMeters[_index]
            : defaultDishWidthMeters;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        // calcule les bounds combinées de tous les renderers du prefab
        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);

        float width = Mathf.Max(bounds.size.x, bounds.size.z); // largeur réelle du modèle (X ou Z)
        if (width < 0.0001f) return;

        obj.transform.localScale *= (target / width) * globalScaleMultiplier; // applique le scale
    }

    // -- Utilitaires --
    Quaternion CameraForwardRotation()
    {
        if (Camera.main == null) return Quaternion.identity;
        Vector3 forward = Camera.main.transform.forward;
        forward.y = 0f;// ignore l'inclinaison verticale pour avoir uniquement l'inclinaison des plats en horizontale
        return forward.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(forward.normalized)
            : Quaternion.identity;
    }

    // -- Vérifie si le doigt est posé sur le plat --
    bool HitsDish(Vector2 screenPos)
    {
        if (_dish == null || Camera.main == null) return false;
        // rayon depuis l'écran vers la scène : touche-t-il le plat ou l'un de ses enfants ?
        return Physics.Raycast(Camera.main.ScreenPointToRay(screenPos), out RaycastHit hit) &&
               (hit.transform == _dish.transform || hit.transform.IsChildOf(_dish.transform));
    }
}