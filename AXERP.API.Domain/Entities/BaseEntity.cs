using System.ComponentModel.DataAnnotations;

namespace AXERP.API.Domain.Entities
{
    public class BaseEntity<T>
    {
        [Key]
        public virtual T ID { get; set; }
    }
}
