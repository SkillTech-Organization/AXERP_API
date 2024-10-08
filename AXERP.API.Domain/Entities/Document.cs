using System.ComponentModel.DataAnnotations.Schema;

namespace AXERP.API.Domain.Entities
{
    [Table("Documents")]
    public class Document : BaseEntity<int>
    {
        public string Name { get; set; }

        public string FileName { get; set; }

        public DateTime? ProcessedAt { get; set; }
    }
}
