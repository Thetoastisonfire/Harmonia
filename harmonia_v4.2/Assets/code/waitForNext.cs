using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class waitForNext : MonoBehaviour
{
    public BoxCollider stickCollider; 
    public float waitTime = 1f;
    private Coroutine wait;

    private void OnTriggerEnter(Collider other)
    {   
        if (other.tag == "sphere") {
            Debug.Log("hit sphere!!!");
            if (wait == null) wait = StartCoroutine(DisableColliderTemporarily());
        }
    }

    private IEnumerator DisableColliderTemporarily()
    {
       if (stickCollider != null) {
            stickCollider.enabled = false; //disable
            yield return new WaitForSeconds(waitTime);
            stickCollider.enabled = true; //re-enable
        }
        wait = null;
    }
}
