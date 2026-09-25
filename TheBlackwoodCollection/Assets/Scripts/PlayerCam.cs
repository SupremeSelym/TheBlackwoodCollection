using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerCam : MonoBehaviour
{
    private Camera mainCamera;
    private CharacterController player;

    [SerializeField] private float lookAngle = 0.0f;
    [SerializeField] private float LookSensitivity = 0.2f;
    [SerializeField] private float LookAngleLimit = 90f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCamera = GetComponentInChildren<Camera>();
        //player = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        HandleLooking(mouseDelta);
    }

    private void HandleLooking(Vector2 mouseDelta) // Camera Movement
    {
        
        lookAngle += -mouseDelta.y * LookSensitivity;
        lookAngle = Mathf.Clamp(lookAngle, -LookAngleLimit, LookAngleLimit);

        mainCamera.transform.localRotation = Quaternion.Euler(lookAngle,0,0); // Look up and Down
        transform.rotation *= Quaternion.Euler(0, mouseDelta.x * LookSensitivity, 0); // Look Left and Right
    }
}
