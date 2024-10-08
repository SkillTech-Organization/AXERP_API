﻿using AXERP.API.Domain.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AXERP.API.Domain.Entities
{
    [Table("Transactions")]
    public class Transaction : BaseEntity<int>
    {
        [Key]
        public override int ID { get; set; }

        [Key]
        public string IDSffx { get; set; }

        public DateTime? DateLoadedEnd { get; set; }

        public DateTime? DateDelivered { get; set; }

        public string SalesContractID { get; set; }

        public string SalesStatusID { get; set; }

        public int? TerminalID { get; set; }

        public double? QtyLoaded { get; set; }

        public int? StockDays { get; set; }

        public int? SlotBookedByAXGTT { get; set; }

        [SqlModifier(SqlModifiers.StringNumeral)]
        public string ToDeliveryID { get; set; }

        public string StatusID { get; set; }

        public int? SpecificDeliveryPointID { get; set; }

        public int? DeliveryPointID { get; set; }

        public int? TransporterID { get; set; }

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

        public string? Reference { get; set; }

        public string? Reference2 { get; set; }

        public string? Reference3 { get; set; }

        public int? BlFileID { get; set; }

        public string AXERPHash { get; set; }
    }
}
