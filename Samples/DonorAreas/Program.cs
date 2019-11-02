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

            const string basePath = "..\\..\\..\\..\\images";

            var destImageName = "pm1.png";
            var srcImageName = "pm2.png";
            var destTargetImageName = "dd1.png";

            // this is our input data.
            var destImage = GetLabImage(basePath, destImageName);
            var srcImage = GetLabImage(basePath, srcImageName);

            var destTargetArea1 = GetArea2D(basePath, destTargetImageName);
            var srcArea1 = GetArea2D(basePath, "sd1.png");
            var destArea2 = GetArea2D(basePath, "dd2.png");
            var srcArea2 = GetArea2D(basePath, "sd2.png");
            var destArea = Area2D.Create(0, 0, destImage.Width, destImage.Height);
            var srcArea = Area2D.Create(0, 0, srcImage.Width, srcImage.Height);

            var map = new Area2DMapBuilder()
                .InitNewMap(destArea, srcArea)
                .AddAssociatedAreas(destTargetArea1, srcArea1)
                .AddAssociatedAreas(destArea2, srcArea2)
                .Build();

            const byte patchSize = 5;
            var nnf = new Nnf(destImage.Width, destImage.Height, srcImage.Width, srcImage.Height, patchSize);
            // Prepage setting for the PM algorithm
            var settings = new PatchMatchSettings
            {
                PatchSize = patchSize
            };
            var calculator = ImagePatchDistance.Cie76;

            var patchMatchNnfBuilder = new PatchMatchNnfBuilder();

            // Create the nnf for the small variant of the images
            // with a couple of iterations.
            patchMatchNnfBuilder.RunRandomNnfInitIteration(nnf, destImage, srcImage, settings, calculator, map);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Forward, settings, calculator, map);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Backward, settings, calculator, map);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Forward, settings, calculator, map);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Backward, settings, calculator, map);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Forward, settings, calculator, map);

            // Restore dest image from the NNF and source image.
            nnf
                .RestoreImage(srcImage, 3, patchSize)
                .FromLabToRgb()
                .FromRgbToBitmap()
                .SaveTo(@"..\..\..\restored.png", ImageFormat.Png);

            // Convert the NNF to an image, save and show it
            nnf
                .ToRgbImage()
                .FromRgbToBitmap()
                .SaveTo(@"..\..\..\nnf.png", ImageFormat.Png)
                .ShowFile();

            sw.Stop();

            Console.WriteLine($"Elapsed time: {sw.Elapsed}");
            Console.WriteLine($"PatchMatchPipeline processing is finished.");
        }

        private static ZsImage GetLabImage(string basePath, string fileName)
        {
            ZsImage result;
            var destFilePath = Path.Combine(basePath, fileName);
            using (var destBitmap = new System.Drawing.Bitmap(destFilePath))
            {
                result = destBitmap.ToRgbImage()
                    .FromRgbToLab();
            }
            return result;
        }

        private static Area2D GetArea2D(string basePath, string fileName)
        {
            Area2D result;
            var destFilePath = Path.Combine(basePath, fileName);
            using (var destBitmap = new Bitmap(destFilePath))
            {
                result = destBitmap.ToArea();
            }
            return result;
        }
    }
}
