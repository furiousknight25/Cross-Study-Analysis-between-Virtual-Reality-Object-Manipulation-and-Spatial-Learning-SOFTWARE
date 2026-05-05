public interface IExperimentLogger<T> where T : struct
{
    void InitializeLog(string filePathBase);
    void LogData(T dataPayload);
    void FlushLog();
    void CloseLog();
}