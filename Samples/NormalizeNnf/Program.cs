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
            const string basePath = "..\\..\\..\\..\\images";
            const int patchSize = 5;

            var srcImageName = "t009.jpg";

            // Prepare images
            var srcImage = GetLabImage(basePath, srcImageName);
            var destImage = GetLabImage(basePath, srcImageName);

            var ignoreArea = GetArea2D(basePath, "m009.png");
            var destArea = ignoreArea.Dilation(patchSize * 2 + 1);

            // Init an nnf
            var nnf = new Nnf(destImage.Width, destImage.Height, srcImage.Width, srcImage.Height, patchSize);

            // Create a mapping of the areas on the dest and source areas.
            var imageArea = Area2D.Create(0, 0, srcImage.Width, srcImage.Height);
            var map = new Area2DMapBuilder()
                .InitNewMap(imageArea, imageArea)
                .SetIgnoredSourcedArea(ignoreArea)
                .Build();

            // Prepage setting for the PM algorithm
            var settings = new PatchMatchSettings { PatchSize = patchSize };
            var calculator = ImagePatchDistance.Cie76;
            var patchMatchNnfBuilder = new PatchMatchNnfBuilder();

            // Create the nnf for the image(while ignoring some area) 
            // with a couple of iterations.
            patchMatchNnfBuilder.RunRandomNnfInitIteration(nnf, destImage, srcImage, settings, calculator, map);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Forward, settings, calculator, map);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Backward, settings, calculator, map);

            // Create a mapping for the area that is a bit bigger 
            // then ignored area.
            map = new Area2DMapBuilder()
                .InitNewMap(imageArea, imageArea)
                .ReduceDestArea(destArea)
                .SetIgnoredSourcedArea(ignoreArea)
                .Build();

            patchMatchNnfBuilder.RunRandomNnfInitIteration(nnf, destImage, srcImage, settings, calculator, map);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Forward, settings, calculator, map);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Backward, settings, calculator, map);

            string fileName1 = @"..\..\..\nnf1_pure.png";
            nnf
                .ToRgbImage()
                .FromRgbToBitmap()
                .SaveTo(fileName1, ImageFormat.Png);

            // Normalize the NNF in the ignored area.
            nnf.Normalize(ignoreArea);

            // Prepare results, save and show them
            string fileName2 = @"..\..\..\nnf2_normalized.png";
            nnf
                .ToRgbImage()
                .FromRgbToBitmap()
                .SaveTo(fileName2, ImageFormat.Png)
                .ShowFile();

            Console.WriteLine($"Nnf normalization is finished.");
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
