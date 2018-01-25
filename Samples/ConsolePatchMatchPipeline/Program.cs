using System;
using System.Drawing.Imaging;
using System.IO;
using Zavolokas.GdiExtensions;
using Zavolokas.ImageProcessing.Parallel.PatchMatch;
using Zavolokas.ImageProcessing.PatchMatch;
using Zavolokas.Structures;
using Zavolokas.Utils.Processes;

namespace ConsolePatchMatchPipeline
{
    public class Program
    {
        public static void Main()
        {
            const string basePath = "..\\..\\..\\images";

            var destImageName = "pm1.png";
            var srcImageName = "pm2.png";

            // this is our input data.
            var destImage = GetLabImage(basePath, destImageName);
            var srcImage = GetLabImage(basePath, srcImageName);

            var input = new PmData(destImage, srcImage)
            {
                Settings =
                {
                    PatchSize = 5,
                    IterationsAmount = 2
                }
            };

            var patchMatchNnfBuilder = new PatchMatchNnfBuilder();

            var pipeline = new PatchMatchPipeline(patchMatchNnfBuilder, ImagePatchDistance.Cie2000);
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
