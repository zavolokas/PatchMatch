using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Zavolokas.GdiExtensions;
using Zavolokas.ImageProcessing.PatchMatch;
using Zavolokas.Structures;
using Zavolokas.Utils.Processes;

namespace MergeNnfs
{
    class Program
    {
        static void Main(string[] args)
        {
            const string basePath = "..\\..\\..\\..\\images";

            var destImageName = "pm1.png";
            var srcImageName = "pm2.png";
            var destArea1ImageName = "pm1_target1.png";
            var destArea2ImageName = "pm1_target2.png";

            // this is our input data.
            var destImage = GetLabImage(basePath, destImageName);
            var srcImage = GetLabImage(basePath, srcImageName);

            var destArea1 = GetArea2D(basePath, destArea1ImageName);
            var destArea2 = GetArea2D(basePath, destArea2ImageName);
            var srcArea = Area2D.Create(0, 0, srcImage.Width, srcImage.Height);

            var map1 = new Area2DMapBuilder()
                .InitNewMap(destArea1, srcArea)
                .Build();

            var map2 = new Area2DMapBuilder()
                .InitNewMap(destArea2, srcArea)
                .Build();

            var settings = new PatchMatchSettings { PatchSize = 5 };
            var patchMatchNnfBuilder = new PatchMatchNnfBuilder();
            var calculator = ImagePatchDistance.Cie76;

            // Create nnf for the images
            // with a couple of iterations.
            var nnf1 = new Nnf(destImage.Width, destImage.Height, srcImage.Width, srcImage.Height, settings.PatchSize);
            patchMatchNnfBuilder.RunRandomNnfInitIteration(nnf1, destImage, srcImage, settings, calculator, map1);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf1, destImage, srcImage, NeighboursCheckDirection.Forward, settings, calculator, map1);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf1, destImage, srcImage, NeighboursCheckDirection.Backward, settings, calculator, map1);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf1, destImage, srcImage, NeighboursCheckDirection.Forward, settings, calculator, map1);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf1, destImage, srcImage, NeighboursCheckDirection.Backward, settings, calculator, map1);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf1, destImage, srcImage, NeighboursCheckDirection.Forward, settings, calculator, map1);

            // Create the nnf for the images
            // with a couple of iterations.
            var nnf2 = new Nnf(destImage.Width, destImage.Height, srcImage.Width, srcImage.Height, settings.PatchSize);
            patchMatchNnfBuilder.RunRandomNnfInitIteration(nnf2, destImage, srcImage, settings, calculator, map2);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf2, destImage, srcImage, NeighboursCheckDirection.Forward, settings, calculator, map2);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf2, destImage, srcImage, NeighboursCheckDirection.Backward, settings, calculator, map2);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf2, destImage, srcImage, NeighboursCheckDirection.Forward, settings, calculator, map2);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf2, destImage, srcImage, NeighboursCheckDirection.Backward, settings, calculator, map2);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf2, destImage, srcImage, NeighboursCheckDirection.Forward, settings, calculator, map2);


            // Show the built NNFs and restored images
            nnf2
                .RestoreImage(srcImage, 3, settings.PatchSize)
                .FromLabToRgb()
                .FromRgbToBitmap()
                .SaveTo(@"..\..\..\restored1.png", ImageFormat.Png);

            nnf1
                .RestoreImage(srcImage, 3, settings.PatchSize)
                .FromLabToRgb()
                .FromRgbToBitmap()
                .SaveTo(@"..\..\..\restored2.png", ImageFormat.Png);

            nnf1
                .ToRgbImage()
                .FromRgbToBitmap()
                .SaveTo(@"..\..\..\nnf1.png", ImageFormat.Png);

            nnf2
                .ToRgbImage()
                .FromRgbToBitmap()
                .SaveTo(@"..\..\..\nnf2.png", ImageFormat.Png);


            // Let's now merge the built NNFs and try to restore an image
            nnf2.Merge(nnf1, map2, map1);

            nnf2
                .RestoreImage(srcImage, 3, settings.PatchSize)
                .FromLabToRgb()
                .FromRgbToBitmap()
                .SaveTo(@"..\..\..\restored_whole.png", ImageFormat.Png);

            nnf2
                .ToRgbImage()
                .FromRgbToBitmap()
                .SaveTo(@"..\..\..\merged_nnf.png", ImageFormat.Png)
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
