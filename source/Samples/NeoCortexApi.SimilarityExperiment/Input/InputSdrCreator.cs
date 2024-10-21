using NeoCortexApi.Tools;

namespace NeoCortexApi.SimilarityExperiment.Input
{
    /// <summary>
    /// Input SDR creator class to create input SDR images from 2D array of 1 and 0 values.
    /// </summary>
    public class InputSdrCreator
    {
        private readonly ILogger<InputSdrCreator> _logger;

        public InputSdrCreator(ILogger<InputSdrCreator> logger)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// It creates input SDR images from 2D array of 1 and 0 values.
        /// </summary>
        /// <param name="inputSdrDirectoryPath">path to store input SDR images</param>
        public async Task CreateInputSdrs(string inputSdrDirectoryPath)
        {
            _logger.LogInformation("Creating input SDR images...");
            
            // delete existing input SDR directory
            if(Directory.Exists(inputSdrDirectoryPath))
            {
                Directory.Delete(inputSdrDirectoryPath, true);
            }
            
            // create input SDR directory
            Directory.CreateDirectory(inputSdrDirectoryPath);
            
            // get input SDR data in 2D arrays of 1 and 0 values
            var inputSdrs = InputSdrData.GetInputSdrs();
            
            // create input SDR images from 2D array of 1 and 0 values
            foreach (KeyValuePair<string, int[][]> inputSdr in inputSdrs)
            {
                // create input SDR image file path to save the image
                var inputSdrPath = Path.Combine(inputSdrDirectoryPath, $"{inputSdr.Key}.png");
                
                // define the height and width of the image
                var inputSdrValue = inputSdr.Value;
                var imageheight = inputSdrValue.Length;
                var imagewidth = inputSdrValue[0].Length;
                
                // create a 2D array to store the pixel values of the image
                var inputImageData = new int[imageheight][];
                
                // set the value of each pixel in the image
                for (int i = 0; i < imageheight; i++)
                {
                    inputImageData[i] = new int[imagewidth];
                    for (int j = 0; j < imagewidth; j++)
                    {
                        // if the value is 1, set the pixel value to 255 (white), else set it to 0 (black)
                        inputImageData[i][j] = inputSdrValue[i][j] == 1 ? 255 : 0;
                    }
                }
                
                // generate the input SDR image file
                await ImageGenerator.GenerateImage(inputSdrPath, imagewidth, imageheight, inputImageData);
                _logger.LogInformation("Input SDR image created at {inputSdrPath}", inputSdrPath);
            }
        }
    }
}