using System.ComponentModel.DataAnnotations.Schema;

namespace AXERP.API.Domain.Entities
{
    [Table("Entities")]
    public class Entity : BaseEntity<int>
    {
        public string Name { get; set; }

        public string FullEntityName { get; set; }

        public string Address { get; set; }

        public string NabisyID { get; set; }
    }
}
