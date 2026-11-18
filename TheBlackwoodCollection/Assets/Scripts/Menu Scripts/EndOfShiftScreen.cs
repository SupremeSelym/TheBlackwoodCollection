using TMPro;
using UnityEngine;


//Listens to the game clock and opens itself when the clock reaches (6:00am)

public class EndOfShiftScreen : MonoBehaviour
{
   
    public static EndOfShiftScreen Instance { get; private set; }
    public static bool IsShowing { get; private set; }


    [Header("References")]
    [Tooltip("Leave empty to find the GameClock in the scene automatically.")]
    [SerializeField] private GameClock clock;
    [Tooltip("The whole end-of-shift panel. It starts hidden.")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text resultText;


    private void Awake()
    {
        Instance = this;
        IsShowing = false;

    }

    private void Start()
    {

        if(panel == null)
        {
            Debug.LogError("[EndOfShiftScreen] 'Panel' is not assigned.", this);
            return;
        }

        if(panel == gameObject)
        {
            Debug.LogError("[EndOfShiftScreen] This script is on the same object as panel. "
                + "Hiding the panel would switch the script off. Put it on a different object.", this);
        }

        panel.SetActive(false);

        if (clock == null) clock = FindFirstObjectByType<GameClock>();
        if(clock == null)
        {
            Debug.LogError("[EndOfShiftScreen] No GameClock Found. " +
                "Drag it into the 'Clock' field.", this);
            return;
        }

        clock.OnClockFinished += HandleClockFinished;

        //If clock has already finished before we started listening, open right away
        if (clock.IsFinished) HandleClockFinished();
    }

    private void onDestroy()
    {
        if (clock != null) clock.OnClockFinished -= HandleClockFinished;
        if (Instance == this) Instance = null;
        IsShowing = false;
    }

    private void HandleClockFinished()
    {
        Show(FormatHour(clock.endHour));
    }

    //Show the screen
    public void Show(string finalTime = "6:00 AM", string result = "")
    {
        if (IsShowing || panel == null) return;
        IsShowing = true;

        if (timeText != null) timeText.text = finalTime;
        if (resultText != null && !string.IsNullOrEmpty(result)) resultText.text = result;

        panel.SetActive(true);


        //Mouse can be used by player to click buttons on screen
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }


    public void SetResult(string message)
    {
        if (resultText != null) resultText.text = message;
    }

    private static string FormatHour(int hour)
    {

        int hour24 = hour % 24;
        string suffix = hour24 < 12 ? "AM" : "PM";
        int hour12 = hour24 % 12;
        if (hour12 == 0) hour12 = 12;
        return $"{hour12}:00 {suffix}";
    }

}
