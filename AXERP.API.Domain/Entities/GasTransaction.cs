using System.Text.Json.Serialization;

namespace AXERP.API.Domain.Entities
{
    public class GasTransaction
    {
        [JsonPropertyName("Delivery ID")]
        public long DeliveryID { get; set; }

        [JsonPropertyName("Date loaded (end)")]
        public DateTime? DateLoadedEnd { get; set; }

        [JsonPropertyName("Date delivered")]
        public DateTime? DateDelivered { get; set; }

        [JsonPropertyName("Sales Contract ID")]
        public string SalesContractID { get; set; }

        [JsonPropertyName("Sales status")]
        public string SalesStatus { get; set; }

        [JsonPropertyName("Terminal")]
        public string Terminal { get; set; }

        [JsonPropertyName("Qty loaded")]
        public double? QtyLoaded { get; set; }

        [JsonPropertyName("To delivery ID")]
        public long? ToDeliveryID { get; set; }

        [JsonPropertyName("Status")]
        public string Status { get; set; }

        [JsonPropertyName("Specific Delivery Point")]
        public string SpecificDeliveryPoint { get; set; }

        [JsonPropertyName("Delivery Point")]
        public string DeliveryPoint { get; set; }

        [JsonPropertyName("Transporter")]
        public string Transporter { get; set; }

        [JsonPropertyName("Delivery U.P.")]
        public double? DeliveryUP { get; set; }

        [JsonPropertyName("Transport Charges")]
        public double? TransportCharges { get; set; }

        [JsonPropertyName("Unit service charge")]
        public double? UnitSlotCharge { get; set; }

        [JsonPropertyName("Service Charge")]
        public double? ServiceCharges { get; set; }

        [JsonPropertyName("Unit storage charge")]
        public double? UnitStorageCharge { get; set; }

        [JsonPropertyName("Storage charge")]
        public double? StorageCharge { get; set; }

        [JsonPropertyName("Other Charges")]
        public double? OtherCharges { get; set; }

        [JsonPropertyName("Sales")]
        public double? Sales { get; set; }

        [JsonPropertyName("CMR")]
        public DateTime? CMR { get; set; }

        [JsonPropertyName("Bio MWh")]
        public double? BioMWh { get; set; }

        [JsonPropertyName("Bill of Lading")]
        public DateTime? BillOfLading { get; set; }

        [JsonPropertyName("Bio addendum")]
        public string BioAddendum { get; set; }

        [JsonPropertyName("Comment")]
        public string Comment { get; set; }

        [JsonPropertyName("Customer note")]
        public string CustomerNote { get; set; }

        [JsonPropertyName("id")]
        public string Customer { get; set; }

        [JsonPropertyName("id")]
        public string Reference { get; set; }

        [JsonPropertyName("Reference 2")]
        public string Reference2 { get; set; }

        [JsonPropertyName("Reference 3")]
        public string Reference3 { get; set; }

        [JsonPropertyName("Truck Loading Customer Comment")]
        public string TruckLoadingCompanyComment { get; set; }

        [JsonPropertyName("Truck company")]
        public string TruckCompany { get; set; }
    }
}
