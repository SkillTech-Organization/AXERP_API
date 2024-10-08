using AXERP.API.Domain.Attributes;
using System.Runtime.Serialization;

namespace AXERP.API.Domain.Models
{
    [DataContract]
    public class BlobFile
    {
        [DataMember]
        [GridProps(order: 1, minWidth: 1000)]
        public string FileName { get; set; }

        [DataMember]
        [GridProps(order: 2, minWidth: 120, maxWidth: 120)]
        public string Folder { get; set; }

        [IgnoreDataMember]
        public string Path
        {
            get
            {
                var fileName = FileName;
                var folderName = !string.IsNullOrWhiteSpace(Folder) ? $"{Folder}/" : "";
                var path = $"{folderName}{fileName}";

                return path;
            }
        }
    }
}
