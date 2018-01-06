# PatchMatch
[![license](https://img.shields.io/github/license/mashape/apistatus.svg?style=flat-square)]()

Implementation of **PatchMatch** algorithm for .NET.

## What is PatchMatch?
This is an algorithm that finds similar patches at given images and builds a **nearest neighbor field (NNF)**. 

The main advantage of the algorithm is that it is quite fast. It is based on the observations that some of the good matches can be found randomly and that these results can be propagated to the neighbour areas due to natural coherence of images.

More information can be found in [this scientific publication](http://gfx.cs.princeton.edu/pubs/Barnes_2009_PAR/index.php).

## What is it for?
It can be used in image processing and image editing tools (inpainting, image reshuffling, content aware image resizing etc).