using System;
using UnityEngine;

namespace VoxelChallenge
{
    /// <summary>
    /// A container for everything needed to render voxels
    /// This is the ephemeral generated runtime data, not the persistent assets
    /// </summary>
    public class VoxelRuntimeRepresentation
    {
        /// <summary>
        /// Bounds of the voxel object, for culling etc
        /// </summary>
        public readonly Bounds bounds;
        /// <summary>
        /// Mesh surrounding the extents of the voxel data, for rendering
        /// </summary>
        public readonly Mesh boundsMesh;
        /// <summary>
        /// 3D texture of voxel data
        /// </summary>
        public readonly Texture3D voxelTexture;

        public VoxelRuntimeRepresentation(Bounds bounds, Mesh boundsMesh, Texture3D voxelTexture)
        {
            this.bounds = bounds;
            this.boundsMesh = boundsMesh;
            this.voxelTexture = voxelTexture;
        }
    }
}