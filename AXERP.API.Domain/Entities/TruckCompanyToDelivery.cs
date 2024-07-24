using System.ComponentModel.DataAnnotations.Schema;

namespace AXERP.API.Domain.Entities
{
    [Table("TruckCompanyToDelivery")]
    public class TruckCompanyToDelivery
    {
        public int ID { get; set; }

        public string? DeliveryID { get; set; }

        public int? TruckCompanyID { get; set; }

        public string Comment { get; set; }
    }
}
