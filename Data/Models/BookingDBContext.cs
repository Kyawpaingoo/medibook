using Microsoft.EntityFrameworkCore;
using System;

namespace Data.Models
{
    public class BookingDBContext : DbContext
    {
        public BookingDBContext(DbContextOptions<BookingDBContext> options) : base(options)
        {

        }
        public virtual DbSet<tbAppointments> tbAppointments { get; set; }
        public virtual DbSet<tbAppointmentStatusHistory> tbAppointmentStatusHistory { get; set; }
        public virtual DbSet<tbPatients> tbPatients { get; set; }
        public virtual DbSet<tbSlots> tbSlots { get; set; }
        public virtual DbSet<tbDoctors> tbDoctors { get; set; }
        public virtual DbSet<tbUsers> tbUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---------- Users ----------
            modelBuilder.Entity<tbUsers>(entity =>
            {
                entity.Property(u => u.Id)
                    .HasDefaultValueSql("gen_random_uuid()")
                    .ValueGeneratedOnAdd();

                entity.Property(u => u.Role)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.HasIndex(u => u.Email).IsUnique();

                entity.HasOne(u => u.Doctor)
                    .WithMany(d => d.Users)
                    .HasForeignKey(u => u.Doctor_Id)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(t => t.HasCheckConstraint("CK_Users_Role", "\"Role\" IN ('Admin','Doctor','Staff')"));
            });

            // ---------- Doctors ----------
            modelBuilder.Entity<tbDoctors>(entity =>
            {
                entity.HasIndex(d => d.Email).IsUnique();
            });

            // ---------- Patients ----------
            modelBuilder.Entity<tbPatients>(entity =>
            {
                entity.HasIndex(p => p.Email).IsUnique();
            });

            // ---------- Slots ----------
            modelBuilder.Entity<tbSlots>(entity =>
            {
                entity.Property(s => s.Status)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.HasOne(s => s.Doctor)
                    .WithMany(d => d.Slots)
                    .HasForeignKey(s => s.Doctor_Id)
                    .OnDelete(DeleteBehavior.Restrict);

                // Mirrors uq_doctor_slot_time: a doctor cannot have two slots starting at the same time.
                entity.HasIndex(s => new { s.Doctor_Id, s.Start_Time }).IsUnique();

                // Supports the hottest read path: "available slots for doctor X in a time range".
                entity.HasIndex(s => new { s.Doctor_Id, s.Status, s.Start_Time });

                entity.ToTable(t => t.HasCheckConstraint("CK_Slots_Status", 
                    "\"Status\" IN ('Available','Reserved','Confirmed','Cancelled')"));
                
                entity.Property<uint>("xmin")
                    .HasColumnType("xid")
                    .ValueGeneratedOnAddOrUpdate()
                    .IsConcurrencyToken();
            });

            // ---------- Appointments ----------
            modelBuilder.Entity<tbAppointments>(entity =>
            {
                entity.Property(a => a.Status)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.HasOne(a => a.Slot)
                    .WithMany(s => s.Appointments)
                    .HasForeignKey(a => a.Slot_Id)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Doctor)
                    .WithMany(d => d.Appointments)
                    .HasForeignKey(a => a.Doctor_Id)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Patient)
                    .WithMany(p => p.Appointments)
                    .HasForeignKey(a => a.Patient_Id)
                    .OnDelete(DeleteBehavior.Restrict);

                // Core double-booking guard: only one active (Reserved/Confirmed) appointment per slot.
                entity.HasIndex(a => a.Slot_Id)
                    .IsUnique()
                    .HasFilter("\"Status\" IN ('Reserved','Confirmed')")
                    .HasDatabaseName("uq_active_appointment_per_slot");

                entity.ToTable(t => t.HasCheckConstraint("CK_Appointments_Status", 
                    "\"Status\" IN ('Reserved','Confirmed','Cancelled')"));
                
                entity.Property<uint>("xmin")
                    .HasColumnType("xid")
                    .ValueGeneratedOnAddOrUpdate()
                    .IsConcurrencyToken();
            });

            // ---------- Appointment status history ----------
            modelBuilder.Entity<tbAppointmentStatusHistory>(entity =>
            {
                entity.Property(h => h.Id).ValueGeneratedOnAdd();

                entity.Property(h => h.From_Status).HasConversion<string>().HasMaxLength(20);
                entity.Property(h => h.To_Status).HasConversion<string>().HasMaxLength(20);

                entity.HasOne(h => h.Appointment)
                    .WithMany(a => a.StatusHistory)
                    .HasForeignKey(h => h.Appointment_Id)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(h => h.ChangedByUser)
                    .WithMany()
                    .HasForeignKey(h => h.Changed_By_User_Id)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
