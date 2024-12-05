using System.Collections;
using System.Collections.Generic;
using UnityEngine;


#region Sphere and Cloud Data
[System.Serializable]
public struct SphereData
{
    public int whichSphere;
    public int whichKindOfSphere;
    public float floatHeight;
    public float floatSpeed;
    public float bonkDistance;
    public float bonkDuration;
    public float radius;

    public SphereData(
    int whichSphere = -1, int whichKindOfSphere = -1,
    float floatHeight = 0.1f, float floatSpeed = 1.0f,
    float bonkDistance = 0.2f, float bonkDuration = 0.2f,
    float radius = 2.0f)
    {
        this.whichSphere = whichSphere; //e.g. 29 is string 2, fret 9
        this.whichKindOfSphere = whichKindOfSphere; //e.g. 1 = chords, 2 = solo, 3 = drums, 5 = emergency stop
        this.floatHeight = floatHeight;
        this.floatSpeed = floatSpeed;
        this.bonkDistance = bonkDistance;
        this.bonkDuration = bonkDuration;
        this.radius = radius;
    }
}

[System.Serializable]
public struct CloudData
{
    public GameObject cloudPrefab;
    public Color cloudColor;
    public Color lightColor;
    public float spawnHeightMin;
    public float spawnHeightMax;
    public float spawnHorizontalRange;

    public CloudData(Color cloudColor, Color lightColor,
    float spawnHeightMin = 3.0f, float spawnHeightMax = 5.0f, float spawnHorizontalRange = 2.0f)
    {
        this.cloudColor = cloudColor != default ? cloudColor : Color.white;
        this.lightColor = lightColor != default ? lightColor : Color.white;
        this.spawnHeightMin = spawnHeightMin;
        this.spawnHeightMax = spawnHeightMax;
        this.spawnHorizontalRange = spawnHorizontalRange;
        this.cloudPrefab = null;
    }
}
#endregion

//sphere class
public class SphereLogic : MonoBehaviour
{

    #region variables
    //////////////////////////////////
    [Header("Sphere Data")]
    [SerializeField] private SphereData sphereData = new SphereData(); //sphere data

    [Header("Cloud Data")]
    [SerializeField] private CloudData cloudData = new CloudData(); //cloud data

    //background variables
    private bool longNote, bonking = false;
    private Vector3 startPosition;
    private Vector3 currentPosition;
    private Coroutine bonkCoroutine;
    //////////////////////////////////
    #endregion

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        //new position via sine wave for float
        if (!bonking)
        {
            float newY = Mathf.Sin(Time.time * sphereData.floatSpeed) * sphereData.floatHeight;
            transform.position = new Vector3(startPosition.x, startPosition.y + newY, startPosition.z);
        }
    }

    #region Sphere Code

    //on hit
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player") return;



        //check if top or bottom was hit
        if (other.ClosestPoint(transform.position).y > transform.position.y)
            longNote = true; //top hit
        else longNote = false; //bottom hit

        Vector3 bonkDirection = (transform.position - other.transform.position).normalized;

        sphereSound(); //play sphere sound or function; ALSO spawns cloud if applicable

        if (bonkCoroutine != null) StopCoroutine(bonkCoroutine); //make sphere bounce
        bonkCoroutine = StartCoroutine(theBonk(bonkDirection));
    }

    #region sphere code (sphere sound calls cloud spawner)
    public void sphereSound()
    {

        string audioName = "";

        if (sphereData.whichSphere != -1 && sphereData.whichKindOfSphere != -1)
        { //naming convention is string, fret, so 27 is 2nd string 7th fret
            switch (sphereData.whichKindOfSphere)
            {
                case 1: //chords 
                    //tempo code
                    SoundManager.Instance.noteBuffers.chordBuffOn = false;
                    if (SoundManager.Instance.noteBuffers.eighthBuffOn) SoundManager.Instance.noteBuffers.eighthBuffOn = false;

                    audioName = "chordProg" + sphereData.whichSphere;
                    StartCoroutine(SoundManager.Instance.PlayAudioClip(audioName, true));
                    break;

                case 2: //solo; slightly different cause of naming convention
                    //tempo code
                    SoundManager.Instance.noteBuffers.eighthBuffOn = false;
                   //if (SoundManager.Instance.noteBuffers.chordBuffOn) SoundManager.Instance.noteBuffers.chordBuffOn = false;
                    if (!SoundManager.Instance.bufferedChordForLoops.chordLoop) SoundManager.Instance.bufferedChordForLoops.chordLoop = true;

                    audioName = "solo_" + (sphereData.whichSphere / 10) + "," + (sphereData.whichSphere % 10) + (longNote ? "_Long" : "_Short");
                    // StartCoroutine(SoundManager.Instance.PlayAudioClip("solo_2,7_Long", false));
                    StartCoroutine(SoundManager.Instance.PlayAudioClip(audioName, false));
                    break;

                case 3: //drums
                    //tempo code
                    SoundManager.Instance.noteBuffers.drumBuffOn = false;

                    StartCoroutine(SoundManager.Instance.PlayNewDrumBacking((Random.value > 0.5f) ? "drumTrack1" : "drumTrack2"));
                    // StartCoroutine(SoundManager.Instance.PlayNewDrumBacking("testDrums2"));
                    break;

                case 5:
                    Debug.Log("Emergency stop");
                    SoundManager.Instance.StopDrumBacking();
                    SoundManager.Instance.StopSoloClip();
                    SoundManager.Instance.StopChordClip();
                    SoundManager.Instance.noteBuffers.chordBuffOn = false;
                    SoundManager.Instance.bufferedChordForLoops.chordLoop = false;
                    SoundManager.Instance.pleaseStop = true;

                    if (SoundManager.Instance.soloClips.TryGetValue("emergencyOff", out AudioClip click))
                    {
                        Debug.Log("playing click!!!!!");
                        StartCoroutine(SoundManager.Instance.PlayClip(click, false));
                    }

                    break;

                default:
                    Debug.LogWarning("bonk sound didn't work");
                    break;
            }

            //spawn cloud if not emergency stop
            if (sphereData.whichKindOfSphere != 5) SpawnCloudAbovePlayer();
        }
        else Debug.LogWarning("sphere number or sphere type was wrong");
    }

    private Vector3 ClampPosition(Vector3 position)
    {
        Vector3 directionFromStart = position - startPosition;

        if (directionFromStart.magnitude > sphereData.radius)
        {
            directionFromStart.Normalize(); //normalize to maintain direction
            position = startPosition + directionFromStart * sphereData.radius; //clamp
        }

        return position;
    }

    private IEnumerator theBonk(Vector3 direction)
    {
        Vector3 bonkPosition = transform.position + direction * sphereData.bonkDistance; // Calculate bonked position
        bonkPosition = ClampPosition(bonkPosition);

        float elapsedTime = 0f;

        //move sphere
        while (elapsedTime < sphereData.bonkDuration)
        {
            transform.position = Vector3.Lerp(transform.position, bonkPosition, (elapsedTime / sphereData.bonkDuration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = bonkPosition;

        //move sphere back
        elapsedTime = 0f;
        while (elapsedTime < sphereData.bonkDuration * 5)
        {
            transform.position = Vector3.Lerp(transform.position, startPosition, (elapsedTime / sphereData.bonkDuration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = startPosition; //in case its not all the way back; currently it doesnt at all lol
    }
    #endregion

    #region cloud code
    private void SpawnCloudAbovePlayer()
    {
        if (cloudData.cloudPrefab != null)
        {
            Vector3 spawnPosition = startPosition;
            spawnPosition += Vector3.up * Random.Range(cloudData.spawnHeightMin, cloudData.spawnHeightMax);
            spawnPosition += new Vector3(Random.Range(-cloudData.spawnHorizontalRange, cloudData.spawnHorizontalRange), 0,
                                            Random.Range(-cloudData.spawnHorizontalRange, cloudData.spawnHorizontalRange));

            GameObject cloudInstance = Instantiate(cloudData.cloudPrefab, spawnPosition, Quaternion.identity);

            // Set cloud color
            Renderer[] renderers = cloudInstance.GetComponentsInChildren<Renderer>();
            foreach (Renderer rend in renderers)
            {
                rend.material.color = cloudData.cloudColor;
                rend.material.EnableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", cloudData.cloudColor * 2f);
            }

            // Set light color
            Light light = cloudInstance.AddComponent<Light>();
            light.color = cloudData.lightColor;
            light.intensity = 2f;
            light.range = 5f;

            // Start the fade-in and fade-out coroutine
            StartCoroutine(FadeCloud(cloudInstance, 0.5f));
        }
        else
        {
            Debug.LogWarning("Cloud prefab is not assigned.");
        }
    }

    private IEnumerator FadeCloud(GameObject cloudInstance, float duration)
    {
        Renderer[] renderers = cloudInstance.GetComponentsInChildren<Renderer>();
        float elapsedTime = 0f;

        // Fade in
        while (elapsedTime < duration)
        {
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / duration);
            SetCloudAlpha(renderers, alpha);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        SetCloudAlpha(renderers, 1f);

        // Wait 0.5 seconds
        yield return new WaitForSeconds(0.5f);

        // Fade out
        elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            SetCloudAlpha(renderers, alpha);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        SetCloudAlpha(renderers, 0f);

        // Destroy cloud instance after fading out
        Destroy(cloudInstance);
    }

    private void SetCloudAlpha(Renderer[] renderers, float alpha)
    {
        foreach (Renderer rend in renderers)
        {
            Color color = rend.material.color;
            color.a = alpha;
            rend.material.color = color;
        }
    }
    #endregion

    //end of sphere code
    #endregion

}