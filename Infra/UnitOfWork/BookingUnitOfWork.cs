using Data.Models;
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

        public BookingUnitOfWork(BookingDBContext bookingContext)
        {
            _bookingContext = bookingContext;
        }


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
