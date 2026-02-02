using LogParserWorkerService;
using LogParserWorkerService.Models;
using LogParserWorkerService.Services.Contracts;
using LogParserWorkerService.Services.Services;


var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<ILogDataReader, LogDataReader>();
builder.Services.AddSingleton<LogReportGenerator>();
builder.Services.AddSingleton<LogStats>();
builder.Services.AddSingleton<TopItem>();
builder.Services.AddSingleton<ILogParser, LogParser>();
builder.Services.AddSingleton<IReportGenerator, ReportGenerator>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();