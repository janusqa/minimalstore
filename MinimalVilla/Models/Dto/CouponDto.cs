namespace MinimalVilla.Models.Dto
{
    public class CouponDto
    {
        public required string Name { get; set; }
        public int Percent { get; set; }
        public bool IsActive { get; set; }
        public DateTime? Created { get; set; }

    }

    public class CreateCouponDto
    {
        public required string Name { get; set; }
        public int Percent { get; set; }
        public bool IsActive { get; set; }
    }
}