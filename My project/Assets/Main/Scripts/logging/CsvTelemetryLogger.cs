using System.IO;
using UnityEngine;

public class CsvTelemetryLogger : IExperimentLogger<LocomotionTelemetry>
{
    private StreamWriter writer;

    public void InitializeLog(string filePathBase)
    {
        string path = $"{filePathBase}_telemetry.csv";
        
        // 'true' enables append mode, keeping the stream open
        writer = new StreamWriter(path, true); 
        writer.WriteLine("Timestamp,X,Y,Z");
    }

    public void LogData(LocomotionTelemetry data)
    {
        if (writer != null)
        {
            // Write directly to the stream. 
            // Note: ToString formatting allocates tiny strings, but the struct itself avoids boxing.
            writer.WriteLine($"{data.timestamp:F3},{data.position.x:F3},{data.position.y:F3},{data.position.z:F3}");
        }
    }

    public void CloseLog()
    {
        if (writer != null)
        {
            writer.Flush();
            writer.Close();
            writer.Dispose();
            writer = null;
        }
    }
}