using Data.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models
{
    [Table("tbAppointment_Status_History")]
    public class tbAppointmentStatusHistory
    {
        [Key]
        public long Id { get; set; }
        [Required]
        public required Guid Appointment_Id { get; set; }
        public AppointmentStatus? From_Status { get; set; }
        [Required]
        public required AppointmentStatus To_Status { get; set; }
        [Required]
        public required DateTime Changed_At { get; set; }
        public Guid? Changed_By_User_Id { get; set; }

        public tbAppointmentscs Appointment { get; set; } = null!;
        public tbUsers? ChangedByUser { get; set; }
    }
}
