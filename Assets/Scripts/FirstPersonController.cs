using UnityEngine;
using UnityEngine.InputSystem;

namespace StarterAssets {
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

		[Space(10)]
		[Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
		public float jumpTimeout = 0.1f;
		[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
		public float fallTimeout = 0.15f;

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
        public string primary = "Standing1HAttack01";
        public string secondary = "Standing1HAttack02";
        public string legendary = "Standing2HAttack01";
        public float castTimeout = 0.1f;
        private float _castTimeoutDelta = 0.1f;

		// cinemachine
		private float _cinemachineTargetPitch;

		// player
		private float _speed;
		private float _rotationVelocity;
		private float _verticalVelocity;
		private float _terminalVelocity = 53.0f;

		// timeout deltatime
		private float _jumpTimeoutDelta;
		private float _fallTimeoutDelta;

		private PlayerInput _playerInput;
		private CharacterController _controller;
		private StarterAssetsInputs _input;
		private GameObject _mainCamera;

		private const float _threshold = 0.01f;

		private void Awake () {
			// get a reference to our main camera
			if (_mainCamera == null) {
				_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
			}
		}

		private void Start () {
			_controller = GetComponent<CharacterController>();
			_input = GetComponent<StarterAssetsInputs>();
			_playerInput = GetComponent<PlayerInput>();

			// reset our timeouts on start
			_jumpTimeoutDelta = jumpTimeout;
			_fallTimeoutDelta = fallTimeout;
			_castTimeoutDelta = castTimeout;
		}

		private void Update () {
			JumpAndGravity();
			GroundedCheck();
			Move();
            Action();
		}

		private void LateUpdate () {
			CameraRotation();
		}

		private void GroundedCheck () {
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z);
			grounded = Physics.CheckSphere(spherePosition, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);

            // set jumping/falling animation parameters
            animator.SetBool("IsGrounded", grounded);

            if (grounded) {
                animator.SetBool("IsFalling", false);
            }

            // just landed
            if (grounded && !lastGrounded) {
                animator.Play("Jumping.FallingToLanding", 0, landingOffset);
            }

            lastGrounded = grounded;
		}

		private void CameraRotation () {
			// if there is an input
			if (_input.look.sqrMagnitude >= _threshold) {
				//Don't multiply mouse input by Time.deltaTime
				float deltaTimeMultiplier = _playerInput.currentControlScheme == "KeyboardMouse" ? 1.0f : Time.deltaTime;
				
				_cinemachineTargetPitch += _input.look.y * rotationSpeed * deltaTimeMultiplier;
				_rotationVelocity = _input.look.x * rotationSpeed * deltaTimeMultiplier;

				// clamp our pitch rotation
				_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, bottomClamp, topClamp);

				// Update Cinemachine camera target pitch
				cinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

				// rotate the player left and right
				transform.Rotate(Vector3.up * _rotationVelocity);
			}
		}

		private void Move () {
			// set target speed based on move speed, sprint speed and if sprint is pressed
			float targetSpeed = _input.sprint ? sprintSpeed : moveSpeed;

			// a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

			// note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is no input, set the target speed to 0
			if (_input.move == Vector2.zero) targetSpeed = 0.0f;

			// a reference to the players current horizontal velocity
			float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

			float speedOffset = 0.1f;
			float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

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
            Vector3 inputMove = new Vector3(_input.move.x, 0.0f, _input.move.y);
			Vector3 inputDirection = inputMove.normalized;

			// note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is a move input rotate player when the player is moving
			if (_input.move != Vector2.zero) {
				// move
				inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
			}

			// move the player
			_controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // set running/sprinting animation parameters
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsRunningLeft", false);
            animator.SetBool("IsRunningRight", false);
            animator.SetBool("IsRunningBack", false);
            animator.SetBool("IsSprinting", false);

            if (_speed > 0f) {
                if (_input.sprint) {
                    animator.SetBool("IsSprinting", true);
                } else {
                    // set running/strifing animation parameters
                    if (inputMove.z > 0 && inputMove.x == 0) animator.SetBool("IsRunning", true);
                    else if (inputMove.z < 0) animator.SetBool("IsRunningBack", true);
                    else if (inputMove.x < 0) animator.SetBool("IsRunningLeft", true);
                    else if (inputMove.x > 0) animator.SetBool("IsRunningRight", true);
                }
            }
		}

		private void JumpAndGravity () {
			if (grounded) {
				// reset the fall timeout timer
				_fallTimeoutDelta = fallTimeout;

				// stop our velocity dropping infinitely when grounded
				if (_verticalVelocity < 0.0f) {
					_verticalVelocity = -2f;
				}

				// Jump
				if (_input.jump && _jumpTimeoutDelta <= 0.0f) {
					// the square root of H * -2 * G = how much velocity needed to reach desired height
					_verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

                    // play jump animation
                    // animator.Play("Jumping.StandingJump", 0, 0.275f);
                    animator.Play("Jumping.StandingJumpCaster", 0, jumpOffset);
				}

				// jump timeout
				if (_jumpTimeoutDelta >= 0.0f) {
					_jumpTimeoutDelta -= Time.deltaTime;
				}
			} else {
				// reset the jump timeout timer
				_jumpTimeoutDelta = jumpTimeout;

				// fall timeout
				if (_fallTimeoutDelta >= 0.0f) {
					_fallTimeoutDelta -= Time.deltaTime;
				} else {
                    animator.SetBool("IsFalling", true);
                }

				// if we are not grounded, do not jump
				_input.jump = false;
			}

			// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
			if (_verticalVelocity < _terminalVelocity) {
				_verticalVelocity += gravity * Time.deltaTime;
			}
		}

        public bool isCasting = false;
        private void Action () {
            if (!isCasting && (_input.primary || _input.secondary)) {
                isCasting = true;
			    _castTimeoutDelta = castTimeout;

                if (_input.primary && !_input.secondary) {
                    // Debug.Log("primary...");
                    animator.Play(primary, 0, 0f);
                } else if (_input.secondary && !_input.primary) {
                    // Debug.Log("secondary...");
                    animator.Play(secondary, 0, 0f);
                } else if (_input.primary && _input.secondary) {
                    // Debug.Log("legendary...");
                    animator.Play(legendary, 0, 0f);
                }
            }

            if (_castTimeoutDelta >= 0.0f) {
                _castTimeoutDelta -= Time.deltaTime;
            } else {
                // isCasting = false;
            }

            if (_castTimeoutDelta <= 0.0f && !_input.primary && !_input.secondary) {
                isCasting = false;
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
        void OnAnimatorMove () {}
	}
}