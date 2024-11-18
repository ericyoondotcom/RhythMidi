using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using System;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.Events;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace RhythMidi
{
    class Ref<T>
    {
        public T value;
    }

    public class ChartResource
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Mapper { get; set; }

        public string MidiFilepath { get; set; }
        public byte[] MidiFileData { get; set; }
        public AudioClip Track { get; set; }
    }

    public class NoteNotifier
    {
        public float TimeInAdvance { get; private set; }
        public UnityAction<Note> OnNote { get; set; }
        public Queue<Note> Notes { get; private set;}
        RhythMidiController.NoteFilter noteFilter;

        public NoteNotifier(float timeInAdvance, RhythMidiController.NoteFilter noteFilter = null)
        {
            TimeInAdvance = timeInAdvance;
            Notes = new Queue<Note>();
            OnNote = delegate {};
            this.noteFilter = noteFilter;
        }

        public void EnqueueNotes(IEnumerable<Note> noteList)
        {
            Notes.Clear();
            foreach(Note note in noteList)
            {
                if(noteFilter != null && !noteFilter(note)) continue;
                Notes.Enqueue(note);
            }
        }

        public void Clear()
        {
            Notes.Clear();
        }
    }

    public class RhythMidiController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The path, relative to StreamingAssets, of the directory that contains all chart directories. Leave blank to not load any charts on Start.")]
        private string chartsPath = "Charts";

        public AudioSource audioSource;

        [Tooltip("Finished when LoadAllFromStreamingAssets finishes.")]
        public UnityEvent onFinishedLoading = new UnityEvent();

        public List<NoteNotifier> noteNotifiers = new List<NoteNotifier>();
        public bool IsPlaying { get; private set; }

        private List<ChartResource> loadedCharts = new List<ChartResource>();
        private ChartResource currentChart;
        private MidiFile midiData;
        public TempoMap CurrentTempoMap { get; private set; }

        public delegate bool NoteFilter(Note note);

        private void Start()
        {
            if(chartsPath.Length > 0) LoadAllFromStreamingAssets(chartsPath);
        }

        /// <summary>
        /// Loads a single chart from a specified filepath to the chart directory.
        /// </summary>
        /// <param name="path">The absolute filepath to the chart.</param>
        /// <param name="onChartLoaded">An optional UnityEvent to invoke when the chart is loaded.</param>
        public void LoadChart(string path, UnityEvent onChartLoaded = null)
        {
            StartCoroutine(LoadChart_Coroutine(path, onChartLoaded));
        }

        /// <summary>
        /// Loads all charts from a specified directory inside StreamingAssets.
        /// 
        /// NOTE: This method is only available on platforms with System.IO, i.e. not WebGL.
        /// </summary>
        /// <param name="chartsPath">The path, relative to StreamingAssets, of the directory that contains all chart directories.</param>
        public void LoadAllFromStreamingAssets(string chartsPath)
        {
            StartCoroutine(LoadAllFromStreamingAssets_Coroutine(chartsPath));
        }

        
        private IEnumerator LoadAllFromStreamingAssets_Coroutine(string chartsPath)
        {
            string chartDirPath = Path.Combine(Application.streamingAssetsPath, chartsPath);
            string[] chartDirectories = Directory.GetDirectories(chartDirPath);
            foreach(string directory in chartDirectories)
            {
                yield return LoadChart_Coroutine(directory);
            }
            onFinishedLoading.Invoke();
        }

        private IEnumerator LoadChart_Coroutine(string directory, UnityEvent onChartLoaded = null)
        {

            string manifestPath = Path.Combine(directory, "manifest.json");

            Ref<string> manifestData = new Ref<string>(); // Ref? it's how you do out params in coroutines
            yield return ReadFile(manifestPath, manifestData);
            
            JSONNode manifest = JSON.Parse(manifestData.value);
            string title = manifest["title"];
            string artist = manifest["artist"];
            string mapper = manifest["mapper"];
            string midi = manifest["midi"];
            string track = manifest["track"];

            ChartResource chart = new ChartResource();
            chart.Title = title;
            chart.Artist = artist;
            chart.Mapper = mapper;
            
            chart.MidiFilepath = Path.Combine(directory, midi);
            yield return LoadMidiFile(chart.MidiFilepath, chart);

            string trackPath = Path.Combine(directory, track);
            yield return LoadTrackAudioClip(trackPath, chart);

            loadedCharts.Add(chart);
            if(onChartLoaded != null) onChartLoaded.Invoke();
        }

        private IEnumerator ReadFile(string path, Ref<string> data)
        {
            string platformCorrectedPath = "file://" + path;
#if UNITY_WEBGL && !UNITY_EDITOR
            platformCorrectedPath = path;
#endif
            using (UnityWebRequest www = UnityWebRequest.Get(platformCorrectedPath))
            {
                yield return www.SendWebRequest();
                while(!www.isDone)
                {
                    yield return null;
                }
                if(www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    throw new Exception("Error reading file: " + www.error);
                }
                else
                {
                    data.value = www.downloadHandler.text;
                }
            }
        }

        private IEnumerator LoadTrackAudioClip(string path, ChartResource chart)
        {
            string platformCorrectedPath = "file://" + path;
#if UNITY_WEBGL && !UNITY_EDITOR
            platformCorrectedPath = path;
#endif
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(platformCorrectedPath, AudioType.MPEG))
            {
                yield return www.SendWebRequest();
                while(!www.isDone)
                {
                    yield return null;
                }
                if(www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    throw new Exception("Error loading audio clip: " + www.error);
                }
                else
                {
                    chart.Track = DownloadHandlerAudioClip.GetContent(www);
                }
            }
        }

        private IEnumerator LoadMidiFile(string path, ChartResource chart)
        {
            string platformCorrectedPath = "file://" + path;
#if UNITY_WEBGL && !UNITY_EDITOR
            platformCorrectedPath = path;
#endif
            using (UnityWebRequest www = UnityWebRequest.Get(platformCorrectedPath))
            {
                yield return www.SendWebRequest();
                while(!www.isDone)
                {
                    yield return null;
                }
                if(www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    throw new Exception("Error loading MIDI file: " + www.error);
                }
                else
                {
                    chart.MidiFileData = www.downloadHandler.data;
                }
            }
        }

        /// <summary>
        /// Gets the information about a chart by its name. FYI, O(n) complexity.
        /// </summary>
        /// <param name="title">The name of the song</param>
        /// <returns>The chart info</returns>
        public ChartResource GetChartByName(string title)
        {
            return loadedCharts.Find(x => x.Title == title);
        }

        /// <summary>
        /// Loads a chart's notes into memory. This must be called before PlayChart.
        /// </summary>
        /// <param name="title">The name of the song</param>
        /// <exception cref="Exception"></exception>
        public void PrepareChart(string title)
        {
            currentChart = GetChartByName(title);
            if(currentChart == null) throw new Exception("Chart not found.");

            using (MemoryStream memoryStream = new MemoryStream(currentChart.MidiFileData))
            {
                midiData = MidiFile.Read(memoryStream);
            }

            IEnumerable<Note> allNotes = midiData.GetNotes();
            CurrentTempoMap = midiData.GetTempoMap();

            if(noteNotifiers.Count == 0)
            {
                Debug.LogWarning("There are no note notifiers. This script will do nothing. Call CreateNoteNotifier before calling PrepareChart.");
            }

            foreach(NoteNotifier noteNotifier in noteNotifiers)
            {
                noteNotifier.EnqueueNotes(allNotes);
            }

            audioSource.clip = currentChart.Track;

            IsPlaying = false;
            audioSource.Stop();
        }
        
        /// <summary>
        /// Creates a NoteNotifier that will invoke a UnityAction `timeInAdvance` seconds before the note is played.
        /// </summary>
        /// <param name="timeInAdvance">The number of seconds to look ahead. This can be negative.</param>
        /// <returns>A note notifier. Add a listener to the OnNote property.</returns>
        public NoteNotifier CreateNoteNotifier(float timeInAdvance, NoteFilter noteFilter = null)
        {
            NoteNotifier noteNotifier = new NoteNotifier(timeInAdvance, noteFilter);
            noteNotifiers.Add(noteNotifier);
            return noteNotifier;
        }

        /// <summary>
        /// Plays the chart that was loaded with PrepareChart.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void PlayChart()
        {
            if(currentChart == null) throw new Exception("No chart loaded.");
            if(audioSource == null) throw new Exception("Audio source not set.");

            audioSource.Play();
            IsPlaying = true;
        }

        /// <summary>
        /// Halts playback of a chart.
        /// </summary>
        public void StopChart()
        {
            audioSource.Stop();
            IsPlaying = false;
            audioSource.time = 0;
            audioSource.clip = null;
            foreach(NoteNotifier noteNotifier in noteNotifiers) noteNotifier.Clear();
        }


        void Update()
        {
            if(!IsPlaying) return;
            if(!audioSource.isPlaying) return;
            foreach(NoteNotifier noteNotifier in noteNotifiers)
            {
                while(noteNotifier.Notes.Count > 0)
                {
                    Note note = noteNotifier.Notes.Peek();
                    MetricTimeSpan metricTime = note.TimeAs<MetricTimeSpan>(CurrentTempoMap);

                    if(metricTime.TotalMilliseconds > (audioSource.time + noteNotifier.TimeInAdvance) * 1000) break;

                    noteNotifier.Notes.Dequeue();
                    noteNotifier.OnNote.Invoke(note);
                }
            }
        }
    }
}