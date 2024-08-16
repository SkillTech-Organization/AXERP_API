namespace AXERP.API.Domain
{
    public static class EnvironmentHelper
    {
        public static string TryGetParameter(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new Exception($"Empty or null parameter key!");
            }

            var value = Environment.GetEnvironmentVariable(key);

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new Exception($"Missing parameter: {key}");
            }

            return value;
        }

        public static string? TryGetOptionalParameter(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new Exception($"Empty or null parameter key!");
            }

            var value = Environment.GetEnvironmentVariable(key);

            return value;
        }
    }
}
