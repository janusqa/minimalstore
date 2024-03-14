using BlazorStore.DataAccess.Data;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinimalVilla.Data.CouponStore;
using MinimalVilla.Models.Domain;
using MinimalVilla.Models.Dto;
using MinimalVilla.Models.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/api/coupon", (ILogger<Program> _logger) =>
{
    _logger.Log(LogLevel.Information, "Getting all coupons");
    return Results.Ok(new ApiResponse { IsSuccess = true, Result = CouponStore.couponList.Select(c => c.ToDto()).ToList(), StatusCode = System.Net.HttpStatusCode.OK });
}).WithName("GetCoupons").Produces<ApiResponse>(200);

app.MapGet("/api/coupon/{id:int}", (int id) =>
{
    var coupon = CouponStore.couponList.FirstOrDefault(c => c.Id == id);
    return coupon is not null
        ? Results.Ok(new ApiResponse { IsSuccess = true, Result = coupon.ToDto(), StatusCode = System.Net.HttpStatusCode.OK })
        : Results.NotFound(new ApiResponse { IsSuccess = false, StatusCode = System.Net.HttpStatusCode.NotFound });
}).WithName("GetCoupon").Produces<ApiResponse>(200).Produces(404);

app.MapPost("/api/coupon", async (IValidator<CreateCouponDto> _validation, [FromBody] CreateCouponDto newCoupon) =>
{
    var validationResult = await _validation.ValidateAsync(newCoupon);

    if (!validationResult.IsValid)
    {
        return Results.BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = validationResult.Errors.Select(e => e.ToString()).ToList(), StatusCode = System.Net.HttpStatusCode.BadRequest });
    }

    if (CouponStore.couponList.FirstOrDefault(c => c.Name.Equals(newCoupon.Name, StringComparison.CurrentCultureIgnoreCase)) is not null)
    {
        return Results.BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = ["Coupon already exists"], StatusCode = System.Net.HttpStatusCode.BadRequest });
    }

    var coupon = new Coupon
    {
        Id = CouponStore.couponList.OrderByDescending(c => c.Id).FirstOrDefault()?.Id + 1 ?? 1,
        Name = newCoupon.Name,
        IsActive = newCoupon.IsActive,
        Percent = newCoupon.Percent,
        Created = DateTime.Now,
        LastUpdated = DateTime.Now
    };
    CouponStore.couponList.Add(coupon);

    return Results.CreatedAtRoute("GetCoupon", new { id = coupon.Id }, new ApiResponse { IsSuccess = true, Result = coupon.ToDto(), StatusCode = System.Net.HttpStatusCode.Created });
    // return Results.Created($"/api/coupon/{coupon.Id}", new ApiResponse { IsSuccess = true, Result = coupon.ToDto(), StatusCode = System.Net.HttpStatusCode.Created });
    //return Results.StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse { IsSuccess = false, ErrorMessage = "Internal Server Error" });

}).WithName("CreateCoupon").Accepts<CreateCouponDto>("application/json").Produces<ApiResponse>(201).Produces(400);

app.MapPut("/api/coupon", ([FromBody] Coupon coupon) =>
{
    // return Results.Ok(CouponStore.couponList.FirstOrDefault(c => c.Id == id));
});

app.MapDelete("/api/coupon/{id:int}", (int id) =>
{
    CouponStore.couponList = CouponStore.couponList.Where(c => c.Id != id).ToList();
    return Results.Ok();
});

app.UseHttpsRedirection();

app.Run();