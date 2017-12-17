using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Zavolokas;
using Zavolokas.GdiExtensions;
using Zavolokas.ImageProcessing.Parallel.PatchMatch;
using Zavolokas.ImageProcessing.PatchMatch;
using Zavolokas.Structures;

namespace PartiallyEmptyImage
{
    class Program
    {
        static void Main(string[] args)
        {
            const string basePath = "..\\..\\..\\images";

            var destImageName = "pm2small.png";
            var srcImageName = "pm1small.png";
            var emptyAreaImageName = "pm2small_ignore.png";

            // This is our input data.
            // NOTE: in real-world applications, created Bitmaps must be destoroyed.
            var destImage = new Bitmap(Path.Combine(basePath, destImageName)).ToRgbImage().FromRgbToLab();
            var srcImage = new Bitmap(Path.Combine(basePath, srcImageName)).ToRgbImage().FromRgbToLab();
            var emptyArea = new Bitmap(Path.Combine(basePath, emptyAreaImageName)).ToArea();
            var destArea = Area2D.Create(0, 0, destImage.Width, destImage.Height);

            var map = new Area2DMapBuilder()
                .InitNewMap(
                    destArea,
                    Area2D.Create(0, 0, srcImage.Width, srcImage.Height))   // src area
                .Build();

            var input = new PmData(destImage, srcImage, map);
            input.Settings.PatchSize = 5;
            input.Settings.IterationsAmount = 2;
            input.DestImagePixelsArea = destArea.Substract(emptyArea);

            var nnfPipeline = new PatchMatchPipeline(ImagePatchDistance.Cie76);
            nnfPipeline.SetInput(input);
            var nnf = nnfPipeline.Process()
                .Output[0]
                .Nnf;

            // Restore dest image from the NNF and source image.
            nnf
                .RestoreImage(srcImage, 3, input.Settings.PatchSize)
                .FromLabToRgb()
                .FromRgbToBitmap()
                .SaveTo(@"..\..\restored.png", ImageFormat.Png);

            // Convert the NNF to an image, save and show it
            nnf
                .ToRgbImage()
                .FromRgbToBitmap()
                .SaveTo(@"..\..\nnf.png", ImageFormat.Png)
                .ShowFile();

            Console.WriteLine($"PatchMatchPipeline processing is finished.");
        }
    }
}
