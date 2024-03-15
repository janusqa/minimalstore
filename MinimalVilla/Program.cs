using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MinimalVilla.Data;
using MinimalVilla.Data.UnitOfWork;
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
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/api/coupon", async (ILogger<Program> _logger, IUnitOfWork _uow) =>
{
    _logger.Log(LogLevel.Information, "Getting all coupons");
    var coupons = (await _uow.Coupons.FromSqlAsync($@"SELECT * FROM Coupons;", [])).Select(c => c.ToDto()).ToList();
    return Results.Ok(new ApiResponse { IsSuccess = true, Result = coupons, StatusCode = System.Net.HttpStatusCode.OK });
}).WithName("GetCoupons").Produces<ApiResponse>(200);

app.MapGet("/api/coupon/{id:int}", async (IUnitOfWork _uow, int id) =>
{
    var coupon = (await _uow.Coupons.FromSqlAsync($@"SELECT * FROM Coupons WHERE Id = @Id", [new SqliteParameter("Id", id)])).FirstOrDefault()?.ToDto();
    return coupon is not null
        ? Results.Ok(new ApiResponse { IsSuccess = true, Result = coupon, StatusCode = System.Net.HttpStatusCode.OK })
        : Results.NotFound(new ApiResponse { IsSuccess = false, StatusCode = System.Net.HttpStatusCode.NotFound });
}).WithName("GetCoupon").Produces<ApiResponse>(200).Produces(404);

app.MapPost("/api/coupon", async (IUnitOfWork _uow, IValidator<CouponCreateDto> _validation, [FromBody] CouponCreateDto newCoupon) =>
{
    var validationResult = await _validation.ValidateAsync(newCoupon);

    if (!validationResult.IsValid)
    {
        return Results.BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = validationResult.Errors.Select(e => e.ToString()).ToList(), StatusCode = System.Net.HttpStatusCode.BadRequest });
    }

    var exists = (await _uow.Coupons.SqlQueryAsync<int>(@"
        SELECT COUNT(Id) FROM Coupons WHERE Name = @Name;
    ", [new SqliteParameter("Name", newCoupon.Name)])).FirstOrDefault();

    if (exists > 0)
    {
        return Results.BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = ["Coupon already exists"], StatusCode = System.Net.HttpStatusCode.BadRequest });
    }

    var coupon = (await _uow.Coupons.SqlQueryAsync<Coupon>($@"
        INSERT INTO Coupons
            (Name, IsActive, Percent, Created, LastUpdated) 
        VALUES
            (@Name, @IsActive, @Percent, @Created, @LastUpdated)
        RETURNING *;
    ", [
        new SqliteParameter("Name", newCoupon.Name),
        new SqliteParameter("IsActive", newCoupon.IsActive),
        new SqliteParameter("Percent", newCoupon.Percent),
        new SqliteParameter("Created", DateTime.UtcNow),
        new SqliteParameter("LastUpdated", DateTime.UtcNow)
    ])).FirstOrDefault()?.ToDto();

    if (coupon is not null)
    {
        return Results.CreatedAtRoute("GetCoupon", new { id = coupon.Id }, new ApiResponse { IsSuccess = true, Result = coupon, StatusCode = System.Net.HttpStatusCode.Created });
    }
    else
    {
        return Results.BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = ["Unable to create coupon"], StatusCode = System.Net.HttpStatusCode.BadRequest });
    }
    // return Results.Created($"/api/coupon/{coupon.Id}", new ApiResponse { IsSuccess = true, Result = coupon.ToDto(), StatusCode = System.Net.HttpStatusCode.Created });
    //return Results.StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse { IsSuccess = false, ErrorMessage = "Internal Server Error" });

}).WithName("CreateCoupon").Accepts<CouponCreateDto>("application/json").Produces<ApiResponse>(201).Produces(400);

app.MapPut("/api/coupon", async (IUnitOfWork _uow, IValidator<CouponDto> _validation, [FromBody] CouponDto coupon) =>
{

    var validationResult = await _validation.ValidateAsync(coupon);

    if (!validationResult.IsValid)
    {
        return Results.BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = validationResult.Errors.Select(e => e.ToString()).ToList(), StatusCode = System.Net.HttpStatusCode.BadRequest });
    }

    var exists = (await _uow.Coupons.SqlQueryAsync<int>(@"
        SELECT COUNT(Id) FROM Coupons WHERE LOWER(Name) = LOWER(@Name) AND Id != @Id;
    ", [
        new SqliteParameter("Id", coupon.Id),
        new SqliteParameter("Name", coupon.Name)
    ])).FirstOrDefault();

    if (exists > 0)
    {
        return Results.BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = ["Coupon already exists"], StatusCode = System.Net.HttpStatusCode.BadRequest });
    }

    var rowsAffected = await _uow.Coupons.ExecuteSqlAsync(@"
        UPDATE Coupons
        SET
            Percent = @Percent,
            IsActive = @IsActive,
            Name = @Name
        WHERE Id = @Id;
    ", [
        new SqliteParameter("Id", coupon.Id),
        new SqliteParameter("Name", coupon.Name),
        new SqliteParameter("Percent", coupon.Percent),
        new SqliteParameter("IsActive", coupon.IsActive),
    ]);

    if (rowsAffected > 0)
    {
        return Results.Ok(new ApiResponse { IsSuccess = true, StatusCode = System.Net.HttpStatusCode.OK });
    }
    else
    {
        return Results.NotFound(new ApiResponse { IsSuccess = false, StatusCode = System.Net.HttpStatusCode.NotFound });
    }
}).WithName("UpdateCoupon").Accepts<CouponDto>("application/json").Produces<ApiResponse>(200).Produces(404); ;

app.MapDelete("/api/coupon/{id:int}", async (IUnitOfWork _uow, int id) =>
{
    var rowsAffected = await _uow.Coupons.ExecuteSqlAsync($@"DELETE FROM Coupons WHERE Id = @Id", [new SqliteParameter("Id", id)]);
    if (rowsAffected > 0)
    {
        return Results.NoContent();
    }
    else
    {
        return Results.NotFound();
    }
}).WithName("DeleteCoupon").Produces(204).Produces(404);

app.UseHttpsRedirection();

app.Run();