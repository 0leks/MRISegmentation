# MRI Scan Segmentation in Virtual Reality

Given a set of MRI scans, create and application that allows a user to easily visualize various organs in virtual reality.

## Currently:

* Application starts with a 2D view of the scans. There is a scroll bar to scroll through the scans.
* Use the mouse to select a seed for the organ that you would like to view.
* Using a flood fill algorithm with a threshold value for pixel difference determining whether to spread to the next pixel.
* The resulting segment is saved to a set of png images which are black and white which correspond to the original scans.
  * Everything part of the segment is black and the rest is white.
* The program then switches to 3D view where it renders the segment in 3D and you can use mouse controls to move it around
* To switch between selecting a seed for segmentation and the 3D view, use *Space*
* Additional segments are added to the ongoing segment, pressing *R* resets the segment.
* Oculus controls: 
  * Use the index finger trigger to grab and move the volume, this maintains orientation.
  * Use the other trigger to affect the rotation of the volume
  * Pressing both index triggers at once allows you to change the size of the volume

## Goals:
* ~~3D visualization of the segment ( going to meet with Sid on Friday 2/3 as he already has a rendering algorithm for segmented data )~~
* Implement a min-cut max-flow algorithm instead of flood fill ( flood fill is the most primitive solution and doesn't work a lot of the time )
* Implement 3D manipulation of the segmentation which the oculus touch controllers.
  * ~~A way to move the segment around, with grab and drag functionality~~
  * ~~A way to rotate the segment, grab with a different button and rotate controller to rotate the segment~~
  * A way to modify the result of the segmentation by 
    * placing a barrier
    * marking areas as part of the segment or as not part of the segment
