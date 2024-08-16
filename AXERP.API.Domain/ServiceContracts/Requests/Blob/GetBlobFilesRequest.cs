namespace AXERP.API.BlobHelper.ServiceContracts.Requests
{
    public class GetBlobFilesRequest
    {
        public string? FolderName { get; set; }

        public string? RegexPattern { get; set; }
    }
}
