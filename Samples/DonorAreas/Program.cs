using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Zavolokas.GdiExtensions;
using Zavolokas.ImageProcessing.PatchMatch;
using Zavolokas.Structures;
using Zavolokas.Utils.Processes;

namespace DonorAreas
{
    class Program
    {
        static void Main(string[] args)
        {
            var sw = new Stopwatch();
            sw.Start();

            const string basePath = "..\\..\\..\\images";

            var destImageName = "pm1.png";
            var srcImageName = "pm2.png";

            // this is our input data.
            var destBitmap = new Bitmap(Path.Combine(basePath, destImageName));
            var destImage = destBitmap
                .ToRgbImage()
                .FromRgbToLab();
            destBitmap.Dispose();

            var srcBitmap = new Bitmap(Path.Combine(basePath, srcImageName));
            var srcImage = srcBitmap
                .ToRgbImage()
                .FromRgbToLab();
            srcBitmap.Dispose();

            //var xw = destImage.Width / 2;
            //var xh = destImage.Height / 2;

            var map = new Area2DMapBuilder()
                .InitNewMap(Area2D.Create(0, 0, destImage.Width, destImage.Height), Area2D.Create(0, 0, srcImage.Width, srcImage.Height))
                .AddAssociatedAreas(Area2D.Create(0, 0, destImage.Width, destImage.Height), Area2D.Create(600, 040, 100, 100))
                //.AddAssociatedAreas(Area2D.Create(0, 0, destImage.Width, destImage.Height), Area2D.Create(0, 0, 600, 400))
                //.ReduceDestArea(Area2D.Create(xw / 2, xh / 2, xw, xh))
                .Build();

            byte patchSize = 5;
            var nnf = new Nnf(destImage.Width, destImage.Height, srcImage.Width, srcImage.Height, patchSize);
            // Prepage setting for the PM algorithm
            var settings = new PatchMatchSettings
            {
                PatchSize = patchSize,
                IterationsAmount = 2
            };
            var calculator = ImagePatchDistance.Cie76;

            var destImagePixelsArea = Area2D.Create(0, 0, destImage.Width, destImage.Height);
            var patchMatchNnfBuilder = new PatchMatchNnfBuilder();

            // Create the nnf for the small variant of the images
            // with a couple of iterations.
            patchMatchNnfBuilder.RunRandomNnfInitIteration(nnf, destImage, srcImage, settings, calculator, map, destImagePixelsArea);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Forward, settings, calculator, map, destImagePixelsArea);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Backward, settings, calculator, map, destImagePixelsArea);

            // Restore dest image from the NNF and source image.
            nnf
                .RestoreImage(srcImage, 3, patchSize)
                .FromLabToRgb()
                .FromRgbToBitmap()
                .SaveTo(@"..\..\restored.png", ImageFormat.Png);

            // Convert the NNF to an image, save and show it
            nnf
                .ToRgbImage()
                .FromRgbToBitmap()
                .SaveTo(@"..\..\nnf.png", ImageFormat.Png)
                .ShowFile();

            sw.Stop();

            Console.WriteLine($"Elapsed time: {sw.Elapsed}");
            Console.WriteLine($"PatchMatchPipeline processing is finished.");
        }
    }
}
