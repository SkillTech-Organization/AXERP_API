namespace AXERP.API.LogHelper
{
    public interface IAxerpLogger
    {
        public void BeginMeasure();

        public void EndMeasure();

        public void LogInformation(int id, string message, params object?[] args);

        public void LogInformation(string message, params object?[] args);

        public void LogDebug(int id, string message, params object?[] args);

        public void LogDebug(string message, params object?[] args);

        public void LogWarning(int id, string message, params object?[] args);

        public void LogWarning(string message, params object?[] args);

        public void LogError(int id, Exception ex, string message, params object?[] args);

        public void LogError(int id, string message, params object?[] args);

        public void LogError(int id, Exception ex);

        public void LogError(Exception ex, string message, params object?[] args);

        public void LogError(string message, params object?[] args);

        public void LogError(Exception ex);
    }
}
