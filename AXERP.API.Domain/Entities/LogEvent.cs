using System.ComponentModel.DataAnnotations.Schema;

namespace AXERP.API.Domain.Entities
{
    [Table("LogEvents")]
    public class LogEvent : BaseEntity<int>
    {
        public long ProcessId { get; set; }

        public string System { get; set; }

        public string Function { get; set; }

        public string Who { get; set; }

        public DateTime When { get; set; }

        public string Description { get; set; }

        public string Result { get; set; }
    }
}
