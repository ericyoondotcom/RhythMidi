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
    public class ChartResource
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Mapper { get; set; }

        public string MidiFilepath { get; set; }
        public AudioClip Track { get; set; }
    }

    public class NoteNotifier
    {
        public float TimeInAdvance { get; private set; }
        public UnityAction<Note> OnNote { get; set; }
        public Queue<Note> Notes { get; private set;}

        public NoteNotifier(float timeInAdvance)
        {
            TimeInAdvance = timeInAdvance;
            Notes = new Queue<Note>();
            OnNote = delegate {};
        }

        public void EnqueueNotes(IEnumerable<Note> noteList)
        {
            Notes.Clear();
            foreach(Note note in noteList) Notes.Enqueue(note);
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

        private void Start()
        {
            if(chartsPath.Length > 0) LoadAllFromStreamingAssets(chartsPath);
        }

        /// <summary>
        /// Loads a single chart from a specified filepath to the chart directory.
        /// </summary>
        /// <param name="path">The absolute filepath to the chart.</param>
        public void LoadChart(string path)
        {
            StartCoroutine(LoadChart_Coroutine(path));
        }

        public void LoadAllFromStreamingAssets(string chartsPath)
        {
            StartCoroutine(LoadAllFromStreamingAssets_Coroutine(chartsPath));
        }

        /// <summary>
        /// Loads all charts from a specified directory inside StreamingAssets.
        /// </summary>
        /// <param name="chartsPath">The path, relative to StreamingAssets, of the directory that contains all chart directories.</param>
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

        private IEnumerator LoadChart_Coroutine(string directory)
        {

            string manifestPath = Path.Combine(directory, "manifest.json");
            string manifestData = File.ReadAllText(manifestPath);
            
            JSONNode manifest = JSON.Parse(manifestData);
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

            string trackPath = Path.Combine(directory, track);
            yield return LoadTrackAudioClip(trackPath, chart);

            loadedCharts.Add(chart);
        }

        private IEnumerator LoadTrackAudioClip(string path, ChartResource chart)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.MPEG))
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

        public ChartResource GetChartByName(string title)
        {
            return loadedCharts.Find(x => x.Title == title);
        }

        public void PrepareChart(string title)
        {
            currentChart = GetChartByName(title);
            if(currentChart == null) throw new Exception("Chart not found.");

            midiData = MidiFile.Read(currentChart.MidiFilepath);
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

        public NoteNotifier CreateNoteNotifier(float timeInAdvance)
        {
            NoteNotifier noteNotifier = new NoteNotifier(timeInAdvance);
            noteNotifiers.Add(noteNotifier);
            return noteNotifier;
        }

        public void PlayChart()
        {
            if(currentChart == null) throw new Exception("No chart loaded.");
            if(audioSource == null) throw new Exception("Audio source not set.");

            audioSource.Play();
            IsPlaying = true;
        }

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