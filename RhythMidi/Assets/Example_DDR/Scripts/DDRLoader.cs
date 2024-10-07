using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using RhythMidi;

public class DDRLoader : MonoBehaviour
{
    [Header("References")]
    public RhythMidiController rhythMidi;
    public HitWindow hitWindow;
    public Transform canvas;

    [Header("Settings")]
    public string chartToPlayName = "ExampleChart";
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
        rhythMidi.onFinishedLoading.AddListener(StartGame);
    }

    void OnNoteMissed(Note note)
    {
        if(note.NoteNumber < 36 || note.NoteNumber > 39) return;
        Instantiate(negativeFeedbackPrefab, feedbackOrigin);
    }

    public void StartGame()
    {
        rhythMidi.CreateNoteNotifier(fallingNotesTime).OnNote += SpawnSprite;
        rhythMidi.PrepareChart(chartToPlayName);
        rhythMidi.PlayChart();
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
        if(Input.GetKeyDown(KeyCode.LeftArrow) && hitWindow.CheckHit(36))
            Instantiate(positiveFeedbackPrefab, feedbackOrigin);
        if(Input.GetKeyDown(KeyCode.DownArrow) && hitWindow.CheckHit(37))
            Instantiate(positiveFeedbackPrefab, feedbackOrigin);
        if(Input.GetKeyDown(KeyCode.UpArrow) && hitWindow.CheckHit(38))
            Instantiate(positiveFeedbackPrefab, feedbackOrigin);
        if(Input.GetKeyDown(KeyCode.RightArrow) && hitWindow.CheckHit(39))
            Instantiate(positiveFeedbackPrefab, feedbackOrigin);
    }
}
