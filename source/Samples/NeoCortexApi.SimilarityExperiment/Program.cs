using Daenet.ImageBinarizerLib.Entities;
using NeoCortexApi.SimilarityExperiment;
using NeoCortexApi.SimilarityExperiment.Input;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// initialize logger
var logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

// register services
builder.Services.AddTransient<InputSdrCreator>();
builder.Services.AddTransient<SimilarityExperiment>();

// build host to get services
var host = builder.Build();

logger.Information("--------------START--------------");
// fetch InputSdrCreator service from host
var inputSdrCreator = host.Services.GetRequiredService<InputSdrCreator>();
var inputSdrDirectoryPath = "./InputSdrs";

// create input SDRs from a 2d array of 1 and 0 values to images
await inputSdrCreator.CreateInputSdrs(inputSdrDirectoryPath);

// fetch SimilarityExperiment service from host
var experiment = host.Services.GetRequiredService<SimilarityExperiment>();

// initialize image encoder settings for input SDR images
var imageEncoderSettings = new BinarizerParams()
{
    ImageHeight = 30,
    ImageWidth = 30,
    RedThreshold = 128,
    GreenThreshold = 128,
    BlueThreshold = 128,
};

// run similarity experiment using input SDR data
await experiment.RunExperiment(inputSdrDirectoryPath, imageEncoderSettings);

logger.Information("--------------END--------------");
logger.Information("press any key to exit...");
Console.ReadLine(); 
