using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Data.Models
{
    [Table("tbDoctors")]
    public class tbDoctors
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public required string Full_Name { get; set; }
        [Required]
        public required string Specialization { get; set; }
        [Required]
        public required string Email { get; set; }
        public string? Phone_Number { get; set; }
        public bool Is_Active { get; set; } = true;
        [Required]
        public required DateTimeOffset Created_At { get; set; }
        [Required]
        public required DateTimeOffset Updated_At { get; set; }

        public ICollection<tbUsers> Users { get; set; } = new List<tbUsers>();
        public ICollection<tbSlots> Slots { get; set; } = new List<tbSlots>();
        public ICollection<tbAppointments> Appointments { get; set; } = new List<tbAppointments>();
    }
}
