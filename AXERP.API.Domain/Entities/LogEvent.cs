using System.ComponentModel.DataAnnotations.Schema;

namespace AXERP.API.Domain.Entities
{
    [Table("LogEvents")]
    public class LogEvent : BaseEntity<int>
    {
        public string Message { get; set; }

        public string MessageTemplate { get; set; }

        public string Level { get; set; }

        public DateTime TimeStamp { get; set; }

        public string Exception { get; set; }

        public string Properties { get; set; }
    }
}
