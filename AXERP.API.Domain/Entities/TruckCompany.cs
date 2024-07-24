using System.ComponentModel.DataAnnotations.Schema;

namespace AXERP.API.Domain.Entities
{
    [Table("TruckCompanies")]
    public class TruckCompany : BaseEntity<int>
    {
        public string Name { get; set; }
    }
}
