using Microsoft.EntityFrameworkCore.Storage;
using MinimalVilla.Data.Repository.IRepository;
using MinimalVilla.Data.Repository;

namespace MinimalVilla.Data.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _db;
        public ICouponRepository Coupons { get; init; }

        public UnitOfWork(
            ApplicationDbContext db
        )
        {
            _db = db;

            Coupons = new CouponRepository(_db);
        }

        public async Task<int> Complete()
        {
            return await _db.SaveChangesAsync();
        }

        public void Dispose()
        {
            _db.Dispose();
        }

        public IDbContextTransaction Transaction() => _db.Database.BeginTransaction();

    }
}