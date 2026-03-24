using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FoodCard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Image dishImage;
    [SerializeField] private Button viewInARButton;

    public void SetData(DishData dish, int index, DishSwitcher dishSwitcher)
    {
        typeText.text = dish.dishName;
        descriptionText.text = dish.description;
        priceText.text = dish.price;

        if (viewInARButton != null && dishSwitcher != null)
        {
            viewInARButton.onClick.RemoveAllListeners();
            viewInARButton.onClick.AddListener(() => dishSwitcher.SelectDish(index));
        }
    }
}
