using AXERP.API.GoogleHelper.Models;

namespace AXERP.API.Functions.SheetProcessors
{
    public abstract class BaseSheetProcessors<RowType>
    {
        abstract public GenericSheetImportResult<RowType> ProcessRows(IList<IList<object>> sheet_value_range, string culture_code);
    }
}
