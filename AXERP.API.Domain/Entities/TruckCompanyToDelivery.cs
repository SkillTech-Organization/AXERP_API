namespace AXERP.API.Domain.Entities
{
    public class TruckCompanyToDelivery
    {
        public int ID { get; set; }

        public string? DeliveryID { get; set; }

        public int? TruckCompanyID { get; set; }

        public string Comment { get; set; }
    }
}
