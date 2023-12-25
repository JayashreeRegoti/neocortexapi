// See https://aka.ms/new-console-template for more information

using NeoCortexApi.Tools;

Console.WriteLine("Hello, World!");
await Run();

async Task Run()
{
    await Task.Yield();
    await ImageGenerator.CreateHorizontalImage();
}
