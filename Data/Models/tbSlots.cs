using Data.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Data.Models
{
    [Table("tbSlots")]
    public class tbSlots
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public required Guid Doctor_Id { get; set; }
        [Required]
        public required DateTime Start_Time { get; set; }
        [Required]
        public required DateTime End_Time { get; set; }
        [Required]
        public required SlotStatus Status { get; set; }
        [Required]
        public required DateTimeOffset Created_At { get; set; }
        [Required]
        public required DateTimeOffset Updated_At { get; set; }

        public tbDoctors Doctor { get; set; } = null!;
        // Not one-to-one: only one Reserved/Confirmed appointment is allowed per slot
        // (enforced by a partial unique index), but cancelled/rebooked history can add more rows.
        public ICollection<tbAppointments> Appointments { get; set; } = new List<tbAppointments>();
    }
}
