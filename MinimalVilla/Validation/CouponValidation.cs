using FluentValidation;
using MinimalVilla.Models.Dto;

namespace MinimalVilla.Validation
{
    public class CouponValidation : AbstractValidator<CouponDto>
    {
        public CouponValidation()
        {
            RuleFor(model => model.Id).NotEmpty().GreaterThan(0);
            RuleFor(model => model.Name).NotEmpty();
            RuleFor(model => model.Percent).InclusiveBetween(1, 100);
        }
    }
}