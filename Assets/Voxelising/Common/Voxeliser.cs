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
        /// <summary>
        /// Creates a rectangular prism mesh from a Bounds
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
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
            // Create uniform uvs in case they're wanted
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
        #endregion

        /// <summary>
        /// Produces a 2D sliced volume texture
        /// Based on: https://developer.nvidia.com/content/basics-gpu-voxelization
        /// </summary>
        /// <param name="mesh">The mesh to be voxelised</param>
        /// <param name="resolution">The resolution of the resulting 3D data</param>
        /// <param name="material">The material to be used for voxelising</param>
        /// <returns></returns>
        public static Texture2D VoxeliseMesh(Mesh mesh, int resolution, Material material)
        {
            // Create our temporary buffers & render target
            var voxelUAVbuffer = new ComputeBuffer(resolution * resolution * resolution, sizeof(float) * 4, ComputeBufferType.Default);
            var localVoxelsBuffer = new float4[resolution * resolution * resolution];
            var viewRT = RenderTexture.GetTemporary(resolution * 2, resolution * 2, 0);

            // We want to render the model into a normalised 1x1x1 cube, on the origin
            var viewMatrix = new Matrix4x4();
            var b = mesh.bounds;
            var inverseMaxDimension = 1f / Mathf.Max(b.size.x, b.size.y, b.size.z);
            var inverseSize = new Vector3(1f / b.size.x, 1f / b.size.y, 1f / b.size.z);
            viewMatrix.SetTRS(-b.min * inverseMaxDimension, Quaternion.identity, inverseSize);

            // Slices are arraged in a square grid, so our "meta" resolution of this 2D grid is the square-root of our 3D resolution
            int metaRes = (int)Mathf.Sqrt(resolution);
            var localMaterial = new Material(material);
            localMaterial.SetInt("_VX_Res", resolution);
            localMaterial.SetInt("_VX_MetaRes", metaRes);

            var commands = new CommandBuffer();
            commands.ClearRandomWriteTargets();
            commands.SetGlobalBuffer("_VoxelUAV", voxelUAVbuffer);
            commands.SetRandomWriteTarget(2, voxelUAVbuffer);
            commands.SetRenderTarget(viewRT);
            commands.SetViewMatrix(viewMatrix);
            commands.SetProjectionMatrix(Matrix4x4.identity);
            // For gapless voxelisation we need to render from front, top, and side
            // This is achieved with swizzles in the vertex shader
            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                commands.DrawMesh(mesh, Matrix4x4.identity, localMaterial, submesh);
                commands.EnableShaderKeyword("VX_SWIZZLE_LEFT");
                commands.DrawMesh(mesh, Matrix4x4.identity, localMaterial, submesh);
                commands.DisableShaderKeyword("VX_SWIZZLE_LEFT");
                commands.EnableShaderKeyword("VX_SWIZZLE_TOP");
                commands.DrawMesh(mesh, Matrix4x4.identity, localMaterial, submesh);
                commands.DisableShaderKeyword("VX_SWIZZLE_TOP");
            }
            Graphics.ExecuteCommandBuffer(commands);

            // Copy the UAV buffer data to managed memory
            voxelUAVbuffer.GetData(localVoxelsBuffer);
            // Convert the float4 buffer data into Colors
            Color[] colors = new Color[localVoxelsBuffer.Length];
            for (int iter = 0; iter < colors.Length; iter++)
            {
                colors[iter].r = localVoxelsBuffer[iter].x;
                colors[iter].g = localVoxelsBuffer[iter].y;
                colors[iter].b = localVoxelsBuffer[iter].z;
                colors[iter].a = localVoxelsBuffer[iter].w;
            }
            // Generate the 2D slices texture from the colors buffer
            Texture2D outTex = new Texture2D(resolution * metaRes, resolution * metaRes, TextureFormat.RGBA32, false);
            outTex.SetPixels(colors);

            // Cleanup temporary buffers
            voxelUAVbuffer.Release();
            RenderTexture.ReleaseTemporary(viewRT);
            // If you don't clear the UAV targets after you're done the Editor UI breaks
            Graphics.ClearRandomWriteTargets();

            return outTex;
        }

        /// <summary>
        /// Given a VoxelAsset, generates everything needed for rendering
        /// </summary>
        /// <param name="voxelAsset"></param>
        /// <returns></returns>
        public static VoxelRuntimeRepresentation PrepareVoxelRepresentation(VoxelAsset voxelAsset)
        {
            if (!voxelAsset.isReadyToDraw)
            {
                return null;
            }

            var bounds = voxelAsset.sourceMesh.bounds;
            var mesh = MeshFromBounds(bounds);
            return new VoxelRuntimeRepresentation(bounds, mesh, voxelAsset.voxelTexture);
        }
    }
}