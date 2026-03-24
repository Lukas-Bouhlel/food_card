using NUnit.Framework;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

[System.Serializable]
public class DishData
{
    public string dishName;
    [TextArea] public string description;
    public string price;
}

public class FoodcardFiller : MonoBehaviour
{
    [SerializeField]
    private FoodCard foodcardPrefab;
    [SerializeField]
    private Transform contentParent;

    [SerializeField]
    private List<DishData> dishes;



    void Start()
    {
        foreach (DishData dish in dishes)
        {
            FoodCard card = Instantiate(foodcardPrefab, contentParent);
            card.SetData(dish);
        }
    }

    void Update()
    {
        
    }
}
