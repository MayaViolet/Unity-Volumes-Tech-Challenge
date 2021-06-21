using UnityEngine;

namespace VoxelChallenge
{
    public class VoxelRenderer : MonoBehaviour
    {
        public VoxelAsset voxelAsset;
        public Material material;

        private VoxelRuntimeRepresentation voxelData;
        private Material localMaterial;

        private void OnEnable()
        {
            voxelData = Voxeliser.Voxelise(voxelAsset);
            localMaterial = new Material(material);
            if (voxelData != null)
            {
                localMaterial.SetVector("_VX_BoundsMin", voxelData.bounds.min);
                localMaterial.SetVector("_VX_BoundsMax", voxelData.bounds.max);
                var size = voxelData.bounds.size;
                localMaterial.SetVector("_VX_BoundsSize", size);
                float maxDimension = Mathf.Max(size.x, size.y, size.z);
                localMaterial.SetFloat("_VX_BoundsMaxDimension", maxDimension);
                localMaterial.SetVector("_VX_BoundsProportions", size/ maxDimension);
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