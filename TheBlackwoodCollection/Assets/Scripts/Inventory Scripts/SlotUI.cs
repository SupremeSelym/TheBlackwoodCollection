using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlotUI : MonoBehaviour
{
    public Image icon;
    //public TMP_Text quantityText;

    // Add quantity as a parameter if needed
    public void Set(ItemData data)
    {
        if (data == null)
        {
            icon.enabled = false;
            //quantityText.text = "";
            return;
        }

        icon.enabled = true;
        icon.sprite = data.icon;
        //quantityText.text = quantity > 1 ? quantity.ToString() : "";
    }
}