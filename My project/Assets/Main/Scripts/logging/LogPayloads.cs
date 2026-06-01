using UnityEngine;

// Telemetry struct (pure data, no reference types to prevent GC allocation)
public struct LocomotionTelemetry
{
    public float timestamp;
    public string eventName; 
    public Vector3 position;
}

// Event struct (Marked Serializable so JsonUtility can convert it to text)
[System.Serializable]
public struct TrialResultData
{
    public float timestamp;
    public string trialId;
    public string eventName;
    public int selectedSlot;
    public string selectedFoil;
}