using UnityEngine;
using UnityEngine.InputSystem;

/* Short documentation
Key Features:
- Ground Movement: Applies friction and acceleration based on input.
- Air Movement: Allows movement in the air with reduced acceleration.
- Jumping: Enables jumping when grounded, with an option for auto bunny hopping.
- Gravity: Continuously applies gravity to the player.

To edit/add:
- Balance it throught the editor
- Create a console log to see real-time values of velocity, gravity, acceleration and so on
 */

[RequireComponent(typeof(CharacterController))]
public class MovementComponent : MonoBehaviour
{
    [Header("Bind Input Actions")]
    public InputActionReference moveAction;
    public InputActionReference jumpAction; 
    
    [Header("Movement Settings")]
    public float moveSpeed = 70f;
    public float runAcceleration = 140;
    public float airAcceleration = 20f;
    public float friction = 60f;
    public float jumpForce = 80f;
    public float gravity = 200f;
    public bool autoBunnyHop = true;
    
    private CharacterController controller;
    public Vector3 velocity;
    public bool grounded;
    public Vector2 moveInput;
    
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        moveInput = moveAction?.action?.ReadValue<Vector2>() ?? Vector2.zero;
        grounded = controller.isGrounded;
        Vector3 movementDirection = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;

        if (grounded)
            GroundMove(movementDirection);
        else 
            AirMove(movementDirection);

        ApplyGravity(); // Apply gravity to vertical velocity
        ApplyMovement(); // Move the character controller

    }

    private void GroundMove(Vector3 movementDirection)
    {
        ApplyFriction();
        
        float targetSpeed = movementDirection.magnitude * moveSpeed;
        
        Accelerate(movementDirection, targetSpeed, runAcceleration);
        
        Jump();
        
    }

    private void ApplyFriction()
    {
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
        float speed = horizontalVelocity.magnitude;
        if (speed < 0.0001f) return;
        
        float drop = speed * friction * Time.deltaTime;
        float newSpeed = Mathf.Max(speed - drop, 0);
        velocity.x *= newSpeed / speed; 
        velocity.z *= newSpeed / speed;
    }

    private void Accelerate(Vector3 movementDirection, float targetSpeed, float acceleration) 
    {
        float currentSpeed = Vector3.Dot(velocity, movementDirection);
        float addSpeed = targetSpeed - currentSpeed;
        if (addSpeed <= 0) return;
        
        float accelSpeed = acceleration * Time.deltaTime * targetSpeed;
        if (accelSpeed > addSpeed)
            accelSpeed = addSpeed;
        
        velocity += movementDirection * accelSpeed;
    }

    private void Jump()
    {
        if (jumpAction.action.WasPressedThisFrame() || autoBunnyHop && jumpAction.action.IsPressed())
        {
            velocity.y = jumpForce;
        }
        
        else if (velocity.y < 0)
        {
            velocity.y = -1; // stick to the ground
        }
    }

    private void AirMove(Vector3 movementDirection)
    {
        float targetSpeed = movementDirection.magnitude * moveSpeed;
        Accelerate(movementDirection, targetSpeed, runAcceleration);
    }

    
    private void ApplyGravity()
    {
        velocity.y -= gravity * Time.deltaTime;
    }
    
    private void ApplyMovement()
    {
        controller.Move(velocity * Time.deltaTime);
    }
    
    private void OnEnable()
    {
        moveAction?.action?.Enable();
        jumpAction?.action?.Enable();
    }
    
    private void OnDisable()
    {
        moveAction?.action?.Disable();
        jumpAction?.action?.Disable();
    }
}
