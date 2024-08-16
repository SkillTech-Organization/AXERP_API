namespace AXERP.API.Domain.ServiceContracts.Requests
{
    public class PagedAppInsightsQueryRequest
    {
        public virtual int Page { get; set; }

        public virtual int PageSize { get; set; }

        public virtual string OrderBy { get; set; }

        public virtual bool OrderDesc { get; set; }

        public virtual DateTime From { get; set; }

        public virtual DateTime To { get; set; }

        public virtual int RowNumberStart => PageSize * (Page - 1);

        public virtual int RowNumberFinish => RowNumberStart == 0 ? RowNumberStart + PageSize : RowNumberStart + PageSize - 1;
    }
}
