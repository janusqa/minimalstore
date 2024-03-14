using FluentValidation;
using MinimalVilla.Models.Dto;

namespace MinimalVilla.Validation
{
    public class CreateCouponValidation : AbstractValidator<CreateCouponDto>
    {
        public CreateCouponValidation()
        {
            RuleFor(model => model.Name).NotEmpty();
            RuleFor(model => model.Percent).InclusiveBetween(1, 100);
        }
    }
}