using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject OptionsPanel; // Renamed for clarity: `Options` -> `OptionsPanel`

    // Call this from your "Start Film" button's OnClick()
    public void StartNewGame()
    {
        Debug.Log("Starting new game...");
        // Implement logic to start a new game (e.g., clear save data or load initial scene)
        // SceneManager.LoadScene("PrologueBattleScene"); // Example scene load
    }

    // Call this from your "Load Film" button's OnClick()
    public void OpenLoadGamePanel()
    {
        Debug.Log("Loading save game panel...");
        // You would likely activate a separate GameObject that holds your load game UI
        // loadGamePanel.SetActive(true);
    }

    // Call this from your "Options" button's OnClick()
    public void OpenOptionsPanel()
    {
        Debug.Log("Opening options panel...");
        OptionsPanel.SetActive(true);
    }
    // Call this from your "Quit Film" button's OnClick()
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit(); // This only works in a built game
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // This works in the editor
#endif
    }
}