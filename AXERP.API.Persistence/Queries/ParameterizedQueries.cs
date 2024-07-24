namespace AXERP.API.Persistence.Queries
{
    public static class ParameterizedQueries
    {
        public static string Count => "SELECT COUNT(*) FROM {0} (NOLOCK)";

        public static string GetAll => "SELECT * FROM {0} (NOLOCK)";

        public static string DeleteAll => "DELETE FROM {0}";

        public static string GetALLIDs => "SELECT ID FROM {0}";
    }
}
