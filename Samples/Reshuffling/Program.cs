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
            const string basePath = "..\\..\\..\\images";

            ZsImage srcImage;
            using (var bitmap = new Bitmap(Path.Combine(basePath, "t009.jpg")))
            {
                srcImage = bitmap.ToArgbImage();
            }
            ZsImage destImage = srcImage.Clone();
            var w = destImage.Width;
            var h = destImage.Height;

            Area2D srcMarkup;
            using (var bitmap = new Bitmap(Path.Combine(basePath, "m009.png")))
            {
                srcMarkup = bitmap.ToArea();
            }

            var destMarkup = srcMarkup.Translate(-300, -30);
            //srcMarkup = destMarkup.Translate(-200, 0);


            destImage
                .CopyFromImage(destMarkup, destImage, srcMarkup);
            //.CopyFromImage(srcMarkup.Translate(-300, 30), destImage, srcMarkup);
            destImage.FromArgbToBitmap()
                .SaveTo("..\\..\\target.png", ImageFormat.Png);

            //var markupImage = destMarkup
            //    .Dilation(5)
            //    .Intersect(Area2D.Create(0, 0, w, h))
            //    .ToBitmap(Color.Red, w, h)
            //    .ToArgbImage();

            //markupImage
            //    .FromArgbToRgb(new[] { 1.0, 1.0, 0 })
            //    //.FromArgbToBitmap()
            //    .FromRgbToBitmap()
            //    .SaveTo("..\\..\\initDestMrkp.png", ImageFormat.Png);

            byte patchSize = 5;

            // Prepage setting for the PM algorithm
            var settings = new PatchMatchSettings
            {
                PatchSize = patchSize,
                IterationsAmount = 2
            };

            var calculator = ImagePatchDistance.Cie76;

            // Init an nnf
            var nnf = new Nnf(destImage.Width, destImage.Height, srcImage.Width, srcImage.Height, patchSize);
            var nnfr = new Nnf(srcImage.Width, srcImage.Height, destImage.Width, destImage.Height, patchSize);


            // TODO: we restore the image from the NNF
            // the NNF was built based on the:
            // 1. dest - src - init NNF is scaled from prev
            // 2. restored - src - 
            // Real dest has too many details

            srcImage.FromArgbToRgb(new[] { 1.0, 1.0, 1.0 })
                .FromRgbToLab();

            destImage.FromArgbToRgb(new[] { 1.0, 1.0, 1.0 })
                .FromRgbToLab();

            var destImageArea = Area2D.Create(0, 0, destImage.Width, destImage.Height);
            var srcImageArea = Area2D.Create(0, 0, srcImage.Width, srcImage.Height);

            Area2DMap map = new Area2DMapBuilder()
                        .InitNewMap(
                            //destArea,
                            destImageArea,
                            Area2D.Create(0, 0, srcImage.Width, srcImage.Height))
                        .Build();

            // The scaling of the NNF from the small images to the bigger ones.
            //nnf = PatchMatchNnfBuilder.ScaleNnf2X(nnf, map, destImage, srcImage, destImageArea, calculator, settings);

            //destImage = nnf.RestoreImage(srcImage, 3, patchSize);

            destImage
                .Clone()
                .FromLabToRgb()
                .ScaleTo(w, h)
                .FromRgbToBitmap()
                .SaveTo($"..\\..\\dest.png", ImageFormat.Png);

            var patchMatchNnfBuilder = new PatchMatchNnfBuilder();

            patchMatchNnfBuilder.RunRandomNnfInitIteration(nnf, destImage, srcImage, settings, calculator, map, destImageArea);
            patchMatchNnfBuilder.RunRandomNnfInitIteration(nnfr, srcImage, destImage, settings, calculator, map, srcImageArea);

            for (int j = 0; j < 3; j++)
            {
                patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Forward, settings, calculator, map, destImageArea);
                Console.WriteLine($"\tIteration {j * 2}");
                patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Backward, settings, calculator, map, destImageArea);
                Console.WriteLine($"\tIteration {j * 2 + 1}");
            }

            for (int j = 0; j < 3; j++)
            {
                patchMatchNnfBuilder.RunBuildNnfIteration(nnfr, srcImage, destImage, NeighboursCheckDirection.Forward, settings, calculator, map, srcImageArea);
                Console.WriteLine($"\tIteration {j * 2}");
                patchMatchNnfBuilder.RunBuildNnfIteration(nnfr, srcImage, destImage, NeighboursCheckDirection.Backward, settings, calculator, map, srcImageArea);
                Console.WriteLine($"\tIteration {j * 2 + 1}");
            }

            nnf.ToRgbImage()
                .ScaleTo(w, h)
                .FromRgbToBitmap()
                .SaveTo($"..\\..\\nnf.png", ImageFormat.Png);

            nnf.RestoreImage(srcImage, 3, patchSize)
                .FromLabToRgb()
                .ScaleTo(w, h)
                .FromRgbToBitmap()
                .SaveTo($"..\\..\\restored.png", ImageFormat.Png);

            nnfr.ToRgbImage()
                .ScaleTo(w, h)
                .FromRgbToBitmap()
                .SaveTo($"..\\..\\nnfr.png", ImageFormat.Png);

            nnfr.RestoreImage(srcImage, 3, patchSize)
                .FromLabToRgb()
                .ScaleTo(w, h)
                .FromRgbToBitmap()
                .SaveTo($"..\\..\\restoredr.png", ImageFormat.Png);

            //destImage.FromArgbToBitmap()
            //    .SaveTo("..\\..\\output.png", ImageFormat.Png)
            //    .ShowFile();
        }
    }
}
