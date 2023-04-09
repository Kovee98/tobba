using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class FirstPersonController : MonoBehaviour {
    [Header("Player")]
    public Animator animator;
    [Tooltip("Move speed of the character in m/s")]
    public float moveSpeed = 5.0f;
    [Tooltip("Sprint speed of the character in m/s")]
    public float sprintSpeed = 8.0f;
    [Tooltip("Rotation speed of the character")]
    public float rotationSpeed = 2.0f;
    [Tooltip("Acceleration and deceleration")]
    public float speedChangeRate = 10.0f;

    [Space(10)]
    [Tooltip("The height the player can jump")]
    public float jumpHeight = 2.0f;
    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float gravity = -30.0f;

    [Header("Player Grounded")]
    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool grounded = true;
    private bool lastGrounded = true;
    public float jumpOffset = 0.275f;
    public float landingOffset = 0.25f;
    [Tooltip("Useful for rough ground")]
    public float groundedOffset = -0.14f;
    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float groundedRadius = 0.5f;
    [Tooltip("What layers the character uses as ground")]
    public LayerMask groundLayers;

    [Header("Cinemachine")]
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject cinemachineCameraTarget;
    [Tooltip("How far in degrees can you move the camera up")]
    public float topClamp = 70.0f;
    [Tooltip("How far in degrees can you move the camera down")]
    public float bottomClamp = -80.0f;

    // actions
    public float castTimeout = 0.1f;
    private float _castTimeoutDelta = 0.1f;
    public float castCooldown = 1f;
    private float _castCooldownDelta = 1f;

    // cinemachine
    private float _cinemachineTargetPitch;

    // player
    private float _speed;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    private PlayerInput _playerInput;
    private CharacterController _controller;
    private GameObject _mainCamera;
    public GameObject leftHand;
    public GameObject rightHand;

    private Ray screenRay;
    private Ray castRay;
    private Vector3 rayOrigin = new Vector3(0.5f, 0.5f, 0f); // center of the screen

    private void Awake () {
        // get a reference to our main camera
        if (_mainCamera == null) {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }

    private void Start () {
        _controller = GetComponent<CharacterController>();
        _playerInput = GetComponent<PlayerInput>();

        // reset our timeouts on start
        _castTimeoutDelta = castTimeout;
        _castCooldownDelta = castCooldown;

        // actual Ray
        screenRay = Camera.main.ViewportPointToRay(rayOrigin);
        castRay = new Ray(leftHand.transform.position, (Vector3.zero - leftHand.transform.position).normalized);
    }

    private void Update () {
        Gravity();
        Movement();
        Action();

        Debug.DrawRay(screenRay.origin, screenRay.direction * rayLength, Color.blue);
        Debug.DrawRay(castRay.origin, castRay.direction * rayLength, Color.green);
    }

    private void LateUpdate () {
        CameraRotation();
    }

    // handles gravity and fall/jump timeouts based on if player is grounded
    private void Gravity () {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z);
        grounded = Physics.CheckSphere(spherePosition, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);

        animator.SetBool("Grounded", grounded);

        if (grounded) {
            if (!lastGrounded) {
                animator.SetTrigger("Land");
                jumpCount = 0;
            }

            // stop our velocity dropping infinitely when grounded
            if (_verticalVelocity < 0.0f) _verticalVelocity = -2f;
        }

        // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
        if (_verticalVelocity < _terminalVelocity) _verticalVelocity += gravity * Time.deltaTime;

        lastGrounded = grounded;
    }

    private const float _threshold = 0f;
    private void CameraRotation () {
        // if there is an input
        if (look.sqrMagnitude >= _threshold) {
            // don't multiply mouse input by Time.deltaTime
            float deltaTimeMultiplier = _playerInput.currentControlScheme == "KeyboardMouse" ? 1.0f : Time.deltaTime;
            
            _cinemachineTargetPitch += look.y * rotationSpeed * deltaTimeMultiplier;
            _rotationVelocity = look.x * rotationSpeed * deltaTimeMultiplier;

            // clamp our pitch rotation
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, bottomClamp, topClamp);

            // Update Cinemachine camera target pitch
            cinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

            // rotate the player left and right
            transform.Rotate(Vector3.up * _rotationVelocity);
        }
    }

    private void Movement () {
        // set target speed based on move speed, sprint speed and if sprint is pressed
        float targetSpeed = sprint ? sprintSpeed : moveSpeed;

        // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon
        // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is no input, set the target speed to 0
        if (move == Vector2.zero) targetSpeed = 0.0f;

        // a reference to the players current horizontal velocity
        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = analogMovement ? move.magnitude : 1f;

        // accelerate or decelerate to target speed
        if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset) {
            // creates curved result rather than a linear one giving a more organic speed change
            // note T in Lerp is clamped, so we don't need to clamp our speed
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * speedChangeRate);

            // round speed to 3 decimal places
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        } else {
            _speed = targetSpeed;
        }

        // normalise input direction
        Vector3 inputMove = new Vector3(move.x, 0.0f, move.y);
        Vector3 inputDirection = inputMove.normalized;

        // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is a move input rotate player when the player is moving
        if (move != Vector2.zero) {
            inputDirection = transform.right * move.x + transform.forward * move.y;
        }

        // move the player
        _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

        // set movement animation parameters
        animator.SetBool("Moving", _speed > 0f);
        animator.SetFloat("Velocity X", inputMove.x);
        animator.SetFloat("Velocity Z", inputMove.z);
    }

    private bool lastPrimary = false;
    private bool lastSecondary = false;
    private void Action () {
        // primary was changed
        if (!lastPrimary && primary) {
            _castTimeoutDelta = castTimeout;
            lastPrimary = true;
        }

        // secondary was changed
        if (!lastSecondary && secondary) {
            _castTimeoutDelta = castTimeout;
            lastSecondary = true;
        }

        if (_castTimeoutDelta > 0.0f) {
            _castTimeoutDelta -= Time.deltaTime;
        } else {
            if (_castTimeoutDelta <= 0.0f) {
                _castCooldownDelta = castCooldown;
                // cast the action
                if (primary && !secondary) {
                    animator.SetTrigger("AttackLeft");
                } else if (secondary && !primary) {
                    animator.SetTrigger("AttackRight");
                } else if (primary && secondary) {
                    animator.SetTrigger("AttackDouble");
                }

                // reset flags
                lastPrimary = false;
                lastSecondary = false;
                primary = false;
                secondary = false;
            }
        }

        if (_castTimeoutDelta > 0.0f) {
            _castCooldownDelta -= Time.deltaTime;
        }
    }

    private static float ClampAngle (float lfAngle, float lfMin, float lfMax) {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;

        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnDrawGizmosSelected () {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (grounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
        Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z), groundedRadius);
    }

    /*
        Animation events
    */
    public void FootR () {}
    public void FootL () {}
    public void Land () {}
    public void Hit () {}

    public float rayLength = 500f;
    private Vector3 castOrigin;
    private Vector3 castDest;
    private float tolerance = 0.01f;
    public void Attack (string side) {

        // ray from center of camera
        screenRay = Camera.main.ViewportPointToRay(rayOrigin);

        RaycastHit hit;
        // our Ray intersected a collider
        if (Physics.Raycast(screenRay, out hit, rayLength)) {
            if (hit.transform.gameObject.tag == "Block") {
                // determine which hand to use
                GameObject hand = leftHand;
                if (side == "right") hand = rightHand;
                Vector3 castDir = (hit.point - hand.transform.position).normalized;
                castRay = new Ray(hand.transform.position, castDir);
            }
        }
    }

    /* snippet for determining which side of the cube we hit (needs to be figured out when ) */
    private Vector3 GetCubeCollisionSide (Vector3 hitPoint, GameObject hitObj) {
        Vector3 objCenter = hitObj.transform.position;
        Vector3 objScale = hitObj.transform.localScale;
        Vector3 diff = hitPoint - objCenter;

        Vector3 newObjPos = objCenter;
        if ((diff.x + tolerance) >= (objScale.x / 2)) newObjPos.x = objCenter.x + objScale.x;
        else if ((diff.x - tolerance) <= -(objScale.x / 2)) newObjPos.x = objCenter.x - objScale.x;
        else if ((diff.z + tolerance) >= (objScale.z / 2)) newObjPos.z = objCenter.z + objScale.z;
        else if ((diff.z - tolerance) <= -(objScale.z / 2)) newObjPos.z = objCenter.z - objScale.z;
        else if ((diff.y + tolerance) >= (objScale.y / 2)) newObjPos.y = objCenter.y + objScale.y;
        else if ((diff.y - tolerance) <= -(objScale.y / 2)) newObjPos.y = objCenter.y - objScale.y;

        Debug.Log("newObjPos: " + newObjPos);

        return newObjPos;
    }

    public void AttackPrimary () {
        Debug.Log("Attack primary...");
    }

    public void AttackSecondary () {
        Debug.Log("Attack secondary...");
    }

    public void AttackLegendary () {
        Debug.Log("Attack legendary...");
    }

    /*
        Input events
    */
    [Header("Character Input Values")]
    private Vector2 move;
    private Vector2 look;
    private bool jump;
    private bool sprint;
    private bool crouch;
    private bool primary;
    private bool secondary;

    [Header("Movement Settings")]
    private bool analogMovement;

    [Header("Mouse Cursor Settings")]
    public bool cursorLocked = true;
    public bool cursorInputForLook = true;

    public void OnMove (InputValue value) {
        move = value.Get<Vector2>();
    }

    public void OnLook (InputValue value) {
        if(cursorInputForLook) {
            look = value.Get<Vector2>();
        }
    }

    private int maxJumpCount = 2;
    private int jumpCount = 0;
    public void OnJump (InputValue value) {
        if (jumpCount < maxJumpCount) {
            // the square root of H * -2 * G = how much velocity needed to reach desired height
            _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

            if (jumpCount == 0) {
                animator.SetTrigger("Jump");
            } else {
                animator.SetTrigger("Flip");
            }

            jumpCount++;
        }
    }

    public void OnSprint (InputValue value) {
        sprint = value.isPressed;
    }

    public void OnCrouch (InputValue value) {
        crouch = value.isPressed;
    }

    public void OnPrimary (InputValue value) {
        primary = value.isPressed;
    }

    public void OnSecondary (InputValue value) {
        secondary = value.isPressed;
    }

    private void OnApplicationFocus (bool hasFocus) {
        SetCursorState(cursorLocked);
    }

    private void SetCursorState (bool newState) {
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
    }
}