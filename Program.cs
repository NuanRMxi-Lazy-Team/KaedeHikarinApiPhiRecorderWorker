using KaedeHikarinCialloTeam.PhiRecorder.Worker;
using KaedeHikarinCialloTeam.PhiRecorder.Worker.Interop;
using KaedeHikarinCialloTeam.PhiRecorder.Worker.Messaging;
using KaedeHikarinCialloTeam.PhiRecorder.Worker.Rendering;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<PhiRendererOptions>(builder.Configuration.GetSection("PhiRecorder"));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.Configure<RenderWorkerOptions>(builder.Configuration.GetSection(RenderWorkerOptions.SectionName));
builder.Services.Configure<S3Options>(builder.Configuration.GetSection(S3Options.SectionName));

builder.Services.AddSingleton(static services =>
    new PhiRenderer(services.GetRequiredService<IOptions<PhiRendererOptions>>().Value));
builder.Services.AddSingleton<RabbitMqClientFactory>();
builder.Services.AddSingleton<RenderEventPublisher>();
builder.Services.AddSingleton<RenderJobState>();
builder.Services.AddSingleton<ChartDownloader>();
builder.Services.AddSingleton<ResultUploader>();
builder.Services.AddSingleton<RenderJobProcessor>();

builder.Services.AddHttpClient("chart", client =>
{
    client.Timeout = Timeout.InfiniteTimeSpan;
});

builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<RenderTaskConsumer>();
builder.Services.AddHostedService<ControlConsumer>();

var host = builder.Build();
host.Run();
