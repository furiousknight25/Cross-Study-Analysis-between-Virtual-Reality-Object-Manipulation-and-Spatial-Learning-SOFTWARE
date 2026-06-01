using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

public class CsvTelemetryLogger : IExperimentLogger<LocomotionTelemetry>
{
    private StreamWriter writer;
    
    // Pre-allocate memory for ~10,000 frames (roughly 2 minutes of 90Hz tracking)
    // This stops Unity from freezing the game to resize the array mid-trial.
    private List<LocomotionTelemetry> telemetryBuffer = new List<LocomotionTelemetry>(10000); 

    public void InitializeLog(string filePathBase)
    {
        string path = $"{filePathBase}_telemetry.csv";
        writer = new StreamWriter(path, true); 
        writer.WriteLine("Timestamp,EventName,X,Y,Z");
    }

    public void LogData(LocomotionTelemetry data)
    {
        // Instantly adds the math to RAM. Zero string creation! Zero stutter!
        telemetryBuffer.Add(data);
    }

    public void FlushLog()
    {
        if (writer != null && telemetryBuffer.Count > 0)
        {
            // Build the giant text block all at once
            StringBuilder sb = new StringBuilder();
            foreach (var data in telemetryBuffer)
            {
                sb.AppendLine($"{data.timestamp:F3},{data.eventName},{data.position.x:F3},{data.position.y:F3},{data.position.z:F3}");
            }
            
            // Write it to disk and push it
            writer.Write(sb.ToString());
            writer.Flush(); 
            
            // Clear the RAM for the next trial
            telemetryBuffer.Clear();
        }
    }

    public void CloseLog()
    {
        FlushLog(); // Dump any leftover data before closing
        if (writer != null)
        {
            writer.Close();
            writer.Dispose();
            writer = null;
        }
    }
}