using System.Collections;
using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource; 
    [SerializeField] private AudioClip[] sounds;
 
    public IEnumerator playSoundEffect(int whichThingie)
    {
        if (whichThingie < 0 || whichThingie >= sounds.Length) {
            Debug.LogError("Invalid sound index: " + whichThingie);
            yield break;
        }

        //play sound
        if (whichThingie != 0) audioSource.PlayOneShot(sounds[whichThingie]);
        else audioSource.Stop();
        Debug.Log("playedSound");

        //wait until end of sound, before ending coroutine
        yield return new WaitForSeconds(sounds[whichThingie].length);
    }
}
