namespace AXERP.API.Domain.ServiceContracts.Requests
{
    public class ProcessBlobFilesRequest
    {
        public string BlobStorageImportFolder { get; set; }

        public string BlobStorageProcessedFolder { get; set; }

        public string BlobStorePdfFileRegexPattern { get; set; }
    }
}
