using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Zavolokas.GdiExtensions;
using Zavolokas.ImageProcessing.PatchMatch;
using Zavolokas.Structures;

namespace Reshuffling
{
    class Program
    {
        static void Main(string[] args)
        {
            const string basePath = "..\\..\\..\\..\\images";

            ZsImage srcImage = GetArgbImage(basePath, "t009.jpg");
            ZsImage destImage = srcImage.Clone();

            var srcMarkup = GetArea2D(basePath, "m009.png");
            var destMarkup = srcMarkup.Translate(-300, -30);

            destImage.CopyFromImage(destMarkup, destImage, srcMarkup);
            destImage.FromArgbToBitmap()
                .SaveTo("..\\..\\..\\target.png", ImageFormat.Png);

            // Prepage setting for the PM algorithm
            const byte patchSize = 5;
            var settings = new PatchMatchSettings { PatchSize = patchSize };

            // Init an nnf
            var nnf = new Nnf(destImage.Width, destImage.Height, srcImage.Width, srcImage.Height, patchSize);

            srcImage.FromArgbToRgb(new[] { 1.0, 1.0, 1.0 })
                .FromRgbToLab();

            destImage.FromArgbToRgb(new[] { 1.0, 1.0, 1.0 })
                .FromRgbToLab();

            destImage
                .Clone()
                .FromLabToRgb()
                .FromRgbToBitmap()
                .SaveTo($"..\\..\\..\\dest.png", ImageFormat.Png);

            var patchMatchNnfBuilder = new PatchMatchNnfBuilder();

            patchMatchNnfBuilder.RunRandomNnfInitIteration(nnf, destImage, srcImage, settings);

            for (int j = 0; j < 3; j++)
            {
                patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Forward, settings);
                Console.WriteLine($"\tIteration {j * 2}");
                patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Backward, settings);
                Console.WriteLine($"\tIteration {j * 2 + 1}");
            }

            nnf.ToRgbImage()
                .FromRgbToBitmap()
                .SaveTo($"..\\..\\..\\nnf.png", ImageFormat.Png);

            nnf.RestoreImage(srcImage, 3, patchSize)
                .FromLabToRgb()
                .FromRgbToBitmap()
                .SaveTo($"..\\..\\..\\restored.png", ImageFormat.Png);
        }

        private static ZsImage GetArgbImage(string basePath, string fileName)
        {
            ZsImage result;
            var destFilePath = Path.Combine(basePath, fileName);
            using (var destBitmap = new System.Drawing.Bitmap(destFilePath))
            {
                result = destBitmap.ToArgbImage();
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
