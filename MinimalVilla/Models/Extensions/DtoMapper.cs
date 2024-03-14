
using MinimalVilla.Models.Domain;
using MinimalVilla.Models.Dto;

namespace MinimalVilla.Models.Extensions
{
    public static class DtoMapper
    {
        // NB this is an "extension method" for model
        // the "this" keyword allows this to appear as a member method
        // of the model. It allows us to call it like myModel.ToDto
        // which looks much better than DomainExtension.ToDto(myModel).
        // aka it is syntactic sugar over the static method.
        public static CouponDto ToDto(this Coupon coupon)
        {
            return new CouponDto
            {
                Name = coupon.Name,
                IsActive = coupon.IsActive,
                Percent = coupon.Percent,
                Created = coupon.Created
            };
        }
    }
}