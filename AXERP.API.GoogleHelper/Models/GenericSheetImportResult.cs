namespace AXERP.API.GoogleHelper.Models
{
    public class GenericSheetImportResult<T>
    {
        public List<T>? Data { get; set; }

        public int ImportedRowCount => Data?.Count ?? 0;

        public int TotalRowsInSheet { get; set; }

        public int InvalidRows { get; set; }

        public List<string> Errors { get; set; }
    }
}
