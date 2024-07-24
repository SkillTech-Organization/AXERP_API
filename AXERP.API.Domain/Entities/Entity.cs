using System.ComponentModel.DataAnnotations.Schema;

namespace AXERP.API.Domain.Entities
{
    [Table("Entities")]
    public class Entity : BaseEntity<int>
    {
        public string Name { get; set; }
    }
}
