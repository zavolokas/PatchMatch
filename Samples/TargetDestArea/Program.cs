using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Zavolokas.GdiExtensions;
using Zavolokas.ImageProcessing.Parallel.PatchMatch;
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

            var destImageName = "pm1small.png";
            var srcImageName = "pm2small.png";
            var targetDestAreaImageName = "pm1small_target.png";
            var ignoreSrcAreaImageName = "pm2small_ignore.png";

            // this is our input data.
            // this is our input data.
            var destBitmap = new Bitmap(Path.Combine(basePath, destImageName));
            var destImage = destBitmap
                .ToRgbImage()
                .FromRgbToLab();
            destBitmap.Dispose();

            var srcBitmap = new Bitmap(Path.Combine(basePath, srcImageName));
            var srcImage = srcBitmap
                .ToRgbImage()
                .FromRgbToLab();
            srcBitmap.Dispose();

            var targetDestAreaBitmap = new Bitmap(Path.Combine(basePath, targetDestAreaImageName));
            var targetDestArea = targetDestAreaBitmap.ToArea();
            targetDestAreaBitmap.Dispose();

            var ignoreSrcAreaBitmap = new Bitmap(Path.Combine(basePath, ignoreSrcAreaImageName));
            var ignoreSrcArea = ignoreSrcAreaBitmap.ToArea();
            ignoreSrcAreaBitmap.Dispose();

            var map = new Area2DMapBuilder()
                .InitNewMap(Area2D.Create(0, 0, destImage.Width, destImage.Height), Area2D.Create(0, 0, srcImage.Width, srcImage.Height))
                .SetIgnoredSourcedArea(ignoreSrcArea)
                .ReduceDestArea(targetDestArea)
                .Build();

            var input = new PmData(destImage, srcImage, map)
            {
                Settings =
                {
                    PatchSize = 5,
                    IterationsAmount = 2
                }
            };

            var nnfPipeline = new PatchMatchPipeline(ImagePatchDistance.Cie76);
            nnfPipeline.SetInput(input);
            var nnf = nnfPipeline.Process()
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
