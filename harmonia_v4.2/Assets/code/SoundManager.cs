using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{

    #region variables
    //[Header("Audio Sources")]
    public static SoundManager Instance { get; private set; }
    public AudioSource soloSource; // For sound effects
    public AudioSource soloSource2; // For sound effects so they don't overlap
    public AudioSource chordSource; // For dialogue
    public AudioSource drumBackingSource; // For background music
    public AudioSource backgroundAmbientSource; // For ambient effects
    [Space(20)]
    [SerializeField] private AudioSource miscAudioSource1;
    [SerializeField] private AudioSource miscAudioSource2;
    [SerializeField] private AudioSource miscAudioSource3;
    private AudioSource[] audioSources;

    //[Header("Volume")]
    private float[] originalVolumes;
    public static float theVolume = 1f;

    [Header("Dictionary Init")]
    private Dictionary<string, AudioClip> chordClips = new Dictionary<string, AudioClip>(); //dialogue, organized like "1,1" and "48,3"
    public Dictionary<string, AudioClip> soloClips = new Dictionary<string, AudioClip>(); //sound effects
    private Dictionary<string, AudioClip> drumBacking = new Dictionary<string, AudioClip>(); //background music
    public bool initializationComplete = false;

    //tempo stuff--------------------------------------------------
    [Header("Tempo Code")]
    private (float bpm, float quarterNoteInterval, float eighthNoteInterval, float nextEighthNoteTime) tempoStuff = (96f, 0f, 0f, 0f);
    public (int eighthCounter, bool chordBuffOn, bool eighthBuffOn, bool drumBuffOn) noteBuffers = (0, false, false, false);
    private string bufferedSphereClipName = ""; //sphere data buffer
    public (string clipName, bool chordLoop) bufferedChordForLoops = ("", false);
    public bool pleaseStop = false;
    //currently playing chord will loop until new chord

    //eighth note * 2 = quarter, eighth note * 32 = 4 bar measure (chords), eighth * 8 = 1 measure (drums)
    //---------------------------------------------------------------

    #endregion

    #region Tempo Control

    public void tempoInit()
    {
        //calculate notes
        tempoStuff.quarterNoteInterval = 60f / tempoStuff.bpm; //0.625?
        tempoStuff.eighthNoteInterval = tempoStuff.quarterNoteInterval / 2f;
        tempoStuff.nextEighthNoteTime = Time.time + tempoStuff.eighthNoteInterval;

    }

    void Update()
    {
        // == is bad cause it might be too precise
        if (Time.time >= tempoStuff.nextEighthNoteTime)
        {
            noteBuffers.eighthCounter += 1;

            //handle buffers for different timings
            /*if (!noteBuffers.chordBuffOn && bufferedChordForLoops.chordLoop && noteBuffers.eighthCounter % 32 == 0)
            {
                StartCoroutine(PlayAudioClip(bufferedChordForLoops.clipName, true));
            }
            else*/ if (noteBuffers.drumBuffOn && (noteBuffers.eighthCounter % /*32*/ 8 == 0))  //4 measures is 32 but idk if thats good
            {
                measureDrum();
            }
            else if (noteBuffers.chordBuffOn && (noteBuffers.eighthCounter % 8 == 0)) //chord timing
            {
                measureChord();
            }
            else if (noteBuffers.eighthBuffOn)  //quarter and eighth notes
            {
                onEighthNote();
            }

            //debug quarter note counter
            /* else if (((noteBuffers.chordBuffOn || noteBuffers.drumBuffOn)) && (noteBuffers.eighthCounter % 2 == 0)) {

                 if (soloClips.TryGetValue("metronomeClick", out AudioClip clip))
                     {
                         Debug.Log("playing click!!!!!");
                         StartCoroutine(PlayClip(clip, false));
                     }
             }*/


            if (noteBuffers.eighthCounter == 64) noteBuffers.eighthCounter = 0; //so it doesn't get too high

            tempoStuff.nextEighthNoteTime += tempoStuff.eighthNoteInterval;
        }
    }

    void measureChord() //chords; Chords will loop if no other chord is played
    {
        // Trigger note playback on the beat
        Debug.Log("Chord played on measure!");

        //so when a hit is queued and chord is queued, it plays
        if (noteBuffers.chordBuffOn)
        {
            bufferedChordForLoops.chordLoop = false; //if new chord, stop loop
            StartCoroutine(PlayAudioClip(bufferedChordForLoops.clipName, true));
        }

        noteBuffers.chordBuffOn = false;
    }

    void measureDrum() //drums
    {
        // Trigger note playback on the beat
        Debug.Log("Drum played on the measure!");

        //so when a hit is queued and drum is queued, it plays
        if (noteBuffers.drumBuffOn)
        {
            StartCoroutine(PlayNewDrumBacking(bufferedSphereClipName)); 
            noteBuffers.drumBuffOn = false;
        }
        // Your note-playing logic goes here
    }

    void onEighthNote() //basically eighth and quarter notes psshhhh probably fine
    {
        // Trigger note playback on the beat
        Debug.Log("Note played on the eighth/quarter note!");

        //so when a hit is queued and solo note is queued, it plays
        if (noteBuffers.eighthBuffOn)
        {
            StartCoroutine(PlayAudioClip(bufferedSphereClipName, false));
        }

        noteBuffers.eighthBuffOn = false;
        // Your note-playing logic goes here
    }



    #endregion

    #region initializing 
    // Start is called before the first frame update
    public void Start()
    {

        #region Audio Init stuff
        audioSources =
        new AudioSource[] {
            soloSource, soloSource2,
            chordSource,
            drumBackingSource, backgroundAmbientSource,
            miscAudioSource1, miscAudioSource2,
            miscAudioSource3
        };

        originalVolumes = new float[audioSources.Length];

        for (int i = 0; i < audioSources.Length; i++)
        {
            if (audioSources[i] != null)
            {
                originalVolumes[i] = audioSources[i].volume;
                Debug.Log("Assigned AudioSource at index " + i + ": " + audioSources[i].name);
            }
        }//14 audio sources, null sources are ignored

        SetGlobalVolume(theVolume);
        #endregion

        #region Tempo Init Stuff
        tempoInit();
        #endregion
        //Debug.LogWarning("global volume on");
    }

    //start is above awake for accessibility
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure soloSource, drumBackingSource, and chordSource are initialized
        if (soloSource == null || drumBackingSource == null || chordSource == null)
        {
            AudioSource[] sources = GetComponents<AudioSource>();
            if (sources.Length < 3)
            {
                soloSource = gameObject.AddComponent<AudioSource>();
                drumBackingSource = gameObject.AddComponent<AudioSource>();
                chordSource = gameObject.AddComponent<AudioSource>(); // Add chordSource if not already present
            }
            else
            {
                soloSource = sources[0];
                drumBackingSource = sources[1];
                chordSource = sources[2]; // Assign chordSource if already present
            }
        }

        StartCoroutine(InitializeAudioClips());
    }

    private IEnumerator InitializeAudioClips()
    {
        StartCoroutine(LoadAudioClips("audio/chords", chordClips));
        StartCoroutine(LoadAudioClips("audio/solo", soloClips));
        yield return StartCoroutine(LoadAudioClips("audio/drums", drumBacking));
        initializationComplete = true;
    }

    private IEnumerator LoadAudioClips(string path, Dictionary<string, AudioClip> clipDictionary)
    {
        AudioClip[] clips = Resources.LoadAll<AudioClip>(path);

        if (clips.Length <= 0) Debug.Log("no clips error");

        int batchSize = 4; // Adjust batch size as needed for performance
        for (int i = 0; i < clips.Length; i += batchSize)
        {
            for (int j = 0; j < batchSize && (i + j) < clips.Length; j++)
            {
                AudioClip clip = clips[i + j];
                clipDictionary[clip.name] = clip;
            }
            // Yield control back to the main thread to avoid freezing
            yield return null;
        }
        Debug.Log("load complete");
    }

    private bool TryParseClipName(string clipName, out int num1, out int num2)
    {
        // Split the clip name by the comma
        string[] parts = clipName.Split(',');

        // Try to parse both parts into integers
        if (parts.Length == 2 && int.TryParse(parts[0], out num1) && int.TryParse(parts[1], out num2))
        {
            return true;
        }

        // If parsing fails, return false and default values
        num1 = 0;
        num2 = 0;
        return false;
    }

    // Method to set the volume
    public void SetGlobalVolume(float volume)
    {
        volume = Mathf.Clamp(volume, 0f, 1f);

        for (int i = 0; i < audioSources.Length; i++)
        {
            if (audioSources[i] != null) audioSources[i].volume = originalVolumes[i] * volume;
        }

    }

    #endregion

    #region playing sounds

    private bool playOrNot(string cName, int chrdSolDrum)
    {

        float timeDifference = Mathf.Abs(Time.time - tempoStuff.nextEighthNoteTime);

        // Define a tolerance value (e.g., 10% of the note interval)
        float noteTolerance = tempoStuff.eighthNoteInterval * 0.1f;  //10% tolerance??
        float oneMeasureTolerance = tempoStuff.eighthNoteInterval * 8 * 0.1f; // 10% of an eighth note in the measure tolerance??


        //basically the chord and drums are using the 8th note tolerance
        //if the time difference is within the tolerance range, OR a buffer is on, OR a chord was played and no other is queued
        switch (chrdSolDrum)
        {
            case 1: //CHORD!!!!
                if ((timeDifference <= oneMeasureTolerance) && noteBuffers.chordBuffOn) return true;

                else if (!noteBuffers.chordBuffOn && (chordSource.isPlaying && Mathf.Abs(chordSource.time) < 0.1f)) return true;
                break;
            case 2: //SOLO NOTES!!!
                if ((timeDifference <= noteTolerance) && noteBuffers.eighthBuffOn) return true;
                break;
            case 3: //DRUMS!!!
                if ((timeDifference <= oneMeasureTolerance) && noteBuffers.drumBuffOn) return true;
                break;
        }


        //otherwise, buffer for next time; these ALL return FALSE!!!   
        switch (chrdSolDrum)
        {
            case 1: //CHORD
                if (!noteBuffers.chordBuffOn)
                { //if its a chord
                    noteBuffers.chordBuffOn = true;
                    bufferedChordForLoops.clipName = cName;
                    bufferedChordForLoops.chordLoop = false;
                }
                break;

            case 2: //SOLO
                if (!noteBuffers.eighthBuffOn)
                {
                    noteBuffers.eighthBuffOn = true;
                    bufferedSphereClipName = cName;
                }
                break;

            case 3: //DRUMS
                if (!noteBuffers.drumBuffOn)
                {
                    noteBuffers.drumBuffOn = true;
                    bufferedSphereClipName = cName;
                }
                break;
        }

        Debug.Log("deciding NOT to play audio YET...");
        return false; //default
    }

    //play audio clip methods start
    public IEnumerator PlayAudioClip(string clipName, bool isChord)
    {

        //actual audio playing code----
        if (playOrNot(clipName, (isChord) ? 1 : 2))
        {
            if (isChord)
            {
                StopChordClip();

                if (chordClips.TryGetValue(clipName, out AudioClip clip))
                {
                    //loop chord code
                    Debug.Log("PLAYING CHORD!!!!!");
                    bufferedChordForLoops.chordLoop = true;
                    bufferedChordForLoops.clipName = clipName;

                    yield return PlayClip(clip, true);
                }
                else Debug.LogWarning($"Chord audio clip not found: {clipName}");

            }
            else if (clipName == "drumtrack1" || clipName == "drumTrack2")
            {
                if (pleaseStop) { pleaseStop = false; }
                else { StartCoroutine(PlayNewDrumBacking(clipName)); }
            }
            else
            {
                StopSoloClip();

                if (soloClips.TryGetValue(clipName, out AudioClip clip))
                {
                    Debug.Log("PLAYING SOLO NOTE!!!!!");
                    yield return PlayClip(clip, false);
                }
                else Debug.LogWarning($"Solo audio clip not found: {clipName}");
            }
        }
        //-------
    }//end of playAudioClip

    public IEnumerator PlayClip(AudioClip clip, bool isChord)
    {
        AudioSource selectedSource = soloSource.isPlaying ? soloSource2 : soloSource;
        if (isChord) selectedSource = chordSource; //dialogue only plays on clip source 3

        selectedSource.clip = clip;
        if (isChord) selectedSource.volume = 1f;
        else selectedSource.volume = 0.7f;
        selectedSource.loop = false;//(isChord ? true : false); // No looping for sound effects
        selectedSource.Play();

        // Wait until the clip finishes playing
        while (selectedSource.isPlaying)
        {
            yield return null;
        }

        //chord loop code
        if (bufferedChordForLoops.chordLoop) StartCoroutine(PlayAudioClip(bufferedChordForLoops.clipName, true));

        StartCoroutine(FadeOutAndStopCoroutine(selectedSource, 0.1f));
    }
    //play audio clip methods end


    public IEnumerator PlayStuntedClip(string clipName)
    {
        Dictionary<string, AudioClip> clipDictionary = soloClips;

        if (clipDictionary.TryGetValue(clipName, out AudioClip clip))
        {
            AudioSource selectedSource = soloSource;

            selectedSource.clip = clip;
            selectedSource.volume = 1f;
            selectedSource.loop = false; // No looping for sound effects
            selectedSource.Play();

            // Wait until the clip finishes playing
            while (selectedSource.isPlaying)
            {
                yield return null;
            }
            StartCoroutine(FadeOutAndStopCoroutine(selectedSource, 0.1f));
        }
        else
        {
            Debug.LogWarning($"Audio clip not found: {clipName}");
        }
    }

    public IEnumerator PlayNewDrumBacking(string clipName)
    {
        // Debug.Log("playing drum backin");

        if (playOrNot(clipName, 3))
        { //shouldn't actually use the chord thing
            StopDrumBacking();
            // StopSoloClip();
            // StopChordClip();
            if (drumBacking.TryGetValue(clipName, out AudioClip clip))
            {
                Debug.Log("PLAYING DRUMS!!!!!");
                drumBackingSource.clip = clip;
                drumBackingSource.volume = 1f;
                drumBackingSource.loop = false; // Background music typically loops
                drumBackingSource.Play();

                // Wait until the clip finishes playing (or looping is stopped externally)
                while (drumBackingSource.isPlaying)
                {
                    yield return null;
                }

                if (pleaseStop) { pleaseStop = false; } 
                else { StartCoroutine(PlayNewDrumBacking(clipName)); }
            }
            else
            {
                Debug.LogWarning($"Drum music clip not found: {clipName}");
            }
        }
    }

    #endregion

    #region stopping sounds
    //stop sound
    public void StopChordClip()
    {
        if (chordSource.isPlaying)
        {
            chordSource.Stop();
        }
    }

    public void StopSoloClip()
    {
        if (soloSource.isPlaying)
        {
            soloSource.Stop();
        }
        if (soloSource2.isPlaying)
        {
            soloSource2.Stop();
        }
    }

    public void StopDrumBacking()
    {
        if (drumBackingSource.isPlaying)
        {
            drumBackingSource.Stop();
        }
    }

    //fade then stop
    public void FadeOutAndStopAudioClip(float fadeDuration, bool isSE = true)
    {
        AudioSource source = isSE ? soloSource : drumBackingSource;
        StartCoroutine(FadeOutAndStopCoroutine(source, fadeDuration));
    }

    private IEnumerator FadeOutAndStopCoroutine(AudioSource source, float fadeDuration)
    {
        float startVolume = source.volume;

        while (source.volume > 0)
        {
            source.volume -= startVolume * Time.deltaTime / fadeDuration;
            yield return null;
        }

        source.Stop();
        source.volume = startVolume; //reset volume for future use
    }



    #endregion

}
