using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    public float senseX;
    public float senseY;

    float xRotation;
    float yRotation;

    private float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    public float wallRunSpeed;

    public float wallRunForce;
    public float maxWallRunTime;
    private float wallRunTimer;

    public float wallCheckDistance;
    public float minJumpHeight;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    private bool wallLeft;
    private bool wallRight;

    public float groundDrag;

    public float playerHeight;
    public LayerMask whatIsGround;
    public LayerMask whatIsWall;
    bool grounded;

    public float maxSlopeAngle;
    private RaycastHit slopeHit;

    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;

    public float crouchSpeed;
    public float crouchYScale;
    public float startYScale;

    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.C;

    public Transform playerObject;
    public Camera playerCamera;
    public AudioListener playerAudioListener;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    Rigidbody rb;

    public MovementState state;

    public enum MovementState
    {
        sprinting,
        walking,
        crouching,
        wallrunning,
        air
    }

    public bool wallrunning;
    
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ResetJump();

        startYScale = transform.localScale.y;
        if(IsOwner)
        {
            playerCamera.enabled = true;
            playerAudioListener.enabled = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * senseX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * senseY;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        playerObject.rotation = Quaternion.Euler(0, yRotation, 0);

        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        StateHandler();
        CheckForWall();
        StateMachine();

        if (grounded)
        {
            rb.drag = groundDrag;
        } else
        {
            rb.drag = 0;
        }

        if(readyToJump && grounded && Input.GetKeyDown(jumpKey))
        {
            readyToJump = false;
            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }

        if(Input.GetKey(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }
        if(Input.GetKeyUp(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);

        }

    }

    private void StateHandler()
    {
        if(wallrunning)
        {
            state = MovementState.wallrunning;
            moveSpeed = wallRunSpeed;
        }
        
        if(Input.GetKey(crouchKey))
        {
            state = MovementState.crouching;
            moveSpeed = crouchSpeed;
        }
        if(grounded && Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
        } else if (grounded)
        {
            state = MovementState.walking;
            moveSpeed = walkSpeed;
        } else
        {
            state = MovementState.air;
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;
        moveDirection = playerObject.forward * verticalInput + playerObject.right * horizontalInput;
        if (wallrunning)
        {
            WallRunningMovement();
        }
        else if (OnSlope())
        {
            rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 10f, ForceMode.Force);
        }
        else if (grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        } else
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }
        
    }

    private void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }

    private void CheckForWall()
    {
        wallRight = Physics.Raycast(transform.position, playerObject.right, out rightWallHit, wallCheckDistance, whatIsWall);
        wallLeft = Physics.Raycast(transform.position, -playerObject.right, out leftWallHit, wallCheckDistance, whatIsWall);
    }

    private bool AboveGround()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, whatIsGround);
    }

    private void StateMachine()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if ((wallRight || wallLeft) && verticalInput > 0 && AboveGround())
        {
            // wall run!
            if (!wallrunning)
                StartWallRun();
        }
        else
        {
            if (wallrunning)
                StopWallRun();
        }
    }

    private void StartWallRun()
    {
        wallrunning = true;
    }

    private void WallRunningMovement()
    {
        rb.useGravity = false;
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        if ((playerObject.forward - wallForward).magnitude > (playerObject.forward - -wallForward).magnitude)
            wallForward = -wallForward;

        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);
    }

    private void StopWallRun()
    {
        wallrunning = false;
        rb.useGravity = true;
    }
}
