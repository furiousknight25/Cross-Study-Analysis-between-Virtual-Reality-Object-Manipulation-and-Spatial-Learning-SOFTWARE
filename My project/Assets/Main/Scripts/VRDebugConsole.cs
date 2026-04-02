using UnityEngine;
using TMPro;

public class VRDebugConsole : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI logText;

    [Header("Settings")]
    public int maxCharacters = 2500; // Prevents massive lag from giant logs

    private string currentLogs = "";

    void Awake()
    {
        // This is the Unity equivalent of an Autoload. 
        // It prevents the console from being destroyed when you load a new scene.
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        // Subscribe to Unity's internal logging event
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Add color based on the type of log
        string colorHex = "#FFFFFF"; // Default white
        if (type == LogType.Error || type == LogType.Exception) colorHex = "#FF4444"; // Red
        else if (type == LogType.Warning) colorHex = "#FFCC00"; // Yellow

        currentLogs += $"<color={colorHex}>{logString}</color>\n";

        // Trim the string if it gets too long so we don't crash the UI
        if (currentLogs.Length > maxCharacters)
        {
            currentLogs = currentLogs.Substring(currentLogs.Length - maxCharacters);
        }

        if (logText != null)
        {
            logText.text = currentLogs;
        }
    }
}