using GetABursary.Worker.Sources.Http;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddPoliteHttpClient();

var host = builder.Build();
host.Run();
