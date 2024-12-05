using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class P_Move : MonoBehaviour 
{    
    
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private GameObject Jimmy;

    [SerializeField] private Rigidbody rb;

    private bool colliding = false;

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        //forward vector
        Vector3 movement = (Jimmy.transform.forward * vertical + Jimmy.transform.right * horizontal) * moveSpeed * Time.deltaTime;

        //move player
        if (!colliding) rb.MovePosition(Jimmy.transform.position + movement);

        //exit with key "1"
        if (Input.GetKeyDown(KeyCode.Alpha1)) {
            Application.Quit();
        }

    }

    private void OnCollisionEnter(Collision other) {
        if (other.gameObject.tag != "ground" || other.gameObject.tag == "furniture") {
            colliding = true;
            Debug.Log("not the ground");
            Vector3 pushDirection = (transform.position - other.transform.position).normalized; //get direction away from collision
            rb.AddForce(pushDirection * moveSpeed / 10, ForceMode.Impulse);
        }
    }
    private void OnCollisionExit(Collision other) {
        if (other.gameObject.tag != "ground") colliding = false;
    }
}

