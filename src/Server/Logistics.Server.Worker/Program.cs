using Logistics.Server.Application;
using Logistics.Server.Infrastructure;
using Logistics.Server.Integrations;
using Logistics.Server.Worker.Jobs;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog
builder.Services.AddSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(builder.Configuration));

// Core Service Layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIntegrations(builder.Configuration);

// Hosted Background Services
builder.Services.AddHostedService<OutboxMessageProcessorJob>();
builder.Services.AddHostedService<CarrierTrackingSyncJob>();
builder.Services.AddHostedService<NotificationDeliveryWorkerJob>();
builder.Services.AddHostedService<BackgroundJobQueueWorkerJob>();

var host = builder.Build();
host.Run();
