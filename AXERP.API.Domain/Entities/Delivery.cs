using AXERP.API.Domain.Attributes;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace AXERP.API.Domain.Entities
{
    [Table("Deliveries")]
    public class Delivery
    {
        [SqlModifier(SqlModifiers.StringNumeral)]
        [JsonProperty("Delivery ID")]
        [JsonRequired]
        public string DeliveryID { get; set; }

        [JsonProperty("Date loaded (end)")]
        public DateTime? DateLoadedEnd { get; set; }

        [JsonProperty("Date delivered")]
        public DateTime? DateDelivered { get; set; }

        [JsonProperty("Sales Contract ID")]
        public string SalesContractID { get; set; }

        [JsonProperty("Sales status")]
        public string SalesStatus { get; set; }

        [JsonProperty("Terminal")]
        public string Terminal { get; set; }

        [JsonProperty("Qty loaded")]
        public double? QtyLoaded { get; set; }

        [JsonProperty("Stock Days")]
        public int? StockDays { get; set; }

        [JsonProperty("Slot booked by AXGTT")]
        public int? SlotBookedByAXGTT { get; set; }

        [JsonProperty("To delivery ID")]
        public long? ToDeliveryID { get; set; }

        [JsonProperty("Status")]
        public string Status { get; set; }

        [JsonProperty("Specific Delivery Point")]
        public string SpecificDeliveryPoint { get; set; }

        [JsonProperty("Delivery Point")]
        public string DeliveryPoint { get; set; }

        [JsonProperty("Transporter")]
        public string Transporter { get; set; }

        [JsonProperty("Delivery U.P.")]
        public double? DeliveryUP { get; set; }

        [JsonProperty("Transport Charges")]
        public double? TransportCharges { get; set; }

        [JsonProperty("Unit service charge")]
        public double? UnitSlotCharge { get; set; }

        [JsonProperty("Service Charge")]
        public double? ServiceCharges { get; set; }

        [JsonProperty("Unit storage charge")]
        public double? UnitStorageCharge { get; set; }

        [JsonProperty("Storage charge")]
        public double? StorageCharge { get; set; }

        [JsonProperty("Other Charges")]
        public double? OtherCharges { get; set; }

        [JsonProperty("Sales")]
        public double? Sales { get; set; }

        [JsonProperty("CMR")]
        public DateTime? CMR { get; set; }

        [JsonProperty("Bio MWh")]
        public double? BioMWh { get; set; }

        [JsonProperty("Bill of Lading")]
        public DateTime? BillOfLading { get; set; }

        [JsonProperty("Bio addendum")]
        public string BioAddendum { get; set; }

        [JsonProperty("Comment")]
        public string Comment { get; set; }

        [JsonProperty("Customer note")]
        public string CustomerNote { get; set; }

        [JsonProperty("Customer")]
        public string Customer { get; set; }

        [JsonProperty("Reference")]
        public string Reference { get; set; }

        [JsonProperty("Reference 2")]
        public string Reference2 { get; set; }

        [JsonProperty("Reference 3")]
        public string Reference3 { get; set; }

        [JsonProperty("Truck Loading Customer Comment")]
        public string TruckLoadingCompanyComment { get; set; }

        [JsonProperty("Truck company")]
        public string TruckCompany { get; set; }
    }
}
