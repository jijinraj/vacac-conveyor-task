using System.Collections.Generic;
using UnityEngine;
using TMPro;

// ==========================================================
// MUSIC ATTRIBUTION
// ==========================================================
//
// Soundtrack currently includes:
//
// "8bit Dungeon Boss" - Kevin MacLeod
// "Pixelland" - Kevin MacLeod
// "Mountain Trials" - Joshua McLean
// "Chopsticks" - Jorge Hernandez
// "Virtual Boy" - Krayzius & Brainstorm
//
// Full creator, source, and licensing information must be
// retained in the repository's CREDITS.md file.
//
// Do not remove soundtrack attribution when redistributing
// this project.
// ==========================================================

[RequireComponent(typeof(AudioSource))]
public class RetroAudioPlayer : MonoBehaviour
{
    // ======================================================
    // MUSIC LIBRARY
    // ======================================================

    [Header("Music Library")]
    public AudioClip[] tracks;


    // ======================================================
    // PLAYBACK SETTINGS
    // ======================================================

    [Header("Playback")]

    [Range(0f, 1f)]
    public float volume = 0.30f;

    public bool playOnStart = true;

    public bool avoidImmediateRepeat = true;


    // ======================================================
    // OPTIONAL UI REFERENCES
    // ======================================================

    [Header("UI")]
    public TMP_Text trackTitleText;
    public TMP_Text playPauseButtonText;
    public TMP_Text muteButtonText;


    // ======================================================
    // INTERNAL STATE
    // ======================================================

    private AudioSource audioSource;

    private int currentTrackIndex = -1;

    private readonly List<int> history =
        new List<int>();

    private int historyPosition = -1;

    private bool isMusicPaused = false;

    private bool isMuted = false;


    // ======================================================
    // UNITY
    // ======================================================

    void Awake()
    {
        audioSource =
            GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = volume;
    }


    void Start()
    {
        UpdateUI();

        if (
            playOnStart &&
            tracks != null &&
            tracks.Length > 0
        )
        {
            PlayRandomTrack(true);
        }
    }


    void Update()
    {
        if (
            audioSource == null ||
            currentTrackIndex < 0 ||
            isMusicPaused
        )
        {
            return;
        }


        // Current song finished naturally.
        if (
            !audioSource.isPlaying &&
            audioSource.clip != null
        )
        {
            PlayRandomTrack(true);
        }
    }


    // ======================================================
    // RANDOM TRACK SELECTION
    // ======================================================

    void PlayRandomTrack(
        bool addToHistory
    )
    {
        if (
            tracks == null ||
            tracks.Length == 0
        )
        {
            Debug.LogWarning(
                "RetroAudioPlayer has no tracks assigned."
            );

            return;
        }


        int nextIndex;


        if (tracks.Length == 1)
        {
            nextIndex = 0;
        }
        else
        {
            do
            {
                nextIndex =
                    Random.Range(
                        0,
                        tracks.Length
                    );
            }
            while (
                avoidImmediateRepeat &&
                nextIndex == currentTrackIndex
            );
        }


        PlayTrack(
            nextIndex,
            addToHistory
        );
    }


    // ======================================================
    // PLAY SPECIFIC TRACK
    // ======================================================

    void PlayTrack(
        int index,
        bool addToHistory
    )
    {
        if (
            tracks == null ||
            index < 0 ||
            index >= tracks.Length ||
            tracks[index] == null
        )
        {
            return;
        }


        currentTrackIndex =
            index;


        if (addToHistory)
        {
            // If we moved backwards through history and then
            // choose a new song, remove the old forward path.

            if (
                historyPosition <
                history.Count - 1
            )
            {
                history.RemoveRange(
                    historyPosition + 1,
                    history.Count -
                    historyPosition -
                    1
                );
            }


            history.Add(
                currentTrackIndex
            );


            historyPosition =
                history.Count - 1;
        }


        audioSource.clip =
            tracks[currentTrackIndex];

        audioSource.volume =
            volume;

        audioSource.mute =
            isMuted;

        isMusicPaused =
            false;


        audioSource.Play();

        UpdateUI();
    }


    // ======================================================
    // NEXT
    // ======================================================

    public void NextTrack()
    {
        // If we previously pressed Previous,
        // move forward through existing history first.

        if (
            historyPosition <
            history.Count - 1
        )
        {
            historyPosition++;


            PlayTrack(
                history[historyPosition],
                false
            );


            return;
        }


        // Otherwise choose a fresh random song.
        PlayRandomTrack(true);
    }


    // ======================================================
    // PREVIOUS
    // ======================================================

    public void PreviousTrack()
    {
        if (history.Count == 0)
            return;


        // Common music-player behaviour:
        // if current track has played > 3 seconds,
        // Previous restarts the current song.

        if (
            audioSource.time > 3f
        )
        {
            audioSource.time = 0f;

            audioSource.Play();

            isMusicPaused = false;

            UpdateUI();

            return;
        }


        if (historyPosition > 0)
        {
            historyPosition--;


            PlayTrack(
                history[historyPosition],
                false
            );
        }
        else
        {
            audioSource.time = 0f;

            audioSource.Play();

            isMusicPaused = false;

            UpdateUI();
        }
    }


    // ======================================================
    // PLAY / PAUSE
    // ======================================================

    public void TogglePlayPause()
    {
        // No track has started yet.
        if (audioSource.clip == null)
        {
            PlayRandomTrack(true);

            return;
        }


        if (isMusicPaused)
        {
            audioSource.UnPause();

            isMusicPaused = false;
        }
        else
        {
            audioSource.Pause();

            isMusicPaused = true;
        }


        UpdateUI();
    }


    // ======================================================
    // RANDOM NOW
    // ======================================================

    public void PlayRandomTrackNow()
    {
        PlayRandomTrack(true);
    }


    // ======================================================
    // MUTE
    // ======================================================

    public void ToggleMute()
    {
        isMuted =
            !isMuted;


        audioSource.mute =
            isMuted;


        UpdateUI();
    }


    // ======================================================
    // UI
    // ======================================================

    void UpdateUI()
    {
        if (trackTitleText != null)
        {
            if (
                currentTrackIndex >= 0 &&
                tracks != null &&
                currentTrackIndex < tracks.Length &&
                tracks[currentTrackIndex] != null
            )
            {
                trackTitleText.text =
                    FormatTrackName(
                        tracks[currentTrackIndex].name
                    );
            }
            else
            {
                trackTitleText.text =
                    "No Track";
            }
        }


        if (playPauseButtonText != null)
        {
            playPauseButtonText.text =
                isMusicPaused
                ? "PLAY"
                : "PAUSE";
        }


        if (muteButtonText != null)
        {
            muteButtonText.text =
                isMuted
                ? "UNMUTE"
                : "MUTE";
        }
    }


    // ======================================================
    // PRETTY TRACK NAME
    // ======================================================

    string FormatTrackName(
        string rawName
    )
    {
        if (
            string.IsNullOrEmpty(
                rawName
            )
        )
        {
            return "Unknown Track";
        }


        return rawName.Replace(
            "_",
            " "
        );
    }
}