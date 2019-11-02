using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Zavolokas.GdiExtensions;
using Zavolokas.ImageProcessing.PatchMatch;
using Zavolokas.Structures;
using Zavolokas.Utils.Processes;

namespace IgnoreArea
{
    class Program
    {
        static void Main(string[] args)
        {
            const string basePath = "..\\..\\..\\..\\images";

            // this is our input data.
            var destImage = GetLabImage(basePath, "pm1.png");
            var srcImage = GetLabImage(basePath, "pm2.png");
            var ignoreArea = GetArea2D(basePath, "pm2_ignore.png");

            var map = new Area2DMapBuilder()
                .InitNewMap(Area2D.Create(0, 0, destImage.Width, destImage.Height), Area2D.Create(0, 0, srcImage.Width, srcImage.Height))
                .SetIgnoredSourcedArea(ignoreArea)
                .Build();

            // Prepage setting for the PM algorithm
            const byte patchSize = 5;
            var settings = new PatchMatchSettings { PatchSize = patchSize };
            var patchMatchNnfBuilder = new PatchMatchNnfBuilder();
            
            var nnf = new Nnf(destImage.Width, destImage.Height, srcImage.Width, srcImage.Height, patchSize);
            var calculator = ImagePatchDistance.Cie76;

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
                .RestoreImage(srcImage, 3, settings.PatchSize)
                .FromLabToRgb()
                .FromRgbToBitmap()
                .SaveTo(@"..\..\..\restored.png", ImageFormat.Png);

            // Convert the NNF to an image, save and show it
            nnf
                .ToRgbImage()
                .FromRgbToBitmap()
                .SaveTo(@"..\..\..\nnf.png", ImageFormat.Png)
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
