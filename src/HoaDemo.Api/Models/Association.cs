using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HoaDemo.Api.Models
{
    public class Association
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Name { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        // Late-fee policy
        public int GracePeriodDays { get; set; } = 15;
        public decimal LateFeeFlatAmount { get; set; } = 25m;
        public decimal LateFeePercent { get; set; } = 0.05m;   // 5% of the unpaid balance
        public decimal LateFeeMax { get; set; } = 100m;       // 0 = no cap

        public virtual ICollection<Homeowner> Homeowners { get; set; } = new List<Homeowner>();
    }
}
