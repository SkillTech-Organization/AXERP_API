using System.ComponentModel.DataAnnotations.Schema;

namespace AXERP.API.Domain.Entities
{
    [Table("CustomerToDelivery")]
    public class CustomerToDelivery
    {
        public int ID { get; set; }

        public string? DeliveryID { get; set; }

        public int? CustomerID { get; set; }

        public string Comment { get; set; }
    }
}
