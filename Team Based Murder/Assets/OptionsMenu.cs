using UnityEngine;
using UnityEngine.Audio; // Required for AudioMixer
using UnityEngine.UI;   // Required for UI elements like Slider

public class OptionsMenu : MonoBehaviour
{
    // --- UI Elements ---
    [Header("UI Elements")]
    [SerializeField] private Slider MasterSlide;
    [SerializeField] private Slider MusicSlide;
    [SerializeField] private Slider SoundSlide;
    [SerializeField] private Slider VoiceSlide;

    // Removed: Dropdown VideoApplicationScreenType;
    // Removed: Dropdown TypewriterEffectSoundDropdown;

    [SerializeField] private GameObject OptionsScreen; // The parent GameObject for the entire options UI

    // --- Audio Mixer ---
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer TheMixer; // The exposed params are: Master, Music, Sound, Voice

    // --- Temporary Setting Storage ---
    // These store changes until Apply/OK is pressed
    private float tempMasterVolume;
    private float tempMusicVolume;
    private float tempSoundVolume;
    private float tempVoiceVolume;
    // Removed: FullScreenMode tempFullScreenMode; 
    // Removed: int tempResolutionIndex; 
    // Removed: bool tempTypewriterEffectSoundEnabled; 

    // --- PlayerPrefs Keys (Constants for good practice) ---
    private const string MASTER_VOLUME_KEY = "Master";
    private const string MUSIC_VOLUME_KEY = "Music";
    private const string SOUND_VOLUME_KEY = "Sound";
    private const string VOICE_VOLUME_KEY = "Voice";
    // Removed: const string FULLSCREEN_MODE_KEY = "FullscreenMode";
    // Removed: const string TYPEWRITER_SOUND_KEY = "TypewriterSoundEnabled"; 

    void Awake()
    {
        // Ensure the OptionsScreen starts hidden
        if (OptionsScreen != null)
        {
            OptionsScreen.SetActive(false);
        }

        // Load and apply settings on awake (initial load)
        LoadSettings(true);
    }

    void Start()
    {
        // Hook up slider events
        MasterSlide.onValueChanged.AddListener(OnMasterVolumeChanged);
        MusicSlide.onValueChanged.AddListener(OnMusicVolumeChanged);
        SoundSlide.onValueChanged.AddListener(OnSoundVolumeChanged);
        VoiceSlide.onValueChanged.AddListener(OnVoiceVolumeChanged);

        // Removed: Hook up dropdown events
    }

    // --- Volume Conversion Helpers ---
    /// <summary>
    /// Converts a linear volume value (0 to 1) to decibels (-80 to 0) for AudioMixer.
    /// </summary>
    private float LinearToDecibel(float linear)
    {
        return linear > 0.0001f ? Mathf.Log10(linear) * 20 : -80f;
    }

    /// <summary>
    /// Converts a decibel value (-80 to 0) to a linear volume value (0 to 1) for UI Sliders.
    /// </summary>
    private float DecibelToLinear(float decibel)
    {
        return Mathf.Pow(10, decibel / 20);
    }

    // --- UI Event Handlers (Apply changes to temporary variables and preview) ---
    public void OnMasterVolumeChanged(float value)
    {
        tempMasterVolume = value;
        TheMixer.SetFloat("Master", LinearToDecibel(tempMasterVolume));
        Debug.Log($"Master Volume Preview: {value}");
    }

    public void OnMusicVolumeChanged(float value)
    {
        tempMusicVolume = value;
        TheMixer.SetFloat("Music", LinearToDecibel(tempMusicVolume));
        Debug.Log($"Music Volume Preview: {value}");
    }

    public void OnSoundVolumeChanged(float value)
    {
        tempSoundVolume = value;
        TheMixer.SetFloat("Sound", LinearToDecibel(tempSoundVolume));
        Debug.Log($"Sound Volume Preview: {value}");
    }

    public void OnVoiceVolumeChanged(float value)
    {
        tempVoiceVolume = value;
        TheMixer.SetFloat("Voice", LinearToDecibel(tempVoiceVolume));
        Debug.Log($"Voice Volume Preview: {value}");
    }

    // Removed: OnFullScreenModeChanged
    // Removed: OnTypewriterSoundChanged

    // --- Options Panel Control ---
    public void OpenOptionsPanel()
    {
        OptionsScreen.SetActive(true);
        LoadSettings(false); // Load current saved settings into UI and temporary variables (not applying live, as they are already live)
        Debug.Log("Options Panel Opened.");
    }

    // --- Button Actions ---

    // Called by the "Apply" button
    public void ApplySettings()
    {
        SaveSettings(); // Save and apply the current temporary settings
        OptionsScreen.SetActive(false); // Close the panel
        Debug.Log("Settings Applied.");
        // OptionsScreen remains active
    }

    // Called by the "OK" button
    public void OKSettings()
    {
        SaveSettings(); // Save and apply the current temporary settings
        Debug.Log("Settings Applied and Panel Closed.");
        // OptionsScreen remains active
    }

    // Called by the "Cancel" button
    public void CancelSettings()
    {
        LoadSettings(true); // Revert to last saved settings (true means apply live too)
        OptionsScreen.SetActive(false); // Close the panel
        Debug.Log("Settings Canceled - Changes Discarded.");
    }

    // --- Core Save/Load/Apply Logic ---

    /// <summary>
    /// Loads settings from PlayerPrefs and updates UI.
    /// Optionally applies settings live (e.g., for initial load or cancel).
    /// </summary>
    /// <param name="applyLive">If true, immediately applies settings to game (e.g., volume).</param>
    private void LoadSettings(bool applyLive)
    {
        // Load volumes from PlayerPrefs (default to 0.7 linear, which is roughly -3dB)
        tempMasterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 0.7f);
        tempMusicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.7f);
        tempSoundVolume = PlayerPrefs.GetFloat(SOUND_VOLUME_KEY, 0.7f);
        tempVoiceVolume = PlayerPrefs.GetFloat(VOICE_VOLUME_KEY, 0.7f);

        // Removed: Load Fullscreen Mode
        // Removed: Load Typewriter Sound setting

        // Update UI elements
        MasterSlide.value = tempMasterVolume;
        MusicSlide.value = tempMusicVolume;
        SoundSlide.value = tempSoundVolume;
        VoiceSlide.value = tempVoiceVolume;

        // Removed: Set dropdown values and RefreshShownValue calls

        if (applyLive)
        {
            // Apply volumes immediately
            TheMixer.SetFloat("Master", LinearToDecibel(tempMasterVolume));
            TheMixer.SetFloat("Music", LinearToDecibel(tempMusicVolume));
            TheMixer.SetFloat("Sound", LinearToDecibel(tempSoundVolume));
            TheMixer.SetFloat("Voice", LinearToDecibel(tempVoiceVolume));

            // Removed: Apply screen mode
            // Removed: Update TypewriterEffectSoundManager
        }

        Debug.Log("Audio Settings Loaded into UI (and applied live if applyLive is true).");
    }

    /// <summary>
    /// Saves the current temporary settings to PlayerPrefs and applies them live.
    /// </summary>
    private void SaveSettings()
    {
        // Apply changes from temporary storage to game
        TheMixer.SetFloat("Master", LinearToDecibel(tempMasterVolume));
        TheMixer.SetFloat("Music", LinearToDecibel(tempMusicVolume));
        TheMixer.SetFloat("Sound", LinearToDecibel(tempSoundVolume));
        TheMixer.SetFloat("Voice", LinearToDecibel(tempVoiceVolume));

        // Removed: Apply screen mode
        // Removed: Update Typewriter sound setting

        // Save to PlayerPrefs
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, tempMasterVolume);
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, tempMusicVolume);
        PlayerPrefs.SetFloat(SOUND_VOLUME_KEY, tempSoundVolume);
        PlayerPrefs.SetFloat(VOICE_VOLUME_KEY, tempVoiceVolume);

        // Removed: Save Fullscreen Mode
        // Removed: Save Typewriter Sound setting

        PlayerPrefs.Save(); // Don't forget to save!
        Debug.Log("Audio Settings Saved to PlayerPrefs.");
    }

    // --- Called by the MainMenu script or other controllers ---
    public void OpenOptionsMenuFromMainMenu()
    {
        OpenOptionsPanel();
    }
}