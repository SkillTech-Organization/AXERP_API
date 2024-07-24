using System.ComponentModel.DataAnnotations.Schema;

namespace AXERP.API.Domain.Entities
{
    [Table("Customers")]
    public class Customer : BaseEntity<int>
    {
        public string Name { get; set; }
    }
}
