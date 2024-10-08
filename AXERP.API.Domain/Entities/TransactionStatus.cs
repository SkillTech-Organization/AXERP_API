using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AXERP.API.Domain.Entities
{
    [Table("TransactionStatuses")]
    public class TransactionStatus
    {
        [Key]
        public string Name { get; set; }

        public string Description { get; set; }
    }
}
