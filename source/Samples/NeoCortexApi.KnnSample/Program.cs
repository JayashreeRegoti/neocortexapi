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

var width = 30;
var height = 30;
var imageEncoderSettings = new BinarizerParams()
{
    ImageHeight = height,
    ImageWidth = width,
    RedThreshold = 128,
    GreenThreshold = 128,
    BlueThreshold = 128,
};

await CreateInputDataSet(host.Services, trainingDataFolderPath, testDataFolderPath);
var knnClassifierFactory = host.Services.GetRequiredService<KnnClassifierFactory>();
var predictor = await knnClassifierFactory.CreatePredictor(trainingDataFolderPath, imageEncoderSettings);
await knnClassifierFactory.ValidateTestData(testDataFolderPath, predictor);

return;


async Task CreateInputDataSet(IServiceProvider services, string trainingDataDirectoryPath, string testDataDirectoryPath)
{
    Console.WriteLine("Creating image with lines...");
    var imageGenerator = services.GetRequiredService<ImageGenerator>();

    // remove previous training data
    Directory.Delete(trainingDataDirectoryPath, true);
    Directory.Delete(testDataDirectoryPath, true);
    
    if(Directory.Exists(testDataDirectoryPath) && Directory.GetFiles(testDataDirectoryPath).Length > 0)
    {
        Console.WriteLine("Test data already exists. Skipping creation.");
    }
    else
    {
        await imageGenerator.CreateImagesWithLine(testDataDirectoryPath, width ,height, 10);
    }
    
    if(Directory.Exists(trainingDataDirectoryPath) && Directory.GetFiles(trainingDataDirectoryPath).Length > 0)
    {
        Console.WriteLine("Training data already exists. Skipping creation.");
    }
    else
    {
        await imageGenerator.CreateImagesWithLine(trainingDataDirectoryPath,width ,height, 9);
    }
    
    Console.WriteLine("Completed Creating image with lines.");
}
