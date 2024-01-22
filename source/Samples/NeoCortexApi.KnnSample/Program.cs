// See https://aka.ms/new-console-template for more information

using Daenet.ImageBinarizerLib.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoCortexApi.KnnSample;
using NeoCortexApi.Tools;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

var logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

builder.Services.AddTransient<ImageGenerator>();
builder.Services.AddTransient<KnnClassifierFactory>();
builder.Services.AddTransient<MultiSequenceLearning>();
var host = builder.Build();

var trainingDataFolderPath = "./TrainingData";
var testDataFolderPath = "./TestData";
var imageEncoderSettings = new BinarizerParams()
{
    ImageHeight = 30,
    ImageWidth = 30,
    RedThreshold = 128,
    GreenThreshold = 128,
    BlueThreshold = 128,
};

await CreateInputDataSet(host.Services, trainingDataFolderPath, testDataFolderPath, imageEncoderSettings);
var knnClassifierFactory = host.Services.GetRequiredService<KnnClassifierFactory>();
var predictor = await knnClassifierFactory.CreatePredictor(trainingDataFolderPath, imageEncoderSettings);
await knnClassifierFactory.ValidateTestData(testDataFolderPath, predictor);

return;


async Task CreateInputDataSet(IServiceProvider services, string trainingDataFolderPath, string testDataFolderPath,
    BinarizerParams binarizerParams)
{
    Console.WriteLine("Creating image with lines...");
    var imageGenerator = services.GetRequiredService<ImageGenerator>();

    await imageGenerator.CreateImagesWithLine(testDataFolderPath, 10);
    await imageGenerator.CreateImagesWithLine(trainingDataFolderPath, 30);
    
    Console.WriteLine("Completed Creating image with lines.");
}
