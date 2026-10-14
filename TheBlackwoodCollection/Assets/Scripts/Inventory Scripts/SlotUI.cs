using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlotUI : MonoBehaviour
{
    public Image icon;
    //public TMP_Text quantityText;

    [Header("Selection colors")]
    public Image background; // leave empty to auto-find on this object
    public Color selectedColor = new Color(1f, 0.85f, 0.2f, 1f); // yellow

    private Color normalColor = Color.gray;

    private void Awake()
    {
        if (background == null) background = GetComponent<Image>();

        if (background != null) normalColor = background.color;
        else Debug.LogWarning("SlotUI: no background Image found. Selection highlight won't show.", this);

        if (icon == null) Debug.LogError("SlotUI: 'Icon' field is not assigned.", this);
        //if (quantityText == null) Debug.LogError("SlotUI: 'Quantity Text' field is not assigned.", this);
    }

    public void Set(ItemData data)
    {
        //if (icon == null || quantityText == null) return;

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

    public void SetSelected(bool selected)
    {
        if (background == null) return;
        background.color = selected ? selectedColor : normalColor;
    }
}