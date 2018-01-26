# PatchMatch
[![license](https://img.shields.io/github/license/mashape/apistatus.svg?style=flat-square)]()

Implementation of **PatchMatch** algorithm for .NET.

## What is PatchMatch?
This is an algorithm that finds similar patches at given images and builds a **nearest neighbor field (NNF)**. 

The main advantage of the algorithm is that it is quite fast. It is based on the observations that some of the good matches can be found randomly and that these results can be propagated to the neighbour areas due to natural coherence of images.

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
   