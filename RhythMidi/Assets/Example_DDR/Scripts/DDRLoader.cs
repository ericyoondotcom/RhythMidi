using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using RhythMidi;
using UnityEngine.Events;
using System.IO;

public class DDRLoader : MonoBehaviour
{
    /*
        MIDI MAP:
        24 - Triggers every beat (C1)
        36 - Left (C2)
        37 - Down (C#2)
        38 - Up (D2)
        39 - Right (D#2)
    */


    [Header("References")]
    public RhythMidiController rhythMidi;
    public HitWindow hitWindow;
    public Transform canvas;
    public AudioClip hitSound;
    public GameObject startGameButton;

    [Header("Settings")]
    public string chartToPlayName = "The Pixel Pirates";
    public string chartDirectoryName = "ExampleChart";
    public float fallingNotesTime = 1f;

    [Header("Prefabs")]
    public GameObject leftNotePrefab;
    public GameObject downNotePrefab;
    public GameObject upNotePrefab;
    public GameObject rightNotePrefab;
    public GameObject positiveFeedbackPrefab;
    public GameObject negativeFeedbackPrefab;

    [Header("Target Positions")]
    public RectTransform feedbackOrigin;
    public RectTransform leftNoteSpawn;
    public RectTransform downNoteSpawn;
    public RectTransform upNoteSpawn;
    public RectTransform rightNoteSpawn;
    public RectTransform leftNoteTarget;
    public RectTransform downNoteTarget;
    public RectTransform upNoteTarget;
    public RectTransform rightNoteTarget;

    void Start()
    {
        hitWindow.OnNoteMissed += OnNoteMissed;
        
        // Workaround for WebGL where System.IO GetDirectories is not available
        UnityEvent loaded = new UnityEvent();
        loaded.AddListener(() => startGameButton.SetActive(true));
        string path = Path.Combine(Application.streamingAssetsPath, "Charts", chartDirectoryName);
        rhythMidi.LoadChart(path, loaded);

        rhythMidi.onFinishedLoading.AddListener(StartGame);
        rhythMidi.CreateNoteNotifier(fallingNotesTime).OnNote += SpawnSprite;
    }

    void OnNoteMissed(Note note)
    {
        if(note.NoteNumber < 36 || note.NoteNumber > 39) return;
        Instantiate(negativeFeedbackPrefab, feedbackOrigin);
    }

    public void StartGame()
    {
        rhythMidi.PrepareChart(chartToPlayName);
        rhythMidi.PlayChart();
        startGameButton.SetActive(false);
    }

    private void SpawnSprite(Note note)
    {
        GameObject prefab = null;
        RectTransform spawn = null;
        RectTransform target = null;
        switch(note.NoteNumber)
        {
            case 36:
                prefab = leftNotePrefab;
                spawn = leftNoteSpawn;
                target = leftNoteTarget;
                break;
            case 37:
                prefab = downNotePrefab;
                spawn = downNoteSpawn;
                target = downNoteTarget;
                break;
            case 38:
                prefab = upNotePrefab;
                spawn = upNoteSpawn;
                target = upNoteTarget;
                break;
            case 39:
                prefab = rightNotePrefab;
                spawn = rightNoteSpawn;
                target = rightNoteTarget;
                break;
            default:
                return;
        }
        GameObject sprite = Instantiate(prefab, canvas);
        NoteSprite behavior = sprite.GetComponent<NoteSprite>();
        behavior.startPosition = spawn;
        behavior.endPosition = target;
        behavior.totalTime = fallingNotesTime;
    }

    void Update()
    {
        bool hit = false;
        if(Input.GetKeyDown(KeyCode.LeftArrow) && hitWindow.CheckHit(36)) hit = true;
        if(Input.GetKeyDown(KeyCode.DownArrow) && hitWindow.CheckHit(37)) hit = true;
        if(Input.GetKeyDown(KeyCode.UpArrow) && hitWindow.CheckHit(38)) hit = true;
        if(Input.GetKeyDown(KeyCode.RightArrow) && hitWindow.CheckHit(39)) hit = true;

        if(hit)
        {
            Instantiate(positiveFeedbackPrefab, feedbackOrigin);
            AudioSource.PlayClipAtPoint(hitSound, Camera.main.transform.position);
        }
    }
}
