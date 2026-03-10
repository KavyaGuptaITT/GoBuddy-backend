using GoBuddy.BusinessLayer.Interfaces;
using GoBuddy.BusinessLayer.Services;
using GoBuddy.Application.Interfaces;
using GoBuddy.Infrastructure.Persistence;
using GoBuddy.Infrastructure.Repositories;
using GoBuddy.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
 
var builder = WebApplication.CreateBuilder(args);
 
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
 
builder.Services.AddControllers();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IRideCancellationService, RideCancellationService>();
builder.Services.AddScoped<IRideSessionRepository, RideSessionRepository>();
builder.Services.AddScoped<IRideRequestRepository, RideRequestRepository>();
 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR(); 
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});
 
var key = Encoding.ASCII.GetBytes("THIS_IS_MY_SUPER_SECRET_KEY_12345678");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});
 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
 
var app = builder.Build();
 
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "GoBuddy API V1");
        c.RoutePrefix = "swagger"; 
    });
}
 
app.UseHttpsRedirection();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<GoBuddy.API.Hubs.DriverLocationHub>("/hubs/driver");
//app.MapHub<NotificationHub>("/notificationHub");
app.Run();