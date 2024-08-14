using AXERP.API.Business.Base;
using AXERP.API.GoogleHelper.Models;
using AXERP.API.LogHelper.Factories;

namespace AXERP.API.Business.SheetProcessors
{
    public abstract class BaseSheetProcessors<RowType, MainClass> : BaseAuditedClass<MainClass> where MainClass : class
    {
        public BaseSheetProcessors(AxerpLoggerFactory axerpLoggerFactory) : base(axerpLoggerFactory) { }

        abstract public GenericSheetImportResult<RowType> ProcessRows(IList<IList<object>> sheet_value_range, string culture_code);
    }
}
