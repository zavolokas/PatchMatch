using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Zavolokas.GdiExtensions;
using Zavolokas.ImageProcessing.Parallel.PatchMatch;
using Zavolokas.ImageProcessing.PatchMatch;
using Zavolokas.Structures;
using Zavolokas.Utils.Processes;

namespace MergeNnfs
{
    class Program
    {
        static void Main(string[] args)
        {
            const string basePath = "..\\..\\..\\images";

            var destImageName = "pm1.png";
            var srcImageName = "pm2.png";
            var destAreaImageName = "pm1_target1.png";
            var srcAreaImageName = "pm1_target2.png";

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

            var destAreaBitmap = new Bitmap(Path.Combine(basePath, destAreaImageName));
            var destArea1 = destAreaBitmap.ToArea();
            destAreaBitmap.Dispose();

            var srcAreaBitmap = new Bitmap(Path.Combine(basePath, srcAreaImageName));
            var destArea2 = srcAreaBitmap.ToArea();
            srcAreaBitmap.Dispose();

            var map1 = new Area2DMapBuilder()
                .InitNewMap(destArea1, Area2D.Create(0, 0, srcImage.Width, srcImage.Height))
                .Build();

            var map2 = new Area2DMapBuilder()
                .InitNewMap(destArea2, Area2D.Create(0, 0, srcImage.Width, srcImage.Height))
                .Build();

            var patchDistanceCalculator = ImagePatchDistance.Cie76;

            var input1 = new PmData(destImage, srcImage, map1);
            input1.Settings.PatchSize = 5;
            input1.Settings.IterationsAmount = 2;

            var patchMatchNnfBuilder = new PatchMatchNnfBuilder();

            var nnf1Pipeline = new PatchMatchPipeline(patchMatchNnfBuilder, patchDistanceCalculator);
            nnf1Pipeline.SetInput(input1);

            var nnf1 = nnf1Pipeline.Process()
                .Output[0]
                .Nnf;

            var input2 = new PmData(destImage, srcImage, map2);
            input2.Settings.PatchSize = 5;
            input2.Settings.IterationsAmount = 2;

            var nnf2Pipeline = new PatchMatchPipeline(patchMatchNnfBuilder, patchDistanceCalculator);
            nnf2Pipeline.SetInput(input2);

            var nnf2 = nnf2Pipeline.Process()
                .Output[0]
                .Nnf;

            input1.Nnf = nnf1;
            input2.Nnf = nnf2;

            var mergePipeline = new MergeNnf();
            mergePipeline.SetInput(new[] {input1, input2});

            nnf1
                .RestoreImage(srcImage, 3, input2.Settings.PatchSize)
                .FromLabToRgb()
                .FromRgbToBitmap()
                .SaveTo(@"..\..\restored1.png", ImageFormat.Png);

            nnf2
                .RestoreImage(srcImage, 3, input2.Settings.PatchSize)
                .FromLabToRgb()
                .FromRgbToBitmap()
                .SaveTo(@"..\..\restored2.png", ImageFormat.Png);

            nnf1
                .ToRgbImage()
                .FromRgbToBitmap()
                .SaveTo(@"..\..\nnf1.png", ImageFormat.Png);

            nnf2
                .ToRgbImage()
                .FromRgbToBitmap()
                .SaveTo(@"..\..\nnf2.png", ImageFormat.Png)
                .ShowFile();

            nnf1 = mergePipeline.Process()
                .Output[0]
                .Nnf;

            nnf1
                .RestoreImage(srcImage, 3, input2.Settings.PatchSize)
                .FromLabToRgb()
                .FromRgbToBitmap()
                .SaveTo(@"..\..\restored_whole.png", ImageFormat.Png);

            nnf1
                .ToRgbImage()
                .FromRgbToBitmap()
                .SaveTo(@"..\..\merged_nnf.png", ImageFormat.Png);

            Console.WriteLine($"PatchMatchPipeline processing is finished.");
        }
    }
}
