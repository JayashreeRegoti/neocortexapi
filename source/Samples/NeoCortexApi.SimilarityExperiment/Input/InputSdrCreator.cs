using NeoCortexApi.Tools;

namespace NeoCortexApi.SimilarityExperiment.Input
{
    public class InputSdrCreator
    {
        public static async Task CreateInputSdrs(string inputSdrDirectoryPath)
        {
            if(Directory.Exists(inputSdrDirectoryPath))
            {
                Directory.Delete(inputSdrDirectoryPath, true);
            }
            
            Directory.CreateDirectory(inputSdrDirectoryPath);
            var inputSdrs = InputSdrData.GetInputSdrs();
            
            foreach (KeyValuePair<string, int[][]> inputSdr in inputSdrs)
            {
                var inputSdrPath = Path.Combine(inputSdrDirectoryPath, $"{inputSdr.Key}.png");
                var inputSdrValue = inputSdr.Value;
                var imageheight = inputSdrValue.Length;
                var imagewidth = inputSdrValue[0].Length;
                var inputImageData = new int[imageheight][];
                // create a variable of array of integer arrays
                
                for (int i = 0; i < imageheight; i++)
                {
                    inputImageData[i] = new int[imagewidth];
                    for (int j = 0; j < imagewidth; j++)
                    {
                        inputImageData[i][j] = inputSdrValue[i][j] == 1 ? 255 : 0;
                    }
                }
                await ImageGenerator.GenerateImage(inputSdrPath, imagewidth, imageheight, inputImageData);
            }
        }
    }
}