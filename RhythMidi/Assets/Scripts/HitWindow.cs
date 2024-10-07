using System.Collections;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

namespace RhythMidi
{
    public class HitWindow : MonoBehaviour
    {
        public RhythMidiController rhythMidi;

        // Tried-and-true values
        public float coyoteTimeTooEarly = 0.1f;
        public float coyoteTimeTooLate = 0.2f;

        public List<Note> validWindow = new List<Note>();

        public UnityAction<Note> OnNoteMissed { get; set; } = delegate {};

        void Start()
        {
            rhythMidi.CreateNoteNotifier(coyoteTimeTooEarly).OnNote += NoteEnterHitWindow;
            rhythMidi.CreateNoteNotifier(-coyoteTimeTooLate).OnNote += NoteExitHitWindow;
        }

        public bool CheckHit(int noteNum, bool removeIfHit = true)
        {
            foreach(Note note in validWindow)
            {
                if(note.NoteNumber == noteNum)
                {
                    if(removeIfHit) validWindow.Remove(note);
                    return true;
                }
            }
            return false;
        }

        void NoteEnterHitWindow(Note note)
        {
            validWindow.Add(note);
        }

        void NoteExitHitWindow(Note note)
        {
            if(validWindow.Remove(note))
            {
                OnNoteMissed.Invoke(note);
            }
        }
    }
}
