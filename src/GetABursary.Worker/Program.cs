using GetABursary.Core;
using GetABursary.Core.Data.Seeding;
using GetABursary.Core.Repositories.Ingestion;
using GetABursary.Worker.Extraction;
using GetABursary.Worker.Pipeline;
using GetABursary.Worker.Pipeline.Jobs;
using GetABursary.Worker.Pipeline.Merging;
using GetABursary.Worker.Scheduling;
using GetABursary.Worker.Sources;
using GetABursary.Worker.Sources.Adapters.StudyTrust;
using GetABursary.Worker.Sources.Http;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddCoreData(builder.Configuration);
builder.Services.AddPoliteHttpClient();
builder.Services.AddIngestionScheduling(builder.Configuration);
builder.Services.AddEligibilityExtraction(builder.Configuration);
builder.Services.AddBursaryMerging();
builder.Services.AddIngestionPipeline();
builder.Services.AddSingleton<ISourceAdapter, GetABursary.Worker.Sources.Adapters.ZaBursaries.ZaBursariesAdapter>();
builder.Services.AddSingleton<ISourceAdapter, StudyTrustAdapter>();
builder.Services.AddSingleton<SourceAdapterRegistry>();
builder.Services.AddScoped<IRawListingRepository, RawListingRepository>();
builder.Services.AddScoped<ISnapshotStore, SnapshotStore>();

var host = builder.Build();

await using (var scope = host.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<IDataSeeder>().SeedAsync(CancellationToken.None);
}

host.Run();
