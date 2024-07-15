namespace AXERP.API.Domain.Entities
{
    public class GasTransaction
    {
        public string DeliveryID { get; set; }

        public DateTime? DateLoadedEnd { get; set; }

        public DateTime? DateDelivered { get; set; }

        public string SalesContractID { get; set; }

        public string SalesStatus { get; set; }

        public string Terminal { get; set; }

        public double? QtyLoaded { get; set; }

        public long? ToDeliveryID { get; set; }

        public string Status { get; set; }

        public string SpecificDeliveryPoint { get; set; }

        public string DeliveryPoint { get; set; }

        public string Transporter { get; set; }

        public double? DeliveryUP { get; set; }

        public double? TransportCharges { get; set; }

        public double? UnitSlotCharge { get; set; }

        public double? ServiceCharges { get; set; }

        public double? UnitStorageCharge { get; set; }

        public double? StorageCharge { get; set; }

        public double? OtherCharges { get; set; }

        public double? Sales { get; set; }

        public DateTime? CMR { get; set; }

        public double? BioMWh { get; set; }

        public DateTime? BillOfLading { get; set; }

        public string BioAddendum { get; set; }

        public string Comment { get; set; }

        public string CustomerNote { get; set; }

        public string Customer { get; set; }

        public string Reference { get; set; }

        public string Reference2 { get; set; }

        public string Reference3 { get; set; }

        public string TruckLoadingCompanyComment { get; set; }

        public string TruckCompany { get; set; }
    }
}
