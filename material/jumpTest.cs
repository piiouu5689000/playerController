using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class jumpTest : MonoBehaviour
{
    public float jumpForce;
    private Rigidbody rb;
    Vector3 newVector;


    void Start() {
        rb = GetComponent<Rigidbody>();
    }
    void Update() {
        newVector = Vector3.up * rb.velocity.y;
        rb.velocity = newVector;
        if (Input.GetKeyDown(KeyCode.Space)){
            newVector.y = jumpForce;
        }

       
    }
}
