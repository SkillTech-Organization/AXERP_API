using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AXERP.API.Domain.Entities
{
    [Table("Interfaces")]
    public class Interface: BaseEntity<int>
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Code { get; set; }

        public string CounterParty { get; set; }

        public DateTime? LastCertificationCheck { get; set; }

        public string Point { get; set; }
    }
}
