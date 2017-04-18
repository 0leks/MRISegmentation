# MRI Scan Segmentation in Virtual Reality

Given a set of MRI scans, create and application that allows a user to easily visualize various organs in virtual reality.

## Currently:

* Application starts with a 2D view of the scans. There is a scroll bar to scroll through the scans.
* Use the mouse to add pixels to the object using left or to the the background using right click.
* The resulting segment is saved to a set of png images which are black and white which correspond to the original scans.
  * Everything part of the segment is black and the rest is white.
* Keyboard controls:
  * *R* to reset the segment as well as the chosen object and background points.
  * *Space* to switch between the 2D view and the 3D view.
  * *F* to generate a segment using region-growing / flood-fill.
  * *S* to generate a segment using Min-cut / Max-flow.
* Oculus controls: 
  * Use the index finger trigger to grab and move the volume, this maintains orientation.
  * Use the other trigger to affect the rotation of the volume
  * Pressing both index triggers at once allows you to change the size of the volume
  * *B* adds the selected point to the object
  * *A* adds the selected point to the background
  
## Goals:
* ~~3D visualization of the segment ( going to meet with Sid on Friday 2/3 as he already has a rendering algorithm for segmented data )~~
* ~~Implement a min-cut max-flow algorithm instead of flood fill ( flood fill is the most primitive solution and doesn't work a lot of the time )~~
* Improve the algorithm to be able to handle larger images in a reasonable amount of time
  * There is now an option to reduce the scale of the data by averaging several pixels together in a grid.
  * Currently working on
    * using this technique in the first pass of the algorithm
    * then increasing the resolution of the pixels along the edge of segment and recomputing the max flow
* ~~Implement 3D manipulation of the segmentation which the oculus touch controllers.~~
  * ~~A way to move the segment around, with grab and drag functionality~~
  * ~~A way to rotate the segment, grab with a different button and rotate controller to rotate the segment~~
  * ~~A way to modify the result of the segmentation by~~
    * ~~marking areas as part of the segment or as not part of the segment~~
* Add better GUI to be able to save and load segments at will.
* Save segments as a text file instead of a set of pngs
* Make ~~Region Growing~~ and Max Flow run in separate threads so that the application doesn't freeze while they run.
* convert segments into triangle meshes for faster rendering
