using UnityEngine;

// Telemetry struct (pure data, no reference types)
// In LogPayloads.cs
public struct LocomotionTelemetry
{
    public float timestamp;
    public string eventName; // Added to match your old ExperimentEvent
    public Vector3 position;
}
// Event struct (Marked Serializable so JsonUtility can read it)
[System.Serializable]
public struct TrialResultData
{
    public float timestamp;
    public string trialId;
    public string eventName;
    public int selectedSlot;
    public string selectedFoil;
}