# PatchMatch
[![license](https://img.shields.io/github/license/mashape/apistatus.svg?style=flat-square)]()
[![Build Status](https://travis-ci.org/zavolokas/PatchMatch.svg?branch=master)](https://travis-ci.org/zavolokas/PatchMatch)

Implementation of **PatchMatch** algorithm for .NET.

> PM> Install-Package Zavolokas.ImageProcessing.PatchMatch

## What is PatchMatch?
The core **PatchMatch** algorithm quickly finds correspondences between an image patches(small square regions). The algorithm can be used in various applications such as:
- [inpsinting(object removal from images)](https://github.com/zavolokas/Inpainting)
- [reshuffling or moving contents of images](https://www.youtube.com/watch?v=IPXqWBvbrIQ)
- [retargeting or changing aspect ratios of images](https://people.csail.mit.edu/mrub/papers/retBenchmark.pdf)
- optical flow estimation
- stereo correspondence.

The main advantage of the algorithm is that it is fast. It is based on the observations that some of the good matches can be found randomly and that these results can be propagated to the neighbour areas due to natural coherence of images.

As an output the algorithm provides built **nearest neighbor field (NNF)** of correspondences between patches of two images. 

As an output the algorithm provides built **nearest neighbor field (NNF)** of correspondences between patches of two images. 

As an output the algorithm provides built **nearest neighbor field (NNF)** of correspondences between patches of two images. 

More information can be found in [this scientific publication](http://gfx.cs.princeton.edu/pubs/Barnes_2009_PAR/index.php).

## What is it for?
It can be used in image processing and image editing tools (inpainting, image reshuffling, content aware image resizing etc).

## How to use it?
In a nutshell **PatchMatch** algorithm consists of:
  - random initialization step of NNF
  - a number of search iterations

`PatchMatchNnfBuilder` class defines two corresponding methods:
  - `RunRandomNnfInitIteration`
  - `RunBuildNnfIteration`

Both these methods take as arguments:
  - An instance of an NNF
  - An image to build NNF for (destination)
  - An image that is a source of patches for the destination image
  - Settings that control algorithm execution

## Examples
### Simple NNF

```csharp
var settings = new PatchMatchSettings { PatchSize = 5 };
// Create an NNF
var nnf = new Nnf(destImage.Width, destImage.Height, srcImage.Width, srcImage.Height, settings.PatchSize);

var patchMatchNnfBuilder = new PatchMatchNnfBuilder();

// NNF initialization step
patchMatchNnfBuilder.RunRandomNnfInitIteration(nnf, destImage, srcImage, settings);

// Few iterations of NNF building in altering directions.
patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Forward, settings);
patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Backward, settings);
patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Forward, settings);
patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Backward, settings);
patchMatchNnfBuilder.RunBuildNnfIteration(nnf, destImage, srcImage, NeighboursCheckDirection.Forward, settings);

// Restore dest image from the NNF and source image.
nnf
    .RestoreImage(srcImage, 3, patchSize)
    .FromLabToRgb()
    .FromRgbToBitmap()
    .SaveTo(@"..\..\restored.png", ImageFormat.Png);

// Convert the NNF to an image and save.
nnf
    .ToRgbImage()
    .FromRgbToBitmap()
    .SaveTo(@"..\..\nnf.png", ImageFormat.Png);

```

| Dest image | Source image |
| ----------- | ------ |
| ![input1]   | ![input2]|

| NNF | Restored image|
| -------- | ---------- |
| ![nnf] | ![restored]|

### Advanced NNF building
It is also possible to provide a mapping of particular areas on both dest and source images to be used or ignored.

```csharp
var mappings = new Area2DMapBuilder()
    .InitNewMap(destImageArea, srcImageArea) // define area on the source that is used to get patches for the dest area
    .AddAssociatedAreas(destImageArea1, srcImageArea1)) // defaine exceptional dest area for 
                                                        // which patches should be found in the specified source area
    .AddAssociatedAreas(destImageArea2, srcImageArea2)
    .SetIgnoredSourcedArea(srcImageAreaToIgnore) // this area on the source will be ignored
    .Build();
```

| Ignored source areas | Result image |
| ----------- | ------ |
| ![ignoredSrc]   | ![ignoredSrcResult]|
    NOTE: All the yellowish parts are restored using other colors.

| Destination area | NNF | Result image|
| ----------- | ------ | ------ |
| ![destArea] | ![destNnf] | ![destAreaResult]|
    NOTE: NNF was built only for the specified dest area.

| Dest image with donor areas | Source image with donor areas|
| ----------- | ------ |
| ![destDonors]   | ![srcDonors]|

| NNF | Restored image|
| -------- | ---------- |
| ![donorsNnf] | ![donorsResult]|

[input1]: images/pm1small.png "dest image"
[input2]: images/pm2small.png "source image"
[nnf]:images/nnf.png "NNF"
[restored]: images/restored.png "restored" 
[ignoredSrc]: images/ignoredSrc.png "ignored source areas"
[ignoredSrcResult]: images/ignoredSrcResult.png "restored image"
[destArea]: images/destArea.png "destination area"
[destNnf]: images/destNnf.png "result NNF"
[destAreaResult]: images/destAreaResult.png "restored image"
[destDonors]: images/destDonors.png "dest image with donors"
[srcDonors]: images/srcDonors.png "src image with donors"
[donorsNnf]: images/donorsNnf.png "result NNF"
[donorsResult]: images/donorsResult.png "result image"
