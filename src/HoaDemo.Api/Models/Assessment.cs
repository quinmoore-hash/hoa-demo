using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HoaDemo.Api.Models
{
    /// <summary>A dues assessment levied against a homeowner.</summary>
    public class Assessment
    {
        public int Id { get; set; }

        public int HomeownerId { get; set; }
        public virtual Homeowner Homeowner { get; set; }

        [Required, StringLength(200)]
        public string Description { get; set; }

        public decimal Amount { get; set; }

        public DateTime DueDate { get; set; }

        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
