using UnityEngine;
using UnityEngine.InputSystem;

namespace StarterAssets {
	public class StarterAssetsInputs : MonoBehaviour {
		[Header("Character Input Values")]
        public Animator animator;
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;
		public bool crouch;
		public bool primary;
		public bool secondary;
		public bool legendary;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

		public void OnMove(InputValue value) {
			move = value.Get<Vector2>();
		}

		public void OnLook(InputValue value) {
			if(cursorInputForLook) {
			    look = value.Get<Vector2>();
			}
		}

		public void OnJump(InputValue value) {
            Debug.Log("jump: " + value.isPressed);
			jump = value.isPressed;
            // animator.SetBool("IsJumping", value.isPressed);
            // animator.SetBool("IsJumping", true);
		}

		public void OnSprint(InputValue value) {
			sprint = value.isPressed;
		}

		public void OnCrouch(InputValue value) {
            Debug.Log("crouching...");
			crouch = value.isPressed;
		}

		public void OnPrimary(InputValue value) {
            Debug.Log("primary...");
			primary = value.isPressed;
		}

		public void OnSecondary(InputValue value) {
            Debug.Log("secondary...");
			secondary = value.isPressed;
		}

		public void OnLegendary(InputValue value) {
            Debug.Log("legendary...");
			legendary = value.isPressed;
		}

		private void OnApplicationFocus(bool hasFocus) {
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState) {
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
}
