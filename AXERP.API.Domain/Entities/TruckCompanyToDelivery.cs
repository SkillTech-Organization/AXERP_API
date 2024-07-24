using System.ComponentModel.DataAnnotations.Schema;

namespace AXERP.API.Domain.Entities
{
    [Table("TruckCompanyToDelivery")]
    public class TruckCompanyToDelivery : BaseEntity<int>
    {
        public string? DeliveryID { get; set; }

        public int? TruckCompanyID { get; set; }

        public string Comment { get; set; }
    }
}
