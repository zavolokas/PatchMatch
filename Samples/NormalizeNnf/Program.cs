using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Zavolokas.GdiExtensions;
using Zavolokas.ImageProcessing.PatchMatch;
using Zavolokas.Structures;
using Zavolokas.Utils.Processes;

namespace NormalizeNnf
{
    class Program
    {
        static void Main(string[] args)
        {
            const string basePath = "..\\..\\..\\images";
            const int patchSize = 5;

            var srcImageName = "t009.jpg";

            // Prepare 2 source images - one small and another 2x bigger
            var srcBitmap = new Bitmap(Path.Combine(basePath, srcImageName));
            var srcImage = srcBitmap
                .ToRgbImage()
                .FromRgbToLab();
            srcBitmap.Dispose();

            var destBitmap = new Bitmap(Path.Combine(basePath, srcImageName));
            var destImage = destBitmap
                .ToRgbImage()
                .FromRgbToLab();
            destBitmap.Dispose();

            var mrkpBitmap = new Bitmap(Path.Combine(basePath, "m009.png"));
            var removeArea = mrkpBitmap.ToArea();
            var destArea = removeArea.Dilation(patchSize * 2 + 1);
            mrkpBitmap.Dispose();

            // Init an nnf
            var nnf = new Nnf(destImage.Width, destImage.Height, srcImage.Width, srcImage.Height, patchSize);

            // Create a mapping of the areas on the dest and source areas.
            var imageArea = Area2D.Create(0, 0, srcImage.Width, srcImage.Height);
            var map = new Area2DMapBuilder()
                .InitNewMap(imageArea, imageArea)
                .SetIgnoredSourcedArea(removeArea)
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
            patchMatchNnfBuilder.RunRandomNnfInitIteration(nnf, destImage, srcImage, settings, calculator, map);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Forward, settings, calculator, map);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Backward, settings, calculator, map);

            // Create a mapping of the areas on the dest and source areas.
            map = new Area2DMapBuilder() 
                .InitNewMap(imageArea, imageArea) 
                .ReduceDestArea(destArea)
                .SetIgnoredSourcedArea(removeArea)
                .Build();

            patchMatchNnfBuilder.RunRandomNnfInitIteration(nnf, destImage, srcImage, settings, calculator, map);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Forward, settings, calculator, map);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Backward, settings, calculator, map);

            string fileName1 = @"..\..\nnf1_pure.png";
            nnf
                .ToRgbImage()
                .FromRgbToBitmap()
                .SaveTo(fileName1, ImageFormat.Png);

            // The scaling of the NNF from the small images to the bigger ones.
            nnf.Normalize(removeArea);

            // Prepare results, save and show them
            string fileName2 = @"..\..\nnf2_normalized.png";
            nnf
                .ToRgbImage()
                .FromRgbToBitmap()
                .SaveTo(fileName2, ImageFormat.Png)
                .ShowFile();

            Console.WriteLine($"Nnf normalization is finished.");
        }
    }
}
