using UnityEngine;
using System.Collections; // Required for Coroutines

public class MenuMusicBehaviour : MonoBehaviour
{
    [Tooltip("Drag all your menu music audio clips here.")]
    [SerializeField] private AudioClip[] menuTracks;

    [Tooltip("Drag the AudioSource component from this GameObject here.")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Optional: Volume for the menu music.")]
    [Range(0f, 1f)]
    public float volume = 0.6f; // Default volume, adjustable in Inspector

    // --- Private Variables ---
    private int lastPlayedIndex = -1; // Keep track of the last played song to avoid immediate repeats

    void Awake()
    {
        // Ensure AudioSource is assigned, or try to get it from this GameObject
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogError("MenuMusicBehaviour: No AudioSource found or assigned on this GameObject!", this);
                enabled = false; // Disable the script if no AudioSource is present
                return;
            }
        }

        // Set initial AudioSource properties
        audioSource.loop = false; // Important: We want individual songs to not loop, so we can pick a new one
        audioSource.volume = volume;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Basic validation
        if (menuTracks == null || menuTracks.Length == 0)
        {
            Debug.LogWarning("MenuMusicBehaviour: No menu tracks assigned! Please assign AudioClips to the 'Menu Tracks' array.", this);
            return;
        }

        // Start playing the first random song
        PlayRandomTrack();
    }

    // Update is called once per frame
    void Update()
    {
        // Check if the current song has finished playing
        if (!audioSource.isPlaying)
        {
            // If it's not playing, and we have tracks to play, play another random one
            if (menuTracks != null && menuTracks.Length > 0)
            {
                PlayRandomTrack();
            }
        }
    }

    /// <summary>
    /// Plays a random audio track from the menuTracks array.
    /// Tries to avoid playing the same track twice in a row if there's more than one track.
    /// </summary>
    private void PlayRandomTrack()
    {
        if (menuTracks == null || menuTracks.Length == 0) return; // Safety check

        int randomIndex = -1;
        if (menuTracks.Length == 1)
        {
            randomIndex = 0; // If only one track, just play that one
        }
        else
        {
            // Pick a new random index, ensuring it's not the same as the last played
            do
            {
                randomIndex = Random.Range(0, menuTracks.Length);
            } while (randomIndex == lastPlayedIndex); // Loop until a different index is found
        }

        lastPlayedIndex = randomIndex; // Update last played index

        audioSource.clip = menuTracks[randomIndex];
        audioSource.Play();
        Debug.Log($"Now playing menu music: {audioSource.clip.name}");
    }
}