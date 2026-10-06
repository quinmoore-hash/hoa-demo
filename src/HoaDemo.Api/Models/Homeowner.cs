using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HoaDemo.Api.Models
{
    public class Homeowner
    {
        public int Id { get; set; }

        public int AssociationId { get; set; }
        public virtual Association Association { get; set; }

        [Required, StringLength(100)]
        public string FirstName { get; set; }

        [Required, StringLength(100)]
        public string LastName { get; set; }

        [EmailAddress, StringLength(254)]
        public string Email { get; set; }

        [Required, StringLength(200)]
        public string PropertyAddress { get; set; }

        public virtual ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
    }
}
