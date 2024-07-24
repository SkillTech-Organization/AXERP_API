using System.ComponentModel.DataAnnotations.Schema;

namespace AXERP.API.Domain.Entities
{
    [Table("Locations")]
    public class Location : BaseEntity<int>
    {
        public string Name { get; set; }
    }
}
