using System.Collections.Generic;
using UnityEngine;

namespace VoxelChallenge
{
    /// <summary>
    /// An asset representing voxel content
    /// A wrapper around a method to generate voxel data & the settings for that generation
    /// TODO: Genericise between different voxel data sources, eg procedural
    /// </summary>
    [CreateAssetMenu(fileName = "Voxel Asset", menuName = "Voxels/Voxel Asset", order = 2)]
    public class VoxelAsset : ScriptableObject
    {
        /// <summary>
        /// The mesh to be voxelised
        /// </summary>
        [SerializeField]
        public Mesh sourceMesh;

        /// <summary>
        /// todo
        /// </summary>
        [SerializeField]
        public Material voxelisingMaterial;

        /// <summary>
        /// The resolution of the 3d voxel texture created
        /// </summary>
        [SerializeField]
        public int resolution = 32;

        public bool isValid
        {
            get
            {
                if (sourceMesh == null || !sourceMesh.isReadable || sourceMesh.vertexCount < 1)
                {
                    return false;
                }
                if (voxelisingMaterial == null)
                {
                    return false;
                }
                if (resolution < 1)
                {
                    return false;
                }
                return true;
            }
        }
    }

}