using Microsoft.EntityFrameworkCore;
using StudentFleaMarket.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);


// Register AppDbContext with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Other Swagger / Controller configurations...
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.Run();