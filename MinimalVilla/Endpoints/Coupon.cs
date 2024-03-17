using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using MinimalVilla.Data.UnitOfWork;
using MinimalVilla.Models.Domain;
using MinimalVilla.Models.Dto;
using MinimalVilla.Models.Extensions;
using MinimalVilla.Utility;

namespace MinimalVilla.Endpoints
{
    public static class CouponEndpoints
    {
        public static void ConfigureCouponEndpoints(this WebApplication app)
        {
            // One way or authorizing
            app.MapGet("/api/coupon", GetAll)
                .WithName("GetCoupons")
                .Produces<ApiResponse>(200)
                .Produces(401)
                .Produces(403)
                .RequireAuthorization(SD.Role_Admin);

            app.MapGet("/api/coupon/{id:int}", Get)
                .WithName("GetCoupon")
                .AddEndpointFilter(async (context, next) =>
                    {
                        // Filters example.  We are using a filter here as just another way to do validations
                        // They are of course capable or other business logic besides validations
                        var id = context.GetArgument<int>(1); // get argument in position 1. See delegate "Get" below.
                        if (id == 0)
                        {
                            return Results.BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = ["Invalid Coupon Id"], StatusCode = System.Net.HttpStatusCode.BadRequest });
                        }
                        // return await next(context);
                        // OR
                        // do something before execution of endpoint
                        Console.WriteLine("Before 1st filter ");
                        var result = await next(context);
                        // do something after execution of endpoint
                        Console.WriteLine("After 1st filter");

                        return result;
                    }).AddEndpointFilter(async (context, next) =>
                    {
                        Console.WriteLine("Before 2nd filter ");
                        var result = await next(context);
                        Console.WriteLine("After 2nd filter");

                        return result;
                    })
                .Produces<ApiResponse>(200)
                .Produces(401)
                .Produces(403)
                .Produces(404);

            app.MapPost("/api/coupon", Post)
                .WithName("CreateCoupon")
                .Accepts<CouponCreateDto>("application/json")
                .Produces<ApiResponse>(201)
                .Produces(401)
                .Produces(403)
                .Produces(400);

            app.MapPut("/api/coupon", Put)
                .WithName("UpdateCoupon")
                .Accepts<CouponDto>("application/json")
                .Produces<ApiResponse>(200)
                .Produces(401)
                .Produces(403)
                .Produces(404);

            app.MapDelete("/api/coupon/{id:int}", Delete)
                .WithName("DeleteCoupon")
                .Produces(204)
                .Produces(401)
                .Produces(403)
                .Produces(404);

            app.MapGet("/api/coupon/special", Special)
                .WithName("SpecialCoupon")
                .Produces(200)
                .Produces(401)
                .Produces(403)
                .Produces(404);
        }

        private async static Task<IResult> GetAll(ILogger<Program> _logger, IUnitOfWork _uow)
        {
            _logger.Log(LogLevel.Information, "Getting all coupons");
            var coupons = (await _uow.Coupons.FromSqlAsync($@"SELECT * FROM Coupons;", [])).Select(c => c.ToDto()).ToList();
            return Results.Ok(new ApiResponse { IsSuccess = true, Result = coupons, StatusCode = System.Net.HttpStatusCode.OK });
        }

        [Authorize]
        private async static Task<IResult> Get(IUnitOfWork _uow, int id)
        {
            var coupon = (await _uow.Coupons.FromSqlAsync($@"SELECT * FROM Coupons WHERE Id = @Id", [new SqliteParameter("Id", id)])).FirstOrDefault()?.ToDto();
            return coupon is not null
                ? Results.Ok(new ApiResponse { IsSuccess = true, Result = coupon, StatusCode = System.Net.HttpStatusCode.OK })
                : Results.NotFound(new ApiResponse { IsSuccess = false, StatusCode = System.Net.HttpStatusCode.NotFound });
        }

        [Authorize(Roles = SD.Role_Admin)]
        private static async Task<IResult> Post(IUnitOfWork _uow, IValidator<CouponCreateDto> _validation, [FromBody] CouponCreateDto newCoupon)
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
        }

        [Authorize(Roles = SD.Role_Admin)]
        private static async Task<IResult> Put(IUnitOfWork _uow, IValidator<CouponDto> _validation, [FromBody] CouponDto coupon)
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
        }

        [Authorize(Roles = SD.Role_Admin)]
        private static async Task<IResult> Delete(IUnitOfWork _uow, int id)
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
        }

        [Authorize(Roles = SD.Role_Admin)]
        private static async Task<IResult> Special(IUnitOfWork _uow, [AsParameters] CouponRequest req)
        {
            req.Logger.Log(LogLevel.Information, "Getting special coupons");
            var coupons = (await _uow.Coupons.FromSqlAsync($@"
                SELECT *
                FROM 
                    Coupons 
                WHERE 
                        @Name is NULL
                    OR 
                        Name LIKE '%' || @Name || '%'
                LIMIT @PageSize 
                OFFSET @Page;
            ", [
                     new SqliteParameter("Name", req.CoupoName ?? (object)DBNull.Value),
                    new SqliteParameter("PageSize", req.PageSize),
                    new SqliteParameter("Page", req.Page-1)
                 ])).Select(c => c.ToDto());

            return Results.Ok(new ApiResponse { IsSuccess = true, Result = coupons, StatusCode = System.Net.HttpStatusCode.OK });
        }
    }
}