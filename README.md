Volumes-Tech-Challenge

This is a project with an implementation of voxel generation & rendering, and samples of their use.

How to - Create a voxel object
...

How to - Add a voxel object to a scene
...

Development Challenges:

-Mesh Voxelisation

After some research into methods of voxelising a mesh, I decided to use an Nvidia gpu voxelisation method as outlined here:
https://developer.nvidia.com/content/basics-gpu-voxelization
This just creates a surface volume & doesn't fill interiors, but is straightforward & fast.

My initial attempt followed this method closely, using a 3D RenderTexture as the UAV target. I was unable to get this initial attempt working, but switching from a RenderTexture to a ComputeBuffer both worked and was easier to read back the contents of for offline processing.

A second deviation from the method was that I'm developing this in MacOS, and Metal does not support geometry shaders in Unity, so instead of generating the side & top views via GS, I just render 3 times with a swizzle on the coordinates & accumulate the results. A potential future improvement is to use instancing to render the 3 views in one call.

-Volume rendering

In order to have a baseline implementation to work off, I created a brute-force raymarching shader. This is useful as a comparison point for any more sophisticated methods.