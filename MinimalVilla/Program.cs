using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MinimalVilla;
using MinimalVilla.Data;
using MinimalVilla.Data.UnitOfWork;
using MinimalVilla.Endpoints;
using MinimalVilla.Models.Domain;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, SwaggerConfiguration>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);
// Fluent Validators
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddIdentityCore<ApplicationUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddApiEndpoints();

// Authentication - Bearer Token
builder.Services.AddAuthentication().AddBearerToken(IdentityConstants.BearerScheme);
// Authentication - Cookies
// builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();

// Authorization
builder.Services.AddAuthorizationBuilder();

var app = builder.Build();

// Generate endpoints for Identity Auth
app.MapIdentityApi<ApplicationUser>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// add endpoints
app.ConfigureCouponEndpoints();

app.UseHttpsRedirection();

app.Run();