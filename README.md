Volumes-Tech-Challenge

This is a project with an implementation of voxel generation & rendering, and samples of their use.

How to - Create a voxel object
From the context menu in the project view, hit Create/Voxels/VoxelAsset
This will create a blank voxel-asset, which is a container for everything needed to prepare voxel data.

A voxel asset requires an input mesh to be voxelised, a material to use for voxelising, and a resolution to voxelise at.
1. To create a voxelisation material, hit the "Create blank voxelisation material" button in the asset inspector to create & assign a material with the correct shader for voxelising. You can assign a desired color map to this material.
2. Then, assign the mesh to be voxelised
3. Finally, select a resolution for the produced voxel data

Once the voxel asset has been configured & shows no errors, hit the "Bake Texture" button in the inspector to produce the voxel 3D texture.
This voxel asset is now ready to use for rendering.

How to - Add a voxel object to a scene
Create a blank gameobject & add the VoxelRenderer component to it.
In the VoxelAsset field of the VoxelRenderer, pick the VoxelAsset you wish to render.
If you change the VoxelAsset property while the game is running, you will need to disable & re-enable the VoxelRenderer for the changes to take effect.
Optionally, you can provide a custom material to use for rendering instead of an automatically generated material.

Development Challenges:

-Mesh Voxelisation

After some research into methods of voxelising a mesh, I decided to use an Nvidia gpu voxelisation method as outlined here:
https://developer.nvidia.com/content/basics-gpu-voxelization
This just creates a surface volume & doesn't fill interiors, but is straightforward & fast.

My initial attempt followed this method closely, using a 3D RenderTexture as the UAV target. I was unable to get this initial attempt working, but switching from a RenderTexture to a ComputeBuffer both worked and was easier to read back the contents of for offline storage.

A second deviation from the method was that I'm developing this in MacOS, and Metal does not support geometry shaders in Unity, so instead of generating the side & top views via GS, I just render 3 times with a swizzle on the coordinates & accumulate the results. A potential future improvement is to use instancing to render the 3 views in one call.

-Volume rendering

In order to have a baseline implementation to work off, I created a brute-force raymarching shader. This is useful as a comparison point for any more sophisticated methods.

I created the start of a sphere-marching implementation that used a Signed Distance Field for better performance. I made the SDF in Houdini but it didn't match up exactly with the voxels created in unity, so the results are off. It does get a better surface shape with much fewer samples than the raymarcher though.