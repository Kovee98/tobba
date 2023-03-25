using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorRootMotion : MonoBehaviour {
    private Animator animator;
    public Transform target;

    // Start is called before the first frame update
    void Start () {
        animator = GetComponent<Animator>();
    }

    void OnAnimatorMove () {
        if (animator) {
            // get the root motion delta for this frame
            Vector3 deltaPosition = animator.deltaPosition;

            // apply the root motion delta to the character's position
            transform.position += deltaPosition;

            // target.position += deltaPosition * 10;

            // // Get the root motion rotation for this frame
            // Quaternion rootMotionRotation = animator.deltaRotation;

            // // Apply the root motion rotation to the character's rotation
            // transform.rotation *= rootMotionRotation;
            // // target.rotation *= rootMotionRotation;
        }
    }

    /* standing jump */
    public void OnLand () {
        animator.SetBool("IsJumping", false);
    }
}
