using Daenet.ImageBinarizerLib.Entities;
using NeoCortexApi.SimilarityExperiment;
using NeoCortexApi.SimilarityExperiment.Input;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

var logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

builder.Services.AddTransient<InputSdrCreator>();
builder.Services.AddTransient<SimilarityExperiment>();
var host = builder.Build();

logger.Information("--------------START--------------");
var inputSdrCreator = host.Services.GetRequiredService<InputSdrCreator>();
var inputSdrDirectoryPath = "./InputSdrs";
await inputSdrCreator.CreateInputSdrs(inputSdrDirectoryPath);

var experiment = host.Services.GetRequiredService<SimilarityExperiment>();
var imageEncoderSettings = new BinarizerParams()
{
    ImageHeight = 30,
    ImageWidth = 30,
    RedThreshold = 128,
    GreenThreshold = 128,
    BlueThreshold = 128,
};
await experiment.RunExperiment(inputSdrDirectoryPath, imageEncoderSettings);
logger.Information("--------------END--------------");
Console.ReadLine(); 
