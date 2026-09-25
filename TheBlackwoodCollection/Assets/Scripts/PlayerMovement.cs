using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float WalkSpeed = 5.5f;
    [SerializeField] private float RunSpeed = 9.0f;
    

    private CharacterController characterController;

    private InputAction moveInput;
    private InputAction sprintInput;

    private float currentMoveSpeed = 0f;
    private Vector3 moveDirection = Vector3.zero;
    //private float lookAngle = 0.0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        moveInput = InputSystem.actions.FindAction("Move");
        sprintInput = InputSystem.actions.FindAction("Sprint");

        currentMoveSpeed = WalkSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 moveVector = moveInput.ReadValue<Vector2>();
        HandleMovement(moveVector);
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
}
