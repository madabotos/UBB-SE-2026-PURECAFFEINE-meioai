using Microsoft.EntityFrameworkCore;
using Property_and_Management.DataAccess;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Repository;

var builder = WebApplication.CreateBuilder(args);

var boardRentConnectionString = builder.Configuration.GetConnectionString("BoardRent")
    ?? throw new InvalidOperationException("Connection string 'BoardRent' is not configured.");

DatabaseInitializer.EnsureDatabaseInitialized(boardRentConnectionString);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(boardRentConnectionString));

builder.Services.AddScoped<IUserRepository>(_ => new UserRepository(boardRentConnectionString));
builder.Services.AddScoped<IGameRepository>(_ => new GameRepository(boardRentConnectionString));
builder.Services.AddScoped<IRequestRepository>(_ => new RequestRepository(boardRentConnectionString));
builder.Services.AddScoped<IRentalRepository>(_ => new RentalRepository(boardRentConnectionString));
builder.Services.AddScoped<INotificationRepository>(_ => new NotificationRepository(boardRentConnectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
