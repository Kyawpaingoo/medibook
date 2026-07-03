using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Dtos
{
    public class DoctorDto
    {
        public Guid Id { get; set; }
        public string? Full_Name { get; set; }
        public string? Specialization { get; set; }
        public string? Email { get; set; }
        public string? Phone_Number { get; set; }
        public bool? Is_Active { get; set; }
        public DateTimeOffset? Created_At { get; set; }
    }

    public class CreateDoctorRequestDto
    {
        public required string Full_Name { get; set; }
        public required string Specialization { get; set; }
        public required string Email { get; set; }
        public string? Phone_Number { get; set; }
    }

    public class UpdateDoctorRequestDto
    {
        public Guid Id { get; set; }
        public required string Full_Name { get; set; }
        public required string Specialization { get; set; }
        public required string Email { get; set; }
        public string? Phone_Number { get; set; }
    }
}
