using System;
using Zavolokas.ImageProcessing.PatchMatch;
using Zavolokas.Structures;

namespace Zavolokas.ImageProcessing.Parallel.PatchMatch
{
    public class PmData
    {
        private ZsImage _destImage;
        private ZsImage _srcImage;
        private PatchMatchSettings _settings;
        private Area2DMap _map;
        private Nnf _nnf;
        public Area2D DestImagePixelsArea;

        public PmData(ZsImage destImage, ZsImage srcImage)
        {
            if (destImage == null)
                throw new ArgumentNullException(nameof(destImage));

            if (srcImage == null)
                throw new ArgumentNullException(nameof(srcImage));

            DestImage = destImage;
            SrcImage = srcImage;

            Nnf = new Nnf(destImage.Width, destImage.Height, srcImage.Width, srcImage.Height);
            Settings = new PatchMatchSettings();

            var destImageArea = Area2D.Create(0, 0, destImage.Width, destImage.Height);
            DestImagePixelsArea = destImageArea;

            var mapBuilder = new Area2DMapBuilder();
            mapBuilder.InitNewMap(
                destImageArea,
                Area2D.Create(0, 0, srcImage.Width, srcImage.Height));
            Map = mapBuilder.Build();
        }

        public PmData(ZsImage destImage, ZsImage srcImage, Area2DMap map)
            : this(destImage, srcImage, new Nnf(destImage.Width, destImage.Height, srcImage.Width, srcImage.Height), map)
        {
        }

        public PmData(ZsImage destImage, ZsImage srcImage, Nnf nnf, Area2DMap map)
        {
            if (destImage == null)
                throw new ArgumentNullException(nameof(destImage));

            if (srcImage == null)
                throw new ArgumentNullException(nameof(srcImage));

            if (map == null)
                throw new ArgumentNullException(nameof(map));

            if (nnf == null)
                throw new ArgumentNullException(nameof(nnf));

            Map = map;
            Nnf = nnf;
            DestImage = destImage;
            SrcImage = srcImage;
            DestImagePixelsArea = Area2D.Create(0, 0, destImage.Width, destImage.Height);

            Settings = new PatchMatchSettings();
        }

        public Nnf Nnf
        {
            get { return _nnf; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                _nnf = value;
            }
        }

        public Area2DMap Map
        {
            get { return _map; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                _map = value;
            }
        }

        public PatchMatchSettings Settings
        {
            get { return _settings; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                _settings = value;
            }
        }

        public ZsImage DestImage
        {
            get { return _destImage; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                _destImage = value;
            }
        }

        public ZsImage SrcImage
        {
            get { return _srcImage; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                _srcImage = value;
            }
        }
    }
}