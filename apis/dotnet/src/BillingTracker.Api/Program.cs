using System.Text.Json.Serialization;
using BillingTracker.Application.Ingestion;
using BillingTracker.Infrastructure.Data;
using BillingTracker.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("BillingTrackerDb")
    ?? throw new InvalidOperationException("Connection string 'BillingTrackerDb' not found.");

builder.Services.AddSingleton(new DatabaseConnectionFactory(connectionString));
builder.Services.AddScoped<IPaymentIngestionRepository, PaymentIngestionRepository>();
builder.Services.AddScoped<PaymentIngestionService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
