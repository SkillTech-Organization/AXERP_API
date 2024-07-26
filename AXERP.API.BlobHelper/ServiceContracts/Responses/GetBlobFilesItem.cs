using Azure.Storage.Blobs.Models;
using System.Text.RegularExpressions;

namespace AXERP.API.BlobHelper.ServiceContracts.Responses
{
    public class GetBlobFilesItem
    {
        public BlobHierarchyItem BlobItem { get; set; }

        public string RegexExpression { get; set; }

        public List<Match> Matches { get; set; }
    }
}
