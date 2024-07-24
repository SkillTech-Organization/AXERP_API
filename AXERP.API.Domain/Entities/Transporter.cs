using System.ComponentModel.DataAnnotations.Schema;

namespace AXERP.API.Domain.Entities
{
    [Table("Transporters")]
    public class Transporter : BaseEntity<int>
    {
        public string Name { get; set; }
    }
}
