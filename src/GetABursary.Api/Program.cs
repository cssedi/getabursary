using GetABursary.Api.Extensions;
using GetABursary.Core;
using GetABursary.Core.Data;
using GetABursary.Core.Data.Seeding;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddCommon();
builder.Services.AddCoreData(builder.Configuration);
builder.Services.AddReportsFeature(builder.Configuration, builder.Environment);
builder.Services.AddTaxonomyFeature();
builder.Services.AddBursaryFeature();

var app = builder.Build();

if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<GetABursaryDbContext>();
    await db.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<IDataSeeder>().SeedAsync(CancellationToken.None);
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "GetABursary API v1"));
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.Run();

public partial class Program;
