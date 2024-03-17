using Microsoft.AspNetCore.Mvc;

namespace MinimalVilla.Models.Dto
{
    public class CouponRequest
    {
        public string? CoupoName { get; set; }
        [FromHeader(Name = "PageSize")]
        public int PageSize { get; set; }
        [FromHeader(Name = "Page")]
        public int Page { get; set; }
        [FromServices]
        public required ILogger<CouponRequest> Logger { get; set; }
    }
}