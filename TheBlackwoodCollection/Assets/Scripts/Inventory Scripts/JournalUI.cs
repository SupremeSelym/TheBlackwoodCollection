using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class JournalUI : MonoBehaviour
{
    /// Other scripts check this to know whether the player should be frozen.
    public static bool IsOpen { get; private set; }

    [Header("References")]
    public GameObject panel;
    public TMP_Text leftText;
    public TMP_Text rightText;
    public Button prevButton;
    public Button nextButton;

    [Header("Settings")]
    public Key toggleKey = Key.J;
    [Tooltip("Show the mouse cursor while the journal is open so you can click the buttons.")]
    public bool unlockCursorWhenOpen = true;

    [Header("Pages (index 0 = left, 1 = right, 2 = left of next spread...)")]
    [TextArea(3, 10)]
    public string[] pages;

    private int spread = 0;
    private CursorLockMode previousLockMode;
    private bool previousCursorVisible;

    private int SpreadCount => Mathf.Max(1, (pages.Length + 1) / 2);

    private void Start()
    {
        IsOpen = false;
        panel.SetActive(false);
        prevButton.onClick.AddListener(PreviousSpread);
        nextButton.onClick.AddListener(NextSpread);
    }

    private void OnDestroy()
    {
        IsOpen = false; // never leave the player frozen if this object goes away
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && kb[toggleKey].wasPressedThisFrame)
        {
            if (panel.activeSelf) Close();
            else Open();
        }
    }

    private void Open()
    {
        panel.SetActive(true);
        IsOpen = true;
        Refresh();

        if (unlockCursorWhenOpen)
        {
            previousLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void Close()
    {
        panel.SetActive(false);
        IsOpen = false;

        if (unlockCursorWhenOpen)
        {
            Cursor.lockState = previousLockMode;
            Cursor.visible = previousCursorVisible;
        }
    }

    private void NextSpread()
    {
        if (spread < SpreadCount - 1)
        {
            spread++;
            Refresh();
        }
    }

    private void PreviousSpread()
    {
        if (spread > 0)
        {
            spread--;
            Refresh();
        }
    }

    private void Refresh()
    {
        int leftIndex = spread * 2;
        int rightIndex = leftIndex + 1;

        leftText.text = leftIndex < pages.Length ? pages[leftIndex] : "";
        rightText.text = rightIndex < pages.Length ? pages[rightIndex] : "";

        prevButton.interactable = spread > 0;
        nextButton.interactable = spread < SpreadCount - 1;
    }
}