using GetABursary.Core;
using GetABursary.Worker.Sources.Http;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddCoreData(builder.Configuration);
builder.Services.AddPoliteHttpClient();

var host = builder.Build();
host.Run();
