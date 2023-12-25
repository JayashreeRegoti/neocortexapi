using FluentAssertions;
using SkiaSharp;

namespace NeoCortexApi.Tests
{
    public class ImageTests
    {
        [Fact]
        public void ImageTest()
        {
            // Provided all the input images and fetch all .jpg files
            var imageFilePaths = Directory.EnumerateFiles("./TestData", "*.jpg", SearchOption.AllDirectories).ToList();
            // We are declaring a dictionary of string and 2-D array to store image in 1 0 format with its file path.
            var imageBinaries = new Dictionary<string, int[][]>();

            //iterate through each image
            foreach (var imageFilePath in imageFilePaths)
            {
                //Decode image using SkiaSharp package
                var image = SKBitmap.Decode(imageFilePath);

                //This is a temporary 2D integer array to store image pixel values as 1 and 0
                var imageBinary = new int[image.Width][];

                //assigning each pixel with a binary number
                for (int x = 0; x < image.Width; x++)
                {
                    imageBinary[x] = new int[image.Height];
                    for (int y = 0; y < image.Height; y++)
                    {
                        var pixel = image.GetPixel(x, y);
                        var red = pixel.Red;
                        var green = pixel.Green;
                        var blue = pixel.Blue;
                        if (red > 128 && green > 128 && blue > 128)
                        {
                            imageBinary[x][y] = 1;
                        }
                        else
                        {
                            imageBinary[x][y] = 0;
                        }
                    }
                }

                //Adding the above 2D array image in a dictionary with Image file path
                imageBinaries.Add(imageFilePath, imageBinary);
            }

#if DEBUG
            // for image binary visualization
            var imageBinaryStrings = imageBinaries.ToDictionary(imageBinary => imageBinary.Key, imageBinary =>
                string.Join("\n", imageBinary.Value.Select(x =>
                    string.Join(",", x))).ToString());
#endif
            //SelectMany is used to attach all the rows in a single array
            var imageBinaryArrays = imageBinaries.Select(imageBinary => imageBinary.Value.SelectMany(x => x).ToArray()).ToList();

            foreach (var imageBinaryArray in imageBinaryArrays)
            {
                // TODO: do stuff here
                // var classifier = new KNN(3);
                // int[] inputVector = HTM.generateInputVector(BagOfWords, wordsInInput);
                // classifier.Train(new List<Vector>(), imageFilePaths);
                // string classification = classifier.Classify(inputVector);
            }

            imageBinaryArrays.Count.Should().Be(3);
            foreach (var imageBinaryArray in imageBinaryArrays)
            {
                imageBinaryArray.Length.Should().Be(100);
            }
        }
    }
}
