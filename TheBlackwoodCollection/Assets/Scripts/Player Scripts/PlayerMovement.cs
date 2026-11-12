using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float WalkSpeed = 5.5f;
    [SerializeField] private float RunSpeed = 9.0f;
    [SerializeField] private float currentMoveSpeed = 0f;
    private CharacterController characterController;

    private InputAction moveInput;
    private InputAction sprintInput;

    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaDrainRate = 25f;
    public float staminaRegenRate = 15f;
    [SerializeField] private GameObject staminaCanvasObject;
    [SerializeField] private Image staminaBarImage;
    [SerializeField] private float exhaustionDelay = 2f; // How many seconds to wait before regenerating
    private float exhaustionTimer = 0f;
    private bool isExhausted = false;

    //private float currentMoveSpeed = 0f;
    private Vector3 moveDirection = Vector3.zero;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        moveInput = InputSystem.actions.FindAction("Move");
        sprintInput = InputSystem.actions.FindAction("Sprint");
        currentMoveSpeed = WalkSpeed;
        currentStamina = maxStamina;

        if (!IsOwner)
        {
            if (staminaCanvasObject != null)
            {
                staminaCanvasObject.SetActive(false);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!IsOwner) return;
        if(PauseMenu.GameIsPaused) return;
        if (JournalUI.IsOpen) return;

        Vector2 moveVector = moveInput.ReadValue<Vector2>();
        HandleMovement(moveVector);
        StartSprint();
        UpdateStaminaUI();


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
        // Check if the input is pressed AND the player is not exhausted
        bool isTryingToSprint = sprintInput.IsPressed() && !isExhausted;

        if (isTryingToSprint && currentStamina > 0f)
        {
            // Sprinting
            currentMoveSpeed = RunSpeed;

            // Drain stamina smoothly over time
            currentStamina -= staminaDrainRate * Time.deltaTime;

            // If stamina hits 0, trigger exhaustion and reset the delay timer
            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                isExhausted = true;
                exhaustionTimer = 0f;
            }
        }
        else
        {
            // Walking / Not Sprinting
            currentMoveSpeed = WalkSpeed;

            // If the player hit exhaustion, wait for the delay time before regenerating
            if (isExhausted)
            {
                exhaustionTimer += Time.deltaTime;

                // Once the wait time is met, clear exhaustion so it can start refilling
                if (exhaustionTimer >= exhaustionDelay)
                {
                    isExhausted = false;
                    exhaustionTimer = 0f;
                }
            }
            else
            {
                // Regenerate stamina normally if not exhausted and not full
                if (currentStamina < maxStamina)
                {
                    currentStamina += staminaRegenRate * Time.deltaTime;
                    currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
                }
            }
        }
    }

    private void UpdateStaminaUI()
    {
        if (staminaBarImage != null)
        {
            staminaBarImage.fillAmount = currentStamina / maxStamina;
        }
    }

}
