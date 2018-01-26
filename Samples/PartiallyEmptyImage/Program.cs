using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Zavolokas.GdiExtensions;
using Zavolokas.ImageProcessing.PatchMatch;
using Zavolokas.Structures;
using Zavolokas.Utils.Processes;

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
            var destImage = GetLabImage(basePath, destImageName);
            var srcImage = GetLabImage(basePath, srcImageName);
            var destArea = Area2D.Create(0, 0, destImage.Width, destImage.Height);
            var srcArea = Area2D.Create(0, 0, srcImage.Width, srcImage.Height);

            var emptyArea = GetArea2D(basePath, emptyAreaImageName);
            var destPixelsArea = destArea.Substract(emptyArea);

            var map = new Area2DMapBuilder()
                .InitNewMap(destArea, srcArea)
                .Build();

            const byte patchSize = 5;
            var settings = new PatchMatchSettings
            {
                PatchSize = patchSize,
            };

            var nnf = new Nnf(destImage.Width, destImage.Height, srcImage.Width, srcImage.Height, patchSize);
            // Prepage setting for the PM algorithm
            var calculator = ImagePatchDistance.Cie76;

            var patchMatchNnfBuilder = new PatchMatchNnfBuilder();

            // Create the nnf for the small variant of the images
            // with a couple of iterations.
            patchMatchNnfBuilder.RunRandomNnfInitIteration(nnf, destImage, srcImage, settings, calculator, map, destPixelsArea);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Forward, settings, calculator, map, destPixelsArea);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Backward, settings, calculator, map, destPixelsArea);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Forward, settings, calculator, map, destPixelsArea);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Backward, settings, calculator, map, destPixelsArea);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Forward, settings, calculator, map, destPixelsArea);

            // Restore dest image from the NNF and source image.
            nnf
                .RestoreImage(srcImage, 3, settings.PatchSize)
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
