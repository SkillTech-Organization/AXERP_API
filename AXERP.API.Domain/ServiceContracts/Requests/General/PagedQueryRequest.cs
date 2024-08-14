namespace AXERP.API.Domain.ServiceContracts.Requests
{
    public class PagedQueryRequest : BaseRequest
    {
        public virtual string QueryTemplate { get; set; }

        public virtual string CountTemplate { get; set; }

        public virtual string Search { get; set; }

        public virtual int Page { get; set; }

        public virtual int PageSize { get; set; }

        public virtual string OrderBy { get; set; }

        public virtual bool SearchOnlyInSelectedColumns { get; set; }

        public virtual bool OrderDesc { get; set; }

        public virtual List<string> Columns { get; set; }

        public virtual int RowNumberStart => PageSize * (Page - 1);

        public virtual int RowNumberFinish => RowNumberStart == 0 ? RowNumberStart + PageSize : RowNumberStart + PageSize - 1;
    }
}
