using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VoxelChallenge
{
    /// <summary>
    /// Produces runtime voxel data from a voxel asset
    /// </summary>
    public static class Voxeliser
    {
        #region PrivateHelpers
        private static Mesh MeshFromBounds(Bounds bounds)
        {
            var mesh = new Mesh();
            var min = bounds.min;
            var max = bounds.max;

            // Vertex order
            // 1---2
            // |   |
            // 0---3
            var verts = new Vector3[8];
            verts[0] = new Vector3(min.x, min.y, min.z);
            verts[1] = new Vector3(min.x, max.y, min.z);
            verts[2] = new Vector3(max.x, max.y, min.z);
            verts[3] = new Vector3(max.x, min.y, min.z);
            // Next 4 are first for with max z
            for (int i = 4; i < verts.Length; i++)
            {
                var v = verts[i - 4];
                v.z = max.z;
                verts[i] = v;
            }
            var uv = new Vector3[8];
            uv[0] = new Vector3(0f, 0f, 1f);
            uv[1] = new Vector3(0f, 1f, 1f);
            uv[2] = new Vector3(1f, 1f, 1f);
            uv[3] = new Vector3(1f, 0f, 1f);
            // Next 4 are first for with max z
            for (int i = 4; i < uv.Length; i++)
            {
                var v = uv[i - 4];
                v.z = 1f;
                uv[i] = v;
            }

            // 4 indices per quad, 1 quad per face, 6 faces
            var indices = new int[]
            {
                // Front face
                0,1,2,3,
                // Left Face
                0,4,5,1,
                // Top Face
                1,5,6,2,
                // Right Face
                2,6,7,3,
                // Bottom Face
                3,7,4,0,
                // Back Face
                4,7,6,5
            };

            mesh.vertices = verts;
            mesh.SetUVs(0, uv);
            mesh.SetIndices(indices, MeshTopology.Quads, 0);
            mesh.RecalculateNormals();
            mesh.bounds = bounds;
            return mesh;
        }

        public static Texture2D VoxeliseMesh(Mesh mesh, int resolution, Material material)
        {
            var voxelUAVbuffer = new ComputeBuffer(resolution * resolution * resolution, sizeof(float) * 4, ComputeBufferType.Default);
            var localVoxelsBuffer = new float4[resolution * resolution * resolution];
            var viewRT = RenderTexture.GetTemporary(resolution * 2, resolution * 2, 0);

            var viewMatrix = new Matrix4x4();
            var b = mesh.bounds;
            var inverseMaxDimension = 1f / Mathf.Max(b.size.x, b.size.y, b.size.z);
            viewMatrix.SetTRS(-b.min * inverseMaxDimension, Quaternion.identity, Vector3.one * inverseMaxDimension);

            int metaRes = (int)Mathf.Sqrt(resolution);

            var commands = new CommandBuffer();
            commands.ClearRandomWriteTargets();
            commands.SetGlobalBuffer("_VoxelUAV", voxelUAVbuffer);
            commands.SetGlobalInt("_Res", resolution);
            commands.SetGlobalInt("_MetaRes", metaRes);
            commands.SetRandomWriteTarget(2, voxelUAVbuffer);
            commands.SetRenderTarget(viewRT);
            commands.ClearRenderTarget(true, true, Color.black);
            commands.DisableScissorRect();
            commands.SetViewMatrix(viewMatrix);
            commands.SetProjectionMatrix(Matrix4x4.identity);
            commands.DrawMesh(mesh, Matrix4x4.identity, material);
            Graphics.ExecuteCommandBuffer(commands);

            voxelUAVbuffer.GetData(localVoxelsBuffer);
            Color[] colors = new Color[localVoxelsBuffer.Length];
            for (int iter = 0; iter < colors.Length; iter++)
            {
                colors[iter].r = localVoxelsBuffer[iter].x;
                colors[iter].g = localVoxelsBuffer[iter].y;
                colors[iter].b = localVoxelsBuffer[iter].z;
                colors[iter].a = localVoxelsBuffer[iter].w;
            }
            Texture2D outTex = new Texture2D(resolution * metaRes, resolution * metaRes, TextureFormat.RGBA32, false);
            outTex.SetPixels(colors);

            voxelUAVbuffer.Release();
            RenderTexture.ReleaseTemporary(viewRT);
            Graphics.ClearRandomWriteTargets();

            return outTex;
        }
        #endregion

        public static VoxelRuntimeRepresentation Voxelise(VoxelAsset voxelAsset)
        {
            if (!voxelAsset.isValid)
            {
                return null;
            }

            var bounds = voxelAsset.sourceMesh.bounds;
            var mesh = MeshFromBounds(bounds);
            return new VoxelRuntimeRepresentation(bounds, mesh, null);
        }
    }
}