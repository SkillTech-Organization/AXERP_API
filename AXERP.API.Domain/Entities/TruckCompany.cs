using System.ComponentModel.DataAnnotations.Schema;

namespace AXERP.API.Domain.Entities
{
    [Table("TruckCompanies")]
    public class TruckCompany
    {
        public int ID { get; set; }

        public string Name { get; set; }
    }
}
