using Data.Models;
using Infra.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infra.UnitOfWork
{
    public class BookingUnitOfWork : IBookingUnitOfWork
    {
        private readonly BookingDBContext _bookingContext;
        private IDbContextTransaction? _transaction;

        private IRepository<tbDoctors>? _doctors;
        private IRepository<tbPatients>? _patients;
        private IRepository<tbAppointments>? _appointments;
        private IRepository<tbAppointmentStatusHistory>? _appointmentStatusHistory;
        private IRepository<tbSlots>? _slots;
        private IRepository<tbUsers>? _users;

        public BookingUnitOfWork(BookingDBContext bookingContext)
        {
            _bookingContext = bookingContext;
        }

        public IRepository<tbDoctors> Doctors => _doctors ??= new Repository<tbDoctors>(_bookingContext);
        public IRepository<tbPatients> Patients => _patients ??= new Repository<tbPatients>(_bookingContext);
        public IRepository<tbAppointments> Appointments => _appointments ??= new Repository<tbAppointments>(_bookingContext);
        public IRepository<tbAppointmentStatusHistory> AppointmentStatusHistory => _appointmentStatusHistory ??= new Repository<tbAppointmentStatusHistory>(_bookingContext);
        public IRepository<tbSlots> Slots => _slots ??= new Repository<tbSlots>(_bookingContext);
        public IRepository<tbUsers> Users => _users ??= new Repository<tbUsers>(_bookingContext);


        // Transaction Management
        public async Task<int> SaveChangeAsync()
        {
            return await _bookingContext.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _bookingContext.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _bookingContext.Dispose();
        }
    }
}
