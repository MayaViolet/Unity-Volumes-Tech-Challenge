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
        /// Material to use for voxelisation
        /// This should use the "Voxels/VoxeliseToBuffer" shader
        /// </summary>
        [SerializeField]
        public Material voxelisingMaterial;

        /// <summary>
        /// The resolution of the 3d voxel texture created
        /// </summary>
        [SerializeField]
        public int resolution = 32;

        /// <summary>
        /// The baked voxel texture, for passing to renderer at runtime
        /// </summary>
        [HideInInspector]
        public Texture3D voxelTexture;

        public string GetProblemDescription()
        {
            if (sourceMesh == null)
            {
                return "Missing mesh";
            }
            if (sourceMesh.vertexCount < 1)
            {
                return "Empty mesh";
            }
            if (voxelisingMaterial == null)
            {
                return "Missing voxelising material";
            }
            if (resolution < 1)
            {
                return "Resolution must be at least 1";
            }
            return null;
        }

        public bool isValid
        {
            get
            {
                var problemText = GetProblemDescription();
                return string.IsNullOrEmpty(problemText);
            }
        }

        public bool isReadyToDraw
        {
            get
            {
                if (!isValid)
                {
                    return false;
                }
                if (voxelTexture == null)
                {
                    return false;
                }
                return true;
            }
        }
    }

}