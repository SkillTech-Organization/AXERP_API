namespace AXERP.API.GoogleHelper.Models
{
    public class ReadGoogleSheetResult<T>
    {
        public List<T>? Data { get; set; }

        public int RowCount => Data?.Count ?? 0;

        public int InvalidRows { get; set; }

        public List<string> Errors { get; set; }
    }
}
