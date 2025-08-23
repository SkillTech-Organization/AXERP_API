using AXERP.API.Domain.Entities;
using AXERP.API.Domain.Models;
using AXERP.API.Domain.Util;
using AXERP.API.GoogleHelper;
using AXERP.API.GoogleHelper.Managers;
using AxerpTransaction = AXERP.API.Domain.Entities.Transaction;

namespace AXERP.API.Business.Services;

public interface IBillOfLadingUpdater
{
    Task UpdateAsync(IEnumerable<AxerpTransaction> transactions, CancellationToken cancellationToken);
}

public sealed class BillOfLadingUpdater : IBillOfLadingUpdater
{
    private readonly GoogleSheetManagerFactory _sheetManagerFactory;

    public BillOfLadingUpdater(GoogleSheetManagerFactory googleSheetManagerFactory)
    {
        _sheetManagerFactory = googleSheetManagerFactory;
    }

    public async Task UpdateAsync(IEnumerable<AxerpTransaction> transactions, CancellationToken cancellationToken)
    {
        using GoogleSheetManager sheetManager = _sheetManagerFactory.Create();

        (int deliveryIdColumn, int billOfLadingColumn) = await GetDeliveryIdAndBillOfLadingColumnNumber(sheetManager);

        HashSet<CellData> deliveryIds = (await sheetManager.ReadColumnAsync(deliveryIdColumn))
            .ToHashSet(new CellData.CompareByValue());

        IEnumerable<CellData> targetCells = GetTargetCells(billOfLadingColumn, deliveryIds, transactions);

        await sheetManager.UpdateCellsAsync(targetCells);
    }

    private static async Task<(int DeliveryIdColumn, int BillOfLadingColumn)> GetDeliveryIdAndBillOfLadingColumnNumber(GoogleSheetManager sheetManager)
    {
        var header = (await sheetManager.ReadHeaderAsync()).ToArray();

        var fieldNames = SheetHelperMethods.GetFieldNamesWithOrder<Delivery>(header);

        int deliveryIdColumn = fieldNames[nameof(Delivery.DeliveryID)] + 1;
        int billOfLadingColumn = fieldNames[nameof(Delivery.BillOfLading)] + 1;

        return (deliveryIdColumn, billOfLadingColumn);
    }

    private static IEnumerable<CellData> GetTargetCells(int billOfLadingColumn, HashSet<CellData> deliveryIds, IEnumerable<AxerpTransaction> transactions)
    {
        List<CellData> targetCells = new(deliveryIds.Count);
        foreach (var transaction in transactions)
        {
            CellData search = new() { Value = transaction.ID + transaction.IDSffx };

            if (!deliveryIds.TryGetValue(search, out CellData? result))
            {
                // _logger.LogWarning("Transaction not found in delivery column: {0}", search.Value);
                continue;
            }

            string? value = transaction.BillOfLading is not null
                ? DateOnly.FromDateTime(transaction.BillOfLading.Value).ToString("dd/MM/yyyy")
                : string.Empty;

            targetCells.Add(new CellData
            {
                Column = billOfLadingColumn,
                Row = result.Row,
                Value = value,
            });
        }

        return targetCells;
    }
}
