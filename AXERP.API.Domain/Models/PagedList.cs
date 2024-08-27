using Newtonsoft.Json;

namespace AXERP.API.Domain.Models
{
    public class PagedList<T> : List<T>
    {
        public int CurrentPage { get; private set; }
        public int TotalPages { get; private set; }
        public int PageSize { get; private set; }
        public int TotalCount { get; private set; }
        public long SelectedIndex { get; private set; } = -1;

        public bool HasPrevious => CurrentPage > 0;
        public bool HasNext => CurrentPage < TotalPages - 1;

        public int FirstRowOnPage
        {

            get { return (CurrentPage) * PageSize + 1; }
        }

        public int LastRowOnPage
        {
            get { return Math.Min(CurrentPage * PageSize, TotalCount); }
        }
        public PagedList(List<T> items, int count, int pageNumber, int pageSize, long selectedIndex)
        {
            TotalCount = count;
            PageSize = pageSize;
            CurrentPage = pageNumber;
            SelectedIndex = selectedIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);

            AddRange(items);
        }

        public static PagedList<T> ToPagedList(IEnumerable<T> source, int pageNumber, int pageSize, long selectedIndex)
        {
            var count = source.Count();
            if (selectedIndex > 0)
            {
                pageNumber = (int)(selectedIndex / pageSize);
            }
            var items = source.Skip((pageNumber) * pageSize).Take(pageSize).ToList();
            return new PagedList<T>(items, count, pageNumber, pageSize, selectedIndex);
        }

        public string GetMetaData()
        {
            var metadata = new
            {
                this.TotalCount,
                this.PageSize,
                this.CurrentPage,
                this.TotalPages,
                this.FirstRowOnPage,
                this.LastRowOnPage,
                this.HasNext,
                this.HasPrevious,
                this.SelectedIndex
            };
            return JsonConvert.SerializeObject(metadata);
        }
    }

}
