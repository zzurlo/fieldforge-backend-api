using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.EntityFrameworkCore;
using Azure.Communication.Sms;

using MediatR;
using Stripe;

using FieldForge.Api.Data;
using FieldForge.Api.Services;
using FieldForge.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddMediatR(typeof(Program).Assembly);
builder.Services.AddScoped<INotificationService, NotificationService>();

// Configure Stripe
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// Configure Azure Communication Services
var azureCommunicationConnectionString = builder.Configuration["AzureCommunication:ConnectionString"];
builder.Services.AddSingleton(_ => new SmsClient(azureCommunicationConnectionString));

builder.Services.AddScoped<INotificationService, NotificationService>();

// Configure Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireOrganizationAdmin", policy =>
        policy.RequireClaim("roles", "OrganizationAdmin"));
});


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("FieldForgeDb")));

// Configure JWT Bearer options
builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters.ValidateIssuer = true;
});

// Configure Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireOrganizationAdmin", policy =>
        policy.RequireClaim("roles", "OrganizationAdmin"));
    
    options.AddPolicy("RequireTechnician", policy =>
        policy.RequireClaim("roles", "Technician"));
    
    options.AddPolicy("RequireBiller", policy =>
        policy.RequireClaim("roles", "Biller"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder => builder
            .WithOrigins(
                "http://localhost:3000",
                "https://my-production-frontend"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("Content-Disposition"));
});


var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHub<DispatchHub>("/hubs/dispatch");

app.UseHttpsRedirection();
app.UseCors("CorsPolicy");
app.UseAuthorization();
app.MapControllers();

app.Run();