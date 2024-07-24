using System.ComponentModel.DataAnnotations.Schema;

namespace AXERP.API.Domain.Entities
{
    [Table("Entities")]
    public class Entity
    {
        public int ID { get; set; }

        public string Name { get; set; }
    }
}
