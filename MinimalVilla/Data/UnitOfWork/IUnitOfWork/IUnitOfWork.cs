using Microsoft.EntityFrameworkCore.Storage;
using MinimalVilla.Data.Repository.IRepository;

namespace MinimalVilla.Data.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        ICouponRepository Coupons { get; init; }

        Task<int> Complete();

        IDbContextTransaction Transaction();
    }
}