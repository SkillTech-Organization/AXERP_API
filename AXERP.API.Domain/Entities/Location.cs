using System.ComponentModel.DataAnnotations.Schema;

namespace AXERP.API.Domain.Entities
{
    [Table("Locations")]
    public class Location
    {
        public int ID { get; set; }

        public string Name { get; set; }
    }
}
