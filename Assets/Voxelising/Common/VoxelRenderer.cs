using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using Unity.Mathematics;

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
            Graphics.DrawMesh(voxelData.boundsMesh, transform.localToWorldMatrix, material, gameObject.layer);
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
            Gizmos.DrawWireMesh(voxelData.boundsMesh);
        }

        private void OnDrawGizmos()
        {
            DrawGrizmosInternal(false);
        }
        #endif
    }
}