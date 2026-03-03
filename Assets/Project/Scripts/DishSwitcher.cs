using UnityEngine;

public class DishSwitcher : MonoBehaviour
{
    [Header("Configuration des Plats")]
    public GameObject[] dishPrefabs;
    public Transform spawnPoint;

    private GameObject currentDishInstance;
    private int currentIndex = 0;
    private bool isFirstDishPlaced = false; // Permet de savoir si la table est vide

    [Header("Paramètres de Swipe")]
    public float swipeThreshold = 50f;
    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;

    void Start()
    {
        // ON NE FAIT PLUS RIEN ICI. 
        // La scène commence totalement vide, aucun prefab n'est instancié.
    }

    void Update()
    {
        DetectSwipe();
    }

    void DetectSwipe()
    {
        // 1. Détection Tactile (Téléphone)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                startTouchPosition = touch.position;
                endTouchPosition = touch.position;
            }

            if (touch.phase == TouchPhase.Ended)
            {
                endTouchPosition = touch.position;
                CheckSwipeDirection();
            }
        }

        // 2. Détection Souris (Ordinateur)
        if (Input.GetMouseButtonDown(0))
        {
            startTouchPosition = Input.mousePosition;
            endTouchPosition = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(0))
        {
            endTouchPosition = Input.mousePosition;
            CheckSwipeDirection();
        }
    }

    void CheckSwipeDirection()
    {
        float swipeDistance = endTouchPosition.x - startTouchPosition.x;

        // Si c'est un vrai swipe (gauche ou droite)
        if (Mathf.Abs(swipeDistance) > swipeThreshold)
        {
            if (swipeDistance > 0) ShowPrevious();
            else ShowNext();
        }
        // Si c'est juste un "clic" (tap) et que la table est vide
        else if (!isFirstDishPlaced)
        {
            // On affiche le premier plat au simple toucher
            ShowDish(currentIndex);
            isFirstDishPlaced = true;
        }
    }

    public void ShowNext()
    {
        if (dishPrefabs.Length == 0) return;
        currentIndex = (currentIndex + 1) % dishPrefabs.Length;
        ShowDish(currentIndex);
        isFirstDishPlaced = true;
    }

    public void ShowPrevious()
    {
        if (dishPrefabs.Length == 0) return;
        currentIndex--;
        if (currentIndex < 0) currentIndex = dishPrefabs.Length - 1;
        ShowDish(currentIndex);
        isFirstDishPlaced = true;
    }

    void ShowDish(int index)
    {
        // On détruit l'ancien SEULEMENT s'il y en a un
        if (currentDishInstance != null)
        {
            Destroy(currentDishInstance);
        }

        // On instancie le nouveau tout frais
        currentDishInstance = Instantiate(dishPrefabs[index], spawnPoint.position, spawnPoint.rotation);
        currentDishInstance.transform.SetParent(spawnPoint);
        currentDishInstance.transform.localPosition = Vector3.zero;
    }
}