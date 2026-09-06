using KaedeHikarinCialloTeam.PhiRecorder.Worker;
using KaedeHikarinCialloTeam.PhiRecorder.Worker.Interop;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<PhiRendererOptions>(builder.Configuration.GetSection("PhiRecorder"));
builder.Services.AddSingleton(static services =>
    new PhiRenderer(services.GetRequiredService<IOptions<PhiRendererOptions>>().Value));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
