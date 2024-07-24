﻿using AXERP.API.Domain.Attributes;
using System.ComponentModel.DataAnnotations.Schema;

namespace AXERP.API.Domain.Entities
{
    [Table("Deliveries")]
    public class Delivery
    {
        [SqlModifier(SqlModifiers.StringNumeral)]
        public string ID { get; set; }

        public DateTime? DateLoadedEnd { get; set; }

        public DateTime? DateDelivered { get; set; }

        public string SalesContractID { get; set; }

        public string SalesStatus { get; set; }

        public int? TerminalID { get; set; }

        public double? QtyLoaded { get; set; }

        [SqlModifier(SqlModifiers.StringNumeral)]
        public string ToDeliveryID { get; set; }

        public string Status { get; set; }

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

        //public int? CustomerToDeliveryID { get; set; }

        public int? ReferenceID1 { get; set; }

        public int? ReferenceID2 { get; set; }

        public int? ReferenceID3 { get; set; }

        //public int? TruckCompanyToDeliveryID { get; set; }
    }
}
