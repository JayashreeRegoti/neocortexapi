// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NeoCortexApi.KnnSample;
using NeoCortexApi.Tools;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

var logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} {Level:u4}: {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Logging.AddSerilog(logger);
builder.Services.AddSingleton<ILogger>(sp => new LoggerConfiguration()
    .MinimumLevel.Information()
    .CreateLogger());

builder.Services.AddTransient<ImageGenerator>();
builder.Services.AddTransient<KnnClassifierFactory>();
var host = builder.Build();

var trainingDataFolderPath = "./TrainingData";
var testDataFolderPath = "./TestData";

await CreateInputDataSet(host.Services, trainingDataFolderPath, testDataFolderPath);

var knnClassifierFactory = host.Services.GetRequiredService<KnnClassifierFactory>();
var predictor = await knnClassifierFactory.CreatePredictor(trainingDataFolderPath);
await knnClassifierFactory.ValidateTestData(testDataFolderPath, predictor);

return;


async Task CreateInputDataSet(IServiceProvider services, string trainingDataFolderPath, string testDataFolderPath)
{
    Console.WriteLine("Creating image with lines...");
    var imageGenerator = services.GetRequiredService<ImageGenerator>();

    await imageGenerator.CreateImagesWithLine(testDataFolderPath, 30);
    await imageGenerator.CreateImagesWithLine(trainingDataFolderPath, 100);
    
    Console.WriteLine("Completed Creating image with lines.");
}
