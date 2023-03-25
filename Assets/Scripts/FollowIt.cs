using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowIt : MonoBehaviour {
    public GameObject target;
    public Vector3 offset;

    void Start () {
        offset = transform.position - target.transform.position;
    }

    void FixedUpdate () {
        transform.position = target.transform.position + offset;
    }
}
