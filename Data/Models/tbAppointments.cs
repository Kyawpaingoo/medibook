using Data.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Data.Models
{
    [Table("tbAppointments")]
    public class tbAppointments
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public required Guid Slot_Id { get; set; }
        [Required]
        public required Guid Doctor_Id { get; set; }
        [Required]
        public required Guid Patient_Id { get; set; }
        [Required]
        public required AppointmentStatus Status { get; set; }
        [Required]
        public required DateTimeOffset Created_At { get; set; }
        [Required]
        public required DateTimeOffset Updated_At { get; set; }

        public tbSlots Slot { get; set; } = null!;
        public tbDoctors Doctor { get; set; } = null!;
        public tbPatients Patient { get; set; } = null!;
        public ICollection<tbAppointmentStatusHistory> StatusHistory { get; set; } = new List<tbAppointmentStatusHistory>();
    }
}
