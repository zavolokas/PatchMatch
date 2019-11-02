using System;
using System.Drawing;
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
            const string basePath = "..\\..\\..\\..\\images";
            const string srcImageName = "pm1.png";
            const string destImageName = "pm2.png";
            const int patchSize = 5;

            // Prepare 2 source images - one small and another 2x bigger
            var srcBitmap = new Bitmap(Path.Combine(basePath, srcImageName));
            var srcImage = srcBitmap
                .ToRgbImage()
                .FromRgbToLab();

            var srcSmallBitmap = srcBitmap.CloneWithScaleTo(srcBitmap.Width / 2, srcBitmap.Height / 2);
            var srcSmallImage = srcSmallBitmap
                .ToRgbImage()
                .FromRgbToLab();

            srcSmallBitmap.Dispose();
            srcBitmap.Dispose();

            var destBitmap = new Bitmap(Path.Combine(basePath, destImageName));
            var destImage = destBitmap
                .ToRgbImage()
                .FromRgbToLab();

            var destSmallBitmap = destBitmap.CloneWithScaleTo(destBitmap.Width / 2, destBitmap.Height / 2);
            var destSmallImage = destSmallBitmap
                .ToRgbImage()
                .FromRgbToLab();

            destBitmap.Dispose();
            destSmallBitmap.Dispose();

            // Init an nnf
            var nnf = new Nnf(destSmallImage.Width, destSmallImage.Height, srcSmallImage.Width, srcSmallImage.Height, patchSize);

            // Prepage setting for the PM algorithm
            var settings = new PatchMatchSettings { PatchSize = patchSize };
            var patchMatchNnfBuilder = new PatchMatchNnfBuilder();

            // Create the nnf for the small variant of the images
            // with a couple of iterations.
            patchMatchNnfBuilder.RunRandomNnfInitIteration(nnf, destSmallImage, srcSmallImage, settings);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destSmallImage, srcSmallImage, NeighboursCheckDirection.Backward, settings);
            patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destSmallImage, srcSmallImage, NeighboursCheckDirection.Forward, settings);

            // The scaling of the NNF from the small images to the bigger ones.
            var scaledNnf = nnf.CloneAndScale2XWithUpdate(destImage, srcImage, settings);

            // Prepare results, save and show them
            scaledNnf
                .RestoreImage(srcImage, 3, patchSize)
                .FromLabToRgb()
                .FromRgbToBitmap()
                .SaveTo(@"..\..\..\l2r.png", ImageFormat.Png);

            scaledNnf
                .ToRgbImage()
                .FromRgbToBitmap()
                .SaveTo(@"..\..\..\l2n.png", ImageFormat.Png)
                .ShowFile();

            Console.WriteLine($"NnfScaleUp processing is finished.");
        }
    }
}
