using System.IO;
using UnityEngine;

public class JsonEventLogger : IExperimentLogger<TrialResultData>
{
    private StreamWriter writer;

    public void InitializeLog(string filePathBase)
    {
        string path = $"{filePathBase}_events.jsonl"; 
        writer = new StreamWriter(path, true);
    }

    public void LogData(TrialResultData data)
    {
        if (writer != null)
        {
            string json = JsonUtility.ToJson(data);
            writer.WriteLine(json);
            
            // JSON events are rare (clicks, placement), so we can safely flush them instantly
            writer.Flush(); 
        }
    }

    public void FlushLog()
    {
        if (writer != null) writer.Flush();
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