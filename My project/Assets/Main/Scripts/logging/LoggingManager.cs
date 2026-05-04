using UnityEngine;
using System;
using System.IO;

public class LoggingManager : MonoBehaviour
{
    public static LoggingManager Instance { get; private set; }

    [Header("Telemetry Settings")]
    public Transform vrHeadset;
    [Tooltip("How often to log headset position (in seconds). 0 = every frame.")]
    public float telemetryLogRate = 0.1f; 

    private CsvTelemetryLogger telemetryLogger;
    private JsonEventLogger eventLogger;
    private float nextLogTime = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Instantiate Loggers
        telemetryLogger = new CsvTelemetryLogger();
        eventLogger = new JsonEventLogger();

        // Generate a base path (e.g., "C:/.../experiment_20240501_133200")
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string basePath = Path.Combine(Application.persistentDataPath, $"experiment_{timestamp}");
        
        telemetryLogger.InitializeLog(basePath);
        eventLogger.InitializeLog(basePath);
        
        Debug.Log($"<color=green>Loggers initialized at: {basePath}</color>");
    }

    private void Update()
    {
        // Automatically handle high-frequency telemetry logging
        if (vrHeadset != null && Time.time >= nextLogTime)
        {
            LocomotionTelemetry locData = new LocomotionTelemetry
            {
                timestamp = Time.time,
                position = vrHeadset.position
            };
            
            telemetryLogger.LogData(locData);
            nextLogTime = Time.time + telemetryLogRate;
        }
    }

    // Public method for UI/Event systems to broadcast discrete events
    public void LogEvent(string trialId, string eventName, int slotIndex = -1, string foil = "")
    {
        TrialResultData eventData = new TrialResultData
        {
            timestamp = Time.time,
            trialId = trialId,
            eventName = eventName,
            selectedSlot = slotIndex,
            selectedFoil = foil
        };
        
        eventLogger.LogData(eventData);
    }

    private void OnApplicationQuit()
    {
        // Crucial: Safely close the file streams when the game shuts down
        telemetryLogger?.CloseLog();
        eventLogger?.CloseLog();
        Debug.Log("Experiment log streams closed safely.");
    }
    
    // Inside LoggingManager.cs
    public void LogTelemetry(string eventName, Vector3 position)
    {
        LocomotionTelemetry data = new LocomotionTelemetry
        {
            timestamp = Time.time,
            eventName = eventName,
            position = position
        };
        telemetryLogger.LogData(data);
    }
}