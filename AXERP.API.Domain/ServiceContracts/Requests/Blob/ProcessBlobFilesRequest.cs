namespace AXERP.API.Domain.ServiceContracts.Requests.Blob
{
    public class ProcessBlobFilesRequest
    {
        public string BlobStorageConnectionString { get; set; }

        public string BlobStorageName { get; set; }

        public string BlobStorageImportFolder { get; set; }

        public string BlobStorageProcessedFolder { get; set; }

        public string BlobStorePdfFileRegexPattern { get; set; }
    }
}
