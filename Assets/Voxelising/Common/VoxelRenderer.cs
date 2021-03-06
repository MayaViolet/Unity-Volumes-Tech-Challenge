using UnityEngine;

namespace VoxelChallenge
{
    /// <summary>
    /// Component for rendering voxel data
    /// </summary>
    public class VoxelRenderer : MonoBehaviour
    {
        /// <summary>
        /// The VoxelAsset of the object to render
        /// </summary>
        public VoxelAsset voxelAsset;
        /// <summary>
        /// Optional material to use for rendering instead of a generated material
        /// </summary>
        public Material material_override;

        private VoxelRuntimeRepresentation voxelData;
        private Material localMaterial;

        /// <summary>
        /// Assign the global shader values needed by voxel shaders
        /// </summary>
        /// <param name="mat">The material to configure</param>
        private void ConfigureMaterial(Material mat)
        {
            var size = voxelData.bounds.size;
            float maxDimension = Mathf.Max(size.x, size.y, size.z);

            mat.SetVector("_VX_BoundsMin", voxelData.bounds.min);
            mat.SetVector("_VX_BoundsMax", voxelData.bounds.max);
            mat.SetVector("_VX_BoundsSize", size);
            mat.SetFloat("_VX_BoundsMaxDimension", maxDimension);
            mat.SetVector("_VX_BoundsProportions", size / maxDimension);
            mat.SetInt("_VX_RaymarchStepCount", voxelData.voxelTexture.width);
        }

        private void OnEnable()
        {
            // Data & materials needed for rendering are setup on enable
            voxelData = Voxeliser.PrepareVoxelRepresentation(voxelAsset);
            if (voxelData == null)
            {
                return;
            }

            if (material_override != null)
            {
                localMaterial = new Material(material_override);
            }
            else
            {
                localMaterial = new Material(Shader.Find("Voxels/VoxelRayMarch"));
                localMaterial.mainTexture = voxelData.voxelTexture;
            }
            ConfigureMaterial(localMaterial);
        }

        private void OnDisable()
        {
            
        }

        private void Update()
        {
            if (voxelData == null || localMaterial == null)
            {
                return;
            }
            
            localMaterial.SetVector("_VX_NoiseFrameOffset", new Vector2(Random.value, Random.value));
            Graphics.DrawMesh(voxelData.boundsMesh, transform.localToWorldMatrix, localMaterial, gameObject.layer);
        }

        #if UNITY_EDITOR
        private void DrawGrizmosInternal(bool selected)
        {
            if (selected)
            {
                Gizmos.color = Color.cyan;
            }
            else
            {
                Gizmos.color = Color.gray;
            }
            if (voxelData == null || localMaterial == null)
            {
                Gizmos.DrawWireSphere(transform.position, 0.5f);
                return;
            }
            Gizmos.DrawWireMesh(voxelData.boundsMesh, transform.position, transform.rotation, transform.lossyScale);
        }

        private void OnDrawGizmos()
        {
            DrawGrizmosInternal(false);
        }
        #endif
    }
}