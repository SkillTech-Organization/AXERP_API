﻿namespace AXERP.API.Domain.ServiceContracts.Requests
{
    public class ProcessBlobFilesRequest : BaseRequest
    {
        public string BlobStorageConnectionString { get; set; }

        public string BlobStorageName { get; set; }

        public string BlobStorageImportFolder { get; set; }

        public string BlobStorageProcessedFolder { get; set; }

        public string BlobStorePdfFileRegexPattern { get; set; }
    }
}
