using System.Collections.Generic;
using UnityEngine;

namespace VoxelChallenge
{
    /// <summary>
    /// An asset representing voxel content
    /// A wrapper around generated voxel data & the settings for that generation
    /// </summary>
    [CreateAssetMenu(fileName = "Voxel Asset", menuName = "Voxels/Voxel Asset", order = 2)]
    public class VoxelAsset : ScriptableObject
    {
        public enum VoxelResolution
        {
            res4 = 1,
            res16 = 16,
            res64 = 64,
            res256 = 256
        };

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
        public VoxelResolution resolution = VoxelResolution.res64;

        /// <summary>
        /// The baked voxel texture, for passing to renderer at runtime
        /// </summary>
        [HideInInspector]
        public Texture3D voxelTexture;

        /// <summary>
        /// Checks for valid inputs & returns a string describing any problems
        /// Return string is empty if it's all good
        /// </summary>
        /// <returns></returns>
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
            return null;
        }

        /// <summary>
        /// Check if there's a problem, returns true if all good
        /// This only checks offline data needed for baking the voxel data
        /// </summary>
        public bool isValid
        {
            get
            {
                var problemText = GetProblemDescription();
                return string.IsNullOrEmpty(problemText);
            }
        }

        /// <summary>
        /// Checks if everything needed to render is present
        /// </summary>
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