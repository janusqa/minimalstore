
using MinimalVilla.Data.Repository.IRepository;
using MinimalVilla.Models.Domain;

namespace MinimalVilla.Data.Repository
{
    public class CouponRepository : Repository<Coupon>, ICouponRepository
    {
        public CouponRepository(ApplicationDbContext db) : base(db)
        {

        }
    }
}