using GetABursary.Core;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddCoreData(builder.Configuration);

var host = builder.Build();
host.Run();
