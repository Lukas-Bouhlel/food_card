using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DishData
{
    public string dishName;
    [TextArea] public string description;
    public string price;
    public GameObject dishPrefab;
}

public class FoodcardFiller : MonoBehaviour
{
    [SerializeField] private FoodCard foodcardPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private DishSwitcher dishSwitcher;
    [SerializeField] private List<DishData> dishes;

    void Start()
    {
        // Auto-populate DishSwitcher prefabs from dish data
        if (dishSwitcher != null)
        {
            dishSwitcher.dishPrefabs = new GameObject[dishes.Count];
            for (int i = 0; i < dishes.Count; i++)
                dishSwitcher.dishPrefabs[i] = dishes[i].dishPrefab;
        }

        for (int i = 0; i < dishes.Count; i++)
        {
            FoodCard card = Instantiate(foodcardPrefab, contentParent);
            card.SetData(dishes[i], i, dishSwitcher);
        }
    }
}
