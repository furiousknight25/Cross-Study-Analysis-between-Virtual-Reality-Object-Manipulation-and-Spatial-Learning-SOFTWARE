using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// 1. Define a struct to hold a single row of "Tidy Data"
public struct ExperimentEvent
{
    public string Timestamp;
    public string EventName; 
    public float X;
    public float Y;
    public float Z;

    // Constructor to easily create an event and auto-grab the exact time it happened
    public ExperimentEvent(string eventName, Vector3 position)
    {
        // "yyyy-MM-dd HH:mm:ss.fff" provides standard formatting down to the millisecond
        this.Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"); 
        this.EventName = eventName;
        this.X = position.x;
        this.Y = position.y;
        this.Z = position.z;
    }
}

public static class ExperimentLogger
{
    // 2. The input is now a flat List of events, rather than a Dictionary
    public static void SaveToCSV(List<ExperimentEvent> eventLog, string baseFileName = "experiment_data")
    {
        string fileTimeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"{baseFileName}_{fileTimeStamp}.csv";
        string path = Path.Combine(Application.persistentDataPath, fileName);

        StringBuilder sb = new StringBuilder();

        // 3. Write the CSV Header row (Crucial for data science tools like Pandas or R)
        sb.AppendLine("Timestamp,EventName,X_Pos,Y_Pos,Z_Pos");

        // 4. Write each event as a single vertical row
        foreach (ExperimentEvent e in eventLog)
        {
            // Because X, Y, and Z have their own columns, we no longer need to 
            // wrap the vector in quotation marks. F3 gives precision to 3 decimal places.
            string line = $"{e.Timestamp},{e.EventName},{e.X:F3},{e.Y:F3},{e.Z:F3}";
            sb.AppendLine(line);
        }

        File.WriteAllText(path, sb.ToString());
        Debug.Log($"[ExperimentLogger] Tidy Data saved to: {path}");
    }
}