// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NeoCortexApi.Tools;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTransient<ImageGenerator>();
var host = builder.Build();

Console.WriteLine("Creating image with lines...");

var imageGenerator = host.Services.GetRequiredService<ImageGenerator>();
await imageGenerator.CreateImagesWithLine(100);

Console.WriteLine("Completed Creating image with lines.");
