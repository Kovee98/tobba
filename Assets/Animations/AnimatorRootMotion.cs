using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorRootMotion : MonoBehaviour {
    private Animator animator;
    public GameObject targetObj;
    // public Transform target;
    public Transform parent;

    public float initFootY = 0f;

    // Start is called before the first frame update
    void Start () {
        animator = GetComponent<Animator>();
        initFootY = animator.GetFloat("FootY");
    }

    void OnAnimatorMove () {
        // Debug.Log("targetObj.transform.position: " + targetObj.transform.position);
        if (animator) {
            Debug.Log("animator.deltaPosition: " + animator.deltaPosition);
            Vector3 deltaPosition = animator.deltaPosition;
            deltaPosition.x = 0f;
            deltaPosition.z = 0f;
            if (deltaPosition.y < 0f) deltaPosition.y = 0f;
            // transform.parent.rotation = animator.rootRotation;
            transform.parent.position += deltaPosition * 10f;

            // float footY = animator.GetFloat("FootY");
            // Debug.Log("footY: " + footY);

            // Vector3 newPosition = parent.position;
            // float deltaY = footY - initFootY;
            // if (deltaY < 0f) deltaY = 0f;
            // Debug.Log("deltaY: " + deltaY);
            // newPosition.y = newPosition.y + deltaY;

            // parent.position = newPosition;
        }
    }

    /* standing jump */
    public void OnLand () {
        animator.SetBool("IsJumping", false);
    }
}
