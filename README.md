# MRI Scan Segmentation in Virtual Reality

Given a set of MRI scans, create and application that allows a user to easily visualize various organs in virtual reality.

## Currently:

* Application starts with a 2D view of the scans. There is a scroll bar to scroll through the scans.
* Use the mouse to select a seed for the organ that you would like to view.
* Using a flood fill algorithm with a threshold value for pixel difference determining whether to spread to the next pixel.
* The resulting segment is saved to a set of png images which are black and red which correstponds to the original scans.
* Everything part of the segment is red and the rest is the original scan.

## Goals:

* 3D visualization of the segment ( going to meet with Sid on Friday 2/3 as he already has a rendering algorithm for segmented data )
* Implement a min-cut max-flow algorithm instead of flood fill ( flood fill is the most primitive solution and doesn't work a lot of the time )
