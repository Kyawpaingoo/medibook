using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Data.Models
{
    [Table("tbPatients")]
    public class tbPatients
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public required string Full_Name { get; set; }
        [Required]
        public required string Email { get; set; }
        public string? Phone_Number { get; set; }
        public DateTime? Date_Of_Birth { get; set; }
        [Required]
        public required DateTimeOffset Created_At { get; set; }
        [Required]
        public required DateTimeOffset Updated_At { get; set; }

        public ICollection<tbAppointments> Appointments { get; set; } = new List<tbAppointments>();
    }
}
