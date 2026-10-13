using UnityEngine;
using UnityEngine.InputSystem;

public class CursorControl : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Makes cursor disappear
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
