﻿using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry;
using AIOChatbot.Configurations;
using System.Reflection;
using Microsoft.Extensions.Logging.Console;

namespace AIOChatbot.Logging;

public static class Extensions
{
    private readonly static string serviceName = Assembly.GetExecutingAssembly().GetName().Name ?? "botapp";
    public static (TracerProvider, MeterProvider) AddDiagnosticsServices(this ServiceCollection services, IConfig config, string sourceName)
    {

        services.AddLogging(c =>
        {
            c.SetMinimumLevel((LogLevel)Enum.Parse(typeof(LogLevel), config.LogLevel));
            c.AddSimpleConsole();
            c.AddOpenTelemetry(o =>
            {
                o.AddAzureMonitorLogExporter(o => o.ConnectionString = config.OpenTelemetryConnectionString)
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName, serviceInstanceId: Environment.MachineName))
                    .IncludeScopes = true;
            });
        });

        var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
            .AddSource(sourceName)
            .AddAzureMonitorTraceExporter(o => o.ConnectionString = config.OpenTelemetryConnectionString)
            .AddHttpClientInstrumentation()
            .Build();

        var metricProvider = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName, serviceInstanceId: Environment.MachineName))
            .AddMeter(sourceName)
            .AddHttpClientInstrumentation()
            .AddAzureMonitorMetricExporter(o => o.ConnectionString = config.OpenTelemetryConnectionString)
            .Build();

        return (tracerProvider, metricProvider);
    }
}