using GetABursary.Core;
using GetABursary.Core.Data.Seeding;
using GetABursary.Worker.Scheduling;
using GetABursary.Worker.Sources.Http;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddCoreData(builder.Configuration);
builder.Services.AddPoliteHttpClient();
builder.Services.AddIngestionScheduling(builder.Configuration);

var host = builder.Build();

await using (var scope = host.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<IDataSeeder>().SeedAsync(CancellationToken.None);
}

host.Run();
