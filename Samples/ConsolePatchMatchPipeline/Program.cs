using System;
using System.Drawing.Imaging;
using System.IO;
using Zavolokas;
using Zavolokas.GdiExtensions;
using Zavolokas.ImageProcessing.Parallel.PatchMatch;
using Zavolokas.ImageProcessing.PatchMatch;
using Zavolokas.Structures;

namespace ConsolePatchMatchPipeline
{
    class Program
    {
        static void Main(string[] args)
        {
            const string basePath = "..\\..\\..\\images";

            var destImageName = "pm1.png";
            var srcImageName = "pm2.png";

            // this is our input data.
            var destImage = GetLabImage(basePath, destImageName);
            var srcImage = GetLabImage(basePath, srcImageName);
            var map = BuildMapping(destImage.Width, destImage.Height, srcImage.Width, srcImage.Height);

            var input = new PmData(destImage, srcImage, map);
            input.Settings.PatchSize = 5;
            input.Settings.IterationsAmount = 2;

            var pipeline = new PatchMatchPipeline(ImagePatchDistance.Cie2000);
            pipeline.SetInput(input);

            var data = pipeline.Process()
                .Output[0];

            var nnf = data.Nnf;

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

        private static Area2DMap BuildMapping(int destWidth, int destHeight, int srcWidth, int srcHeight)
        {
            var mapBuilder = new Area2DMapBuilder();
            mapBuilder.InitNewMap(
                Area2D.Create(0, 0, destWidth, destHeight),
                Area2D.Create(0, 0, srcWidth, srcHeight));

            //var xw = 50;// destImage.Width / 2;
            //var xh = 50;//destImage.Height / 2;
            //mapBuilder.ReduceDestArea(Area2D.Create(200, 200, xw, xh));
            ////.SetIgnoredSourcedArea();

            var map = mapBuilder.Build();
            return map;
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
    }
}
