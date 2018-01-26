using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Zavolokas.GdiExtensions;
using Zavolokas.ImageProcessing.PatchMatch;
using Zavolokas.Structures;
using Zavolokas.Utils.Processes;

namespace TargetDestArea
{
    class Program
    {
        static void Main(string[] args)
        {
            const string basePath = "..\\..\\..\\images";
            const string destImageName = "pm1small.png";
            const string srcImageName = "pm2small.png";
            const string targetDestAreaImageName = "pm1small_target.png";
            const string ignoreSrcAreaImageName = "pm2small_ignore.png";

            // this is our input data.
            var destImage = GetLabImage(basePath, destImageName);
            var srcImage = GetLabImage(basePath, srcImageName);

            var targetDestArea = GetArea2D(basePath, targetDestAreaImageName);
            var ignoreSrcArea = GetArea2D(basePath, ignoreSrcAreaImageName);

            var map = new Area2DMapBuilder()
                .InitNewMap(Area2D.Create(0, 0, destImage.Width, destImage.Height), Area2D.Create(0, 0, srcImage.Width, srcImage.Height))
                .SetIgnoredSourcedArea(ignoreSrcArea)
                .ReduceDestArea(targetDestArea)
                .Build();

            var settings = new PatchMatchSettings {PatchSize = 5};
            var patchMatchNnfBuilder = new PatchMatchNnfBuilder();
            var nnf = new Nnf(destImage.Width, destImage.Height, srcImage.Width, srcImage.Height, settings.PatchSize);
            var calculator = ImagePatchDistance.Cie76;

            // Create the nnf the images
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
