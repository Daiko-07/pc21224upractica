using Microsoft.EntityFrameworkCore;
using PC212242.UPractica.Clerky.Platform.Paperwork.Application.Internal.CommandServices;
using PC212242.UPractica.Clerky.Platform.Paperwork.Application.Services;
using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Repositories;
using PC212242.UPractica.Clerky.Platform.Paperwork.Infrastructure.Persistence.EFC.Repositories;
using PC212242.UPractica.Clerky.Platform.Shared.Infrastructure.Persistence.EFC;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 36))
    )
);

builder.Services.AddScoped<IContractRepository, ContractRepository>();
builder.Services.AddScoped<IContractCommandService, ContractCommandService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection();

app.MapControllers();

app.Run();