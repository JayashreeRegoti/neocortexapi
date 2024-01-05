// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NeoCortexApi.KnnSample;
using NeoCortexApi.Tools;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTransient<ImageGenerator>();
builder.Services.AddTransient<KnnClassifierFactory>();
var host = builder.Build();

var trainingDataFolderPath = "./TrainingData";
var testDataFolderPath = "./TestData";

await CreateInputDataSet(host.Services, trainingDataFolderPath, testDataFolderPath);

var knnClassifierFactory = host.Services.GetRequiredService<KnnClassifierFactory>();
// var classifier = await knnClassifierFactory.GetTrainModel(trainingDataFolderPath);

return;


async Task CreateInputDataSet(IServiceProvider services, string trainingDataFolderPath, string testDataFolderPath)
{
    Console.WriteLine("Creating image with lines...");
    var imageGenerator = services.GetRequiredService<ImageGenerator>();

    await imageGenerator.CreateImagesWithLine(testDataFolderPath, 30);
    await imageGenerator.CreateImagesWithLine(trainingDataFolderPath, 300);
    
    Console.WriteLine("Completed Creating image with lines.");
}
