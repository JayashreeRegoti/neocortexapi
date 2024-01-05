// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NeoCortexApi.Tools;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTransient<ImageGenerator>();
var host = builder.Build();

Console.WriteLine("Creating image with lines...");

var imageGenerator = host.Services.GetRequiredService<ImageGenerator>();

var testDataFolderPath = "./TestData";
await imageGenerator.CreateImagesWithLine(testDataFolderPath, 30);

var trainingDataFolderPath = "./TrainingData";
await imageGenerator.CreateImagesWithLine(trainingDataFolderPath, 300);

Console.WriteLine("Completed Creating image with lines.");
