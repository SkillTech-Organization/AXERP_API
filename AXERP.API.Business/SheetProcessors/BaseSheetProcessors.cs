using AXERP.API.GoogleHelper.Models;

namespace AXERP.API.Business.SheetProcessors
{
    public abstract class BaseSheetProcessors<RowType>
    {
        abstract public GenericSheetImportResult<RowType> ProcessRows(IList<IList<object>> sheet_value_range, string culture_code);
    }
}
