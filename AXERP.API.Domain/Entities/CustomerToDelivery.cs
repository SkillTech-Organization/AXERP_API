namespace AXERP.API.Domain.Entities
{
    public class CustomerToDelivery
    {
        public int ID { get; set; }

        public string? DeliveryID { get; set; }

        public int? CustomerID { get; set; }

        public string Comment { get; set; }
    }
}
