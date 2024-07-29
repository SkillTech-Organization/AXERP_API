using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AXERP.API.Domain.Entities
{
    [Table("Points")]
    public class Point
    {
        [Key]
        public string Name { get; set; }

        public string Description { get; set; }
    }
}
