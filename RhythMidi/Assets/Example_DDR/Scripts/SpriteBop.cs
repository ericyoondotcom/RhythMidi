using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using RhythMidi;

public class SpriteBop : MonoBehaviour
{
    public float timeToBop = 0.2f;
    public float bopSize = 0.8f;
    public RhythMidiController rhythMidi;
    float t = 0;
    Vector3 bopStart;
    Vector3 bopEnd;
    void Start()
    {
        rhythMidi.CreateNoteNotifier(0f).OnNote += OnNote;
        bopStart = transform.localScale;
        bopEnd = bopStart * bopSize;
    }

    void OnNote(Note note)
    {
        // 24 is the note that triggers every beat
        if(note.NoteNumber != 24) return;
        t = timeToBop;
    }

    void Update()
    {
        if(t > 0) {
            t -= Time.deltaTime;
        }
        transform.localScale = Vector3.Lerp(bopEnd, bopStart, t / timeToBop);
    }
}
