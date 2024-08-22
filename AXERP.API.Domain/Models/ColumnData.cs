namespace AXERP.API.Domain.Models
{
    public class ColumnData
    {
        public string Name { get; set; }

        public string Title { get; set; }

        public string Type { get; set; }

        public int? Order { get; set; }

        /// <summary>
        /// Column min width in pixels
        /// </summary>
        public int? MinWidth { get; set; }

        /// <summary>
        /// Column max width in pixels
        /// </summary>
        public int? MaxWidth { get; set; }
    }
}
