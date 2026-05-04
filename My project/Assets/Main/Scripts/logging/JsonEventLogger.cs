using System.IO;
using UnityEngine;

public class JsonEventLogger : IExperimentLogger<TrialResultData>
{
    private StreamWriter writer;

    public void InitializeLog(string filePathBase)
    {
        // Using .jsonl (JSON Lines) so we can append discrete JSON objects per line
        string path = $"{filePathBase}_events.jsonl"; 
        writer = new StreamWriter(path, true);
    }

    public void LogData(TrialResultData data)
    {
        if (writer != null)
        {
            // JsonUtility.ToJson avoids heap allocations for the object parameter 
            // because TrialResultData is passed via strict generic typing, bypassing object boxing.
            string json = JsonUtility.ToJson(data);
            writer.WriteLine(json);
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