using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Zavolokas;
using Zavolokas.GdiExtensions;
using Zavolokas.ImageProcessing.Parallel.PatchMatch;
using Zavolokas.ImageProcessing.PatchMatch;
using Zavolokas.Structures;

namespace IgnoreArea
{
    class Program
    {
        static void Main(string[] args)
        {
            const string basePath = "..\\..\\..\\images";

            // this is our input data.
            var destBitmap = new Bitmap(Path.Combine(basePath, "pm1.png"));
            var destImage = destBitmap
                .ToRgbImage()
                .FromRgbToLab();
            destBitmap.Dispose();

            var srcBitmap = new Bitmap(Path.Combine(basePath, "pm2.png"));
            var srcImage = srcBitmap
                .ToRgbImage()
                .FromRgbToLab();
            srcBitmap.Dispose();

            var ignoreAreaBitmap = new Bitmap(Path.Combine(basePath, "pm2_ignore.png"));
            var ignoreArea = ignoreAreaBitmap.ToArea();
            ignoreAreaBitmap.Dispose();

            var map = new Area2DMapBuilder()
                .InitNewMap(Area2D.Create(0, 0, destImage.Width, destImage.Height), Area2D.Create(0, 0, srcImage.Width, srcImage.Height))
                .SetIgnoredSourcedArea(ignoreArea)
                .Build();

            var input = new PmData(destImage, srcImage, map);
            input.Settings.PatchSize = 5;
            input.Settings.IterationsAmount = 2;

            var nnfPipeline = new PatchMatchPipeline(ImagePatchDistance.Cie76);
            nnfPipeline.SetInput(input);
            var nnf = nnfPipeline
                .Process()
                .Output[0]
                .Nnf;

            // Restore dest image from the NNF and source image.
            nnf
                .RestoreImage(srcImage, 3, input.Settings.PatchSize)
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
    }
}
