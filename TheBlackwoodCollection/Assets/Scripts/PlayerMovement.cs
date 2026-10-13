using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float WalkSpeed = 5.5f;
    [SerializeField] private float RunSpeed = 9.0f;
    [SerializeField] private float currentMoveSpeed = 0f;
    [SerializeField] private Light flashlight;
//SerializeField] private GameObject flashlight;
    private CharacterController characterController;

    private InputAction moveInput;
    private InputAction sprintInput;
    private InputAction toggleFlash;

    //private float currentMoveSpeed = 0f;
    private Vector3 moveDirection = Vector3.zero;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        moveInput = InputSystem.actions.FindAction("Move");
        sprintInput = InputSystem.actions.FindAction("Sprint");
        toggleFlash = InputSystem.actions.FindAction("Flashlight");
        flashlight.enabled=false;
        currentMoveSpeed = WalkSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        if(IsOwner == false)
        {
            return;
        }

        Vector2 moveVector = moveInput.ReadValue<Vector2>();
        HandleMovement(moveVector);
        StartSprint();
        toggleFlashlight();
    } 

    private void HandleMovement(Vector2 moveVector)
    {
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        float oldY = moveDirection.y;

        Vector2 newSpeed = new Vector2(moveVector.y * currentMoveSpeed, moveVector.x * currentMoveSpeed);

        moveDirection = (forward * newSpeed.x) + (right * newSpeed.y);
        moveDirection.y = oldY;

        characterController.Move(moveDirection * Time.deltaTime);
    }
    private void StartSprint()
    {
        if (sprintInput.IsPressed())
        {
            currentMoveSpeed = RunSpeed;
        }
        else if(!sprintInput.IsPressed())
        {
            currentMoveSpeed = WalkSpeed;
        }
    }
    private void toggleFlashlight()
    {
        if (toggleFlash.IsPressed())
        {
            flashlight.enabled = true;
            Debug.Log("Flashlight on");
        }
        else
        {
            flashlight.enabled = false;
        }
    }
}
