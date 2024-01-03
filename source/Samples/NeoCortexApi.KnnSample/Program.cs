// See https://aka.ms/new-console-template for more information

using NeoCortexApi.Tools;

Console.WriteLine("Creating image with lines...");
await Run();
Console.WriteLine("Completed Creating image with lines.");
return;

async Task Run()
{
    await Task.Yield();
    await ImageGenerator.CreateImageWithLines(100);
}
