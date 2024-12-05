using System.Collections;
using UnityEngine;

public class handDown : MonoBehaviour
{
    [SerializeField] private OVRCameraRig cameraRig;
    [SerializeField] private Transform player;
    [SerializeField] private float swingAngle = 30f;
    [SerializeField] private float swingDuration = 1f;
    [SerializeField] private float returnDuration = 1f;

    private Transform rightHandAnchor;
    private Transform leftHandAnchor;
    private Quaternion startingRotation;
    private Coroutine swingStick;
    private bool swingAgain = false;

    void Start()
    {
        // Cache hand anchor transforms
        rightHandAnchor = cameraRig.rightHandAnchor;
        leftHandAnchor = cameraRig.leftHandAnchor;

        // Initialize stick's starting rotation
        startingRotation = transform.rotation;
    }

    void Update()
    {
        // Check Oculus controller input for the right hand (A button) and left hand (X button)
        if (OVRInput.GetDown(OVRInput.Button.One) || swingAgain)
        {
            if (swingStick == null)
            {
                swingStick = StartCoroutine(SwingDown(rightHandAnchor));
                swingAgain = false;
            }
            else
            {
                swingAgain = true;
            }
        }

        if (OVRInput.GetDown(OVRInput.Button.Three) || swingAgain)
        {
            if (swingStick == null)
            {
                swingStick = StartCoroutine(SwingDown(leftHandAnchor));
                swingAgain = false;
            }
            else
            {
                swingAgain = true;
            }
        }
    }

    private IEnumerator SwingDown(Transform handAnchor)
    {
        // Reset rotation based on the player's current direction
        startingRotation = Quaternion.Euler(0, player.eulerAngles.y, 0);

        // Align to the hand controller's forward direction
        Vector3 direction = handAnchor.forward;
        Quaternion targetRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(swingAngle, 0, 0) * startingRotation;

        // Perform swing
        float elapsedTime = 0f;
        while (elapsedTime < swingDuration)
        {
            transform.rotation = Quaternion.Slerp(startingRotation, targetRotation, elapsedTime / swingDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Set to final target rotation
        transform.rotation = targetRotation;

        // Return from swing
        elapsedTime = 0f;
        while (elapsedTime < returnDuration)
        {
            transform.rotation = Quaternion.Slerp(targetRotation, startingRotation, elapsedTime / returnDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Reset to initial rotation relative to the player
        transform.rotation = startingRotation;
        swingStick = null;
    }
}
