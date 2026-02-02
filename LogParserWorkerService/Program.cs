using LogParserWorkerService;
using LogParserWorkerService.Models;
using LogParserWorkerService.Repositories.Concrete;
using LogParserWorkerService.Repositories.Interface;
using LogParserWorkerService.Services.Concrete;
using LogParserWorkerService.Services.Contracts;
using LogParserWorkerService.Services.Interface;
using LogParserWorkerService.Services.Services;


var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<ILogDataReader, LogDataReader>();
builder.Services.AddSingleton<LogReportGenerator>();
builder.Services.AddSingleton<LogStats>();
builder.Services.AddSingleton<TopItem>();
builder.Services.AddSingleton<ILogParser, LogParser>();
builder.Services.AddSingleton<IReportGenerator, ReportGenerator>();
builder.Services.AddSingleton<IBulkLogRepository, BulkLogRepository>();
builder.Services.AddSingleton<IUpsertService, UpsertService>();


builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();