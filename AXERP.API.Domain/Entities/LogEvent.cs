using AXERP.API.Domain.Attributes;
using System.ComponentModel.DataAnnotations.Schema;

namespace AXERP.API.Domain.Entities
{
    [Table("LogEvents")]
    public class LogEvent : BaseEntity<int>
    {
        [GridProps(order: 1, maxWidth: 200)]
        public long ProcessId { get; set; }

        [GridProps(order: 2, minWidth:250)]
        public string System { get; set; }

        [GridProps(order: 3)]
        public string Function { get; set; }

        [GridProps(order: 4)]
        public string Who { get; set; }

        [GridProps(order: 5, maxWidth: 150)]
        public DateTime When { get; set; }

        [GridProps(order: 6, maxWidth: 100)]
        public string Result { get; set; }

        [GridProps(order: 7, minWidth: 1000)]
        public string Description { get; set; }
    }
}
