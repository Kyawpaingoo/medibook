using System;
using System.Collections.Generic;
using System.Text;

namespace Infra.UnitOfWork
{
    public interface IBookingUnitOfWork : IDisposable
    {
        // Transaction and SaveChanges
        Task<int> SaveChangeAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
