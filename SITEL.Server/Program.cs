using Microsoft.EntityFrameworkCore;
using SITEL.Server.Data;

var builder = WebApplication.CreateBuilder(args);

// Registrar DbContext para SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();

// COMENTA O ELIMINA ESTAS LÍNEAS TEMPORALMENTE:
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

var app = builder.Build();

// COMENTA O ELIMINA ESTE BLOQUE:
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();