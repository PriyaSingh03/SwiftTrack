using LastMileDelivery.API.Data; // Ensure this matches your namespace
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Connection String
var strConn = builder.Configuration.GetSection("ConnectionString")["DefaultConnection"];
builder.Services.AddDbContext<ApplicationDbContext>(options =>
   options.UseSqlServer(strConn));

// 2. Add CORS Policy (Crucial for MVC to API communication)
builder.Services.AddCors(options => {
    options.AddPolicy("AllowMVC", policy => {
        policy.AllowAnyOrigin() // In production, replace with your MVC URL
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 3. Enable CORS Middleware (Must be before Authorization)
app.UseCors("AllowMVC");

app.UseAuthorization();
app.MapControllers();
app.Run();