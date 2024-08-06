namespace AXERP.API.Persistence.Queries
{
    public static class TransactionQueries
    {
        public const string Sql_Insert_Delivery = @"
                INSERT INTO Transactions
                       (ID
                       ,DateLoadedEnd
                       ,DateDelivered
                       ,SalesContractID
                       ,SalesStatus
                       ,TerminalID
                       ,QtyLoaded
                       ,ToDeliveryID
                       ,Status
                       ,SpecificDeliveryPointID
                       ,DeliveryPointID
                       ,TransporterID
                       ,DeliveryUP
                       ,TransportCharges
                       ,UnitSlotCharge
                       ,ServiceCharges
                       ,UnitStorageCharge
                       ,StorageCharge
                       ,OtherCharges
                       ,Sales
                       ,CMR
                       ,BioMWh
                       ,BillOfLading
                       ,BioAddendum
                       ,Comment
                       ,ReferenceID1
                       ,ReferenceID2
                       ,ReferenceID3)
                 VALUES
                       (@ID
                       ,@DateLoadedEnd
                       ,@DateDelivered
                       ,@SalesContractID
                       ,@SalesStatus
                       ,@TerminalID
                       ,@QtyLoaded
                       ,@ToDeliveryID
                       ,@Status
                       ,@SpecificDeliveryPointID
                       ,@DeliveryPointID
                       ,@TransporterID
                       ,@DeliveryUP
                       ,@TransportCharges
                       ,@UnitSlotCharge
                       ,@ServiceCharges
                       ,@UnitStorageCharge
                       ,@StorageCharge
                       ,@OtherCharges
                       ,@Sales
                       ,@CMR
                       ,@BioMWh
                       ,@BillOfLading
                       ,@BioAddendum
                       ,@Comment
                       ,@ReferenceID1
                       ,@ReferenceID2
                       ,@ReferenceID3)
            ";

        public const string Sql_Select_Delivery_IDs = @"
                select ID from Deliveries
            ";

        public const string Sql_Query_Paged_GasTransactions =
            @"
            select X.* from 
                (select _table.*, ROW_NUMBER() OVER (/**orderby**/) AS RowNumber from Deliveries _table /**where**/)
            as X where RowNumber between @start and @finish
            ";

        public const string Sql_Query_Paged_GasTransactions_Dynamic_Columns =
            @"
            select /**select**/ from 
                (select _table.*, ROW_NUMBER() OVER (/**orderby**/) AS RowNumber from Deliveries _table /**where**/)
            as X where RowNumber between @start and @finish
            ";

        public const string Sql_Query_Count_GasTransactions = "SELECT COUNT(*) FROM Deliveries";
    }
}
