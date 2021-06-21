using UnityEngine;

namespace VoxelChallenge
{
    public class VoxelRenderer : MonoBehaviour
    {
        public VoxelAsset voxelAsset;
        public Material material;
        public Material material_LOD;
        public float LODdistance = 10f;

        private VoxelRuntimeRepresentation voxelData;
        private Material localMaterial;

        private void ConfigureMaterial(Material mat)
        {
            mat.SetVector("_VX_BoundsMin", voxelData.bounds.min);
            mat.SetVector("_VX_BoundsMax", voxelData.bounds.max);
            var size = voxelData.bounds.size;
            mat.SetVector("_VX_BoundsSize", size);
            float maxDimension = Mathf.Max(size.x, size.y, size.z);
            mat.SetFloat("_VX_BoundsMaxDimension", maxDimension);
            mat.SetVector("_VX_BoundsProportions", size / maxDimension);
        }

        private void OnEnable()
        {
            voxelData = Voxeliser.Voxelise(voxelAsset);
            localMaterial = material;
            if (voxelData != null)
            {
                ConfigureMaterial(localMaterial);
                ConfigureMaterial(material_LOD);
            }
        }

        private void OnDisable()
        {
            if (voxelData != null)
            {
                voxelData.Dispose();
            }
            voxelData = null;
        }

        private void Update()
        {
            if (voxelData == null || material == null)
            {
                return;
            }
            var materialToUse = localMaterial;
            var cameraDistance = Vector3.Distance(Camera.main.transform.position, transform.position);
            if (cameraDistance > LODdistance)
            {
                materialToUse = material_LOD;
            }
            materialToUse.SetVector("_VX_NoiseFrameOffset", new Vector2(Random.value, Random.value));
            Graphics.DrawMesh(voxelData.boundsMesh, transform.localToWorldMatrix, materialToUse, gameObject.layer);
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
            if (voxelData == null || material == null)
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