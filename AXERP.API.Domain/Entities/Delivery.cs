using AXERP.API.Domain.Attributes;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace AXERP.API.Domain.Entities
{
    [Table("Deliveries")]
    public class Delivery
    {
        [JsonProperty("Delivery ID")]
        [JsonRequired]
        [GridProps(maxWidth: 150)]
        public int DeliveryID { get; set; }

        [JsonProperty("Delivery ID Suffix")]
        [JsonRequired]
        [GridProps(maxWidth: 200)]
        public string DeliveryIDSffx { get; set; }

        [JsonProperty("Date loaded (end)")]
        [GridProps(maxWidth: 180)]
        public DateTime? DateLoadedEnd { get; set; }

        [JsonProperty("Date delivered")]
        [GridProps(maxWidth: 150)]
        public DateTime? DateDelivered { get; set; }

        [JsonProperty("Sales Contract ID")]
        [GridProps(maxWidth: 200)]
        public string SalesContractID { get; set; }

        [JsonProperty("Sales status")]
        [GridProps(maxWidth: 150)]
        public string SalesStatus { get; set; }

        [JsonProperty("Terminal")]
        [GridProps(maxWidth: 200)]
        public string Terminal { get; set; }

        [JsonProperty("Qty loaded")]
        [GridProps(maxWidth: 300)]
        public double? QtyLoaded { get; set; }

        [JsonProperty("Stock Days")]
        [GridProps(maxWidth: 150)]
        public int? StockDays { get; set; }

        [JsonProperty("Slot booked by AXGTT")]
        [GridProps(maxWidth: 300)]
        public int? SlotBookedByAXGTT { get; set; }

        [JsonProperty("To delivery ID")]
        [GridProps(maxWidth: 300)]
        public string ToDeliveryID { get; set; }

        [JsonProperty("Status")]
        [GridProps(maxWidth: 100)]
        public string Status { get; set; }

        [JsonProperty("Specific Delivery Point")]
        [GridProps(maxWidth: 300)]
        public string SpecificDeliveryPoint { get; set; }

        [JsonProperty("Delivery Point")]
        [GridProps(maxWidth: 300)]
        public string DeliveryPoint { get; set; }

        [JsonProperty("Transporter")]
        [GridProps(maxWidth: 300)]
        public string Transporter { get; set; }

        [JsonProperty("Delivery U.P.")]
        [GridProps(maxWidth: 300)]
        public double? DeliveryUP { get; set; }

        [JsonProperty("Transport Charges")]
        [GridProps(maxWidth: 300)]
        public double? TransportCharges { get; set; }

        [JsonProperty("Unit service charge")]
        [GridProps(maxWidth: 300)]
        public double? UnitSlotCharge { get; set; }

        [JsonProperty("Service Charge")]
        [GridProps(maxWidth: 300)]
        public double? ServiceCharges { get; set; }

        [JsonProperty("Unit storage charge")]
        [GridProps(maxWidth: 300)]
        public double? UnitStorageCharge { get; set; }

        [JsonProperty("Storage charge")]
        [GridProps(maxWidth: 300)]
        public double? StorageCharge { get; set; }

        [JsonProperty("Other Charges")]
        [GridProps(maxWidth: 300)]
        public double? OtherCharges { get; set; }

        [JsonProperty("Sales")]
        [GridProps(maxWidth: 300)]
        public double? Sales { get; set; }

        [JsonProperty("CMR")]
        [GridProps(maxWidth: 150)]
        public DateTime? CMR { get; set; }

        [JsonProperty("Bio MWh")]
        [GridProps(maxWidth: 300)]
        public double? BioMWh { get; set; }

        [JsonProperty("Bill of Lading")]
        [GridProps(maxWidth: 150)]
        public DateTime? BillOfLading { get; set; }

        [JsonProperty("Bio addendum")]
        [GridProps(maxWidth: 300)]
        public string BioAddendum { get; set; }

        [JsonProperty("Comment")]
        public string Comment { get; set; }

        [JsonProperty("Customer note")]
        public string CustomerNote { get; set; }

        [JsonProperty("Customer")]
        [GridProps(maxWidth: 300)]
        public string Customer { get; set; }

        [JsonProperty("Reference")]
        [GridProps(maxWidth: 500)]
        public string Reference { get; set; }

        [JsonProperty("Reference 2")]
        [GridProps(maxWidth: 500)]
        public string Reference2 { get; set; }

        [JsonProperty("Reference 3")]
        [GridProps(maxWidth: 500)]
        public string Reference3 { get; set; }

        [JsonProperty("BL Filename")]
        public string BLFilename { get; set; }

        [JsonProperty("Truck Loading Customer Comment")]
        public string TruckLoadingCompanyComment { get; set; }

        [JsonProperty("Truck company")]
        [GridProps(maxWidth: 300)]
        public string TruckCompany { get; set; }

        [JsonProperty("AXERP Hash")]
        [GridProps(maxWidth: 300)]
        public string AXERPHash { get; set; }
    }
}
