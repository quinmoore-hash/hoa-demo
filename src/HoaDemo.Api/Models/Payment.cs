using System;
using System.ComponentModel.DataAnnotations;

namespace HoaDemo.Api.Models
{
    public class Payment
    {
        public int Id { get; set; }

        public int AssessmentId { get; set; }
        public virtual Assessment Assessment { get; set; }

        public decimal Amount { get; set; }

        public DateTime PaidOn { get; set; }

        [StringLength(50)]
        public string Method { get; set; }   // Check, ACH, Card...

        [StringLength(50)]
        public string ReferenceNumber { get; set; }
    }
}
