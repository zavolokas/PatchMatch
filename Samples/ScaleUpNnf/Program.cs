using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using Zavolokas.GdiExtensions;
using Zavolokas.ImageProcessing.PatchMatch;
using Zavolokas.Structures;
using Zavolokas.Utils.Processes;

namespace ScaleUpNnf
{
    class Program
    {
        static void Main(string[] args)
        {
            const string basePath = "..\\..\\..\\images";
            const int patchSize = 5;

            var srcImageName = "pm1.png";
            var destImageName = "pm2.png";

            // Prepare 2 source images - one small and another 2x bigger
            var srcBitmap = new Bitmap(Path.Combine(basePath, srcImageName));
            var srcImage = srcBitmap
                .ToRgbImage()
                .FromRgbToLab();

            var srcSmallBitmap = srcBitmap.CloneWithScaleTo(srcBitmap.Width / 2, srcBitmap.Height / 2, InterpolationMode.Default);
            var srcSmallImage = srcSmallBitmap
                .ToRgbImage()
                .FromRgbToLab();

            srcSmallBitmap.Dispose();
            srcBitmap.Dispose();

            // Prepare 2 destination images - one small and another 2x bigger
            var destBitmap = new Bitmap(Path.Combine(basePath, destImageName));
            var destImage = destBitmap
                .ToRgbImage()
                .FromRgbToLab();
            var destImagePixelsArea = Area2D.Create(0, 0, destImage.Width, destImage.Height);

            var destSmallBitmap = destBitmap.CloneWithScaleTo(destBitmap.Width / 2, destBitmap.Height / 2, InterpolationMode.Default);
            var destSmallImage = destSmallBitmap
                .ToRgbImage()
                .FromRgbToLab();
            var destSmallImagePixelsArea = Area2D.Create(0, 0, destSmallImage.Width, destSmallImage.Height);

            destBitmap.Dispose();
            destSmallBitmap.Dispose();

            // Init an nnf
            var nnf = new Nnf(destSmallImage.Width, destSmallImage.Height, srcSmallImage.Width, srcSmallImage.Height, patchSize);

            // Create a mapping of the areas on the dest and source areas.
            var map = new Area2DMapBuilder()
                .InitNewMap(
                    Area2D.Create(0, 0, destSmallImage.Width, destSmallImage.Height),
                    Area2D.Create(0, 0, srcSmallImage.Width, srcSmallImage.Height))
                .Build();

            // Create a mapping of the areas on the dest and source areas.
            var nextLevelMap = new Area2DMapBuilder()
                .InitNewMap(
                    Area2D.Create(0, 0, destImage.Width, destImage.Height),
                    Area2D.Create(0, 0, srcImage.Width, srcImage.Height))
                .Build();

            // Prepage setting for the PM algorithm
            var settings = new PatchMatchSettings
            {
                PatchSize = patchSize,
                IterationsAmount = 2
            };

            var calculator = ImagePatchDistance.Cie76;
            var patchMatchNnfBuilder = new PatchMatchNnfBuilder();

            // Create the nnf for the small variant of the images
            // with a couple of iterations.
            patchMatchNnfBuilder.RunRandomNnfInitIteration(nnf, destImage, srcImage, settings, calculator, map, destImagePixelsArea);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Forward, settings, calculator, map, destImagePixelsArea);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Backward, settings, calculator, map, destImagePixelsArea);

            Nnf scaledNnf = null;

            // The scaling of the NNF from the small images to the bigger ones.
            scaledNnf = nnf.CloneAndScale2XWithUpdate(destImage, srcImage, settings, nextLevelMap, calculator, destImagePixelsArea);

            // Prepare results, save and show them
            scaledNnf
                .RestoreImage(srcImage, 3, patchSize)
                .FromLabToRgb()
                .FromRgbToBitmap()
                .SaveTo(@"..\..\l2r.png", ImageFormat.Png);

            scaledNnf
                .ToRgbImage()
                .FromRgbToBitmap()
                .SaveTo(@"..\..\l2n.png", ImageFormat.Png)
                .ShowFile();

            Console.WriteLine($"NnfScaleUp processing is finished.");
        }
    }
}
