using UnityEngine;
using System;
using System.IO;

public class LoggingManager : MonoBehaviour
{
    public static LoggingManager Instance { get; private set; }

    [Header("Telemetry Settings")]
    public Transform vrHeadset;
    [Tooltip("How often to log headset position. Set to 0 to log EVERY frame (90Hz).")]
    public float telemetryLogRate = 0.0f; 

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

        telemetryLogger = new CsvTelemetryLogger();
        eventLogger = new JsonEventLogger();

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string basePath = Path.Combine(Application.persistentDataPath, $"experiment_{timestamp}");
        
        telemetryLogger.InitializeLog(basePath);
        eventLogger.InitializeLog(basePath);
        
        Debug.Log($"<color=green>Loggers initialized at: {basePath}</color>");
    }

    private void Update()
    {
        if (vrHeadset != null)
        {
            // If rate is 0, log every frame. Otherwise, wait for the timer.
            if (telemetryLogRate <= 0f || Time.time >= nextLogTime)
            {
                LocomotionTelemetry locData = new LocomotionTelemetry
                {
                    timestamp = Time.time,
                    eventName = "HMD_Tracking",
                    position = vrHeadset.position
                };
                
                telemetryLogger.LogData(locData);
                
                if (telemetryLogRate > 0f) nextLogTime = Time.time + telemetryLogRate;
            }
        }
    }

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

    public void SaveToDisk()
    {
        telemetryLogger?.FlushLog();
        eventLogger?.FlushLog();
        Debug.Log("<color=cyan>Massive RAM Buffer successfully flushed to disk.</color>");
    }

    private void OnApplicationQuit()
    {
        telemetryLogger?.CloseLog();
        eventLogger?.CloseLog();
    }
}