using n2n.Extensions;
using n2n.Infrastructure.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder
    .AddConfiguration()
    .AddInfrastructure()
    .AddTelemetry()
    .AddWorkers();

var host = builder.Build();
host.Run();
