using Data.Models;
using Infra.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infra.UnitOfWork
{
    public interface IBookingUnitOfWork : IDisposable
    {
        IRepository<tbDoctors> Doctors { get; }
        IRepository<tbPatients> Patients { get; }
        IRepository<tbAppointments> Appointments { get; }
        IRepository<tbAppointmentStatusHistory> AppointmentStatusHistory { get; }
        IRepository<tbSlots> Slots { get; }
        IRepository<tbUsers> Users { get; }
        // Transaction and SaveChanges
        Task<int> SaveChangeAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
