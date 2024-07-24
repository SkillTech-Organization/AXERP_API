using System.ComponentModel.DataAnnotations.Schema;

namespace AXERP.API.Domain.Entities
{
    [Table("Customers")]
    public class Customer
    {
        public int ID { get; set; }

        public string Name { get; set; }
    }
}
