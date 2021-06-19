using UnityEngine;
using UnityEngine.Rendering;
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
            mesh.SetIndices(indices, MeshTopology.Quads, 0);
            mesh.RecalculateNormals();
            mesh.bounds = bounds;
            return mesh;
        }

        private static RenderTexture VoxeliseMesh(Mesh mesh, int resolution, Material material)
        {
            var volumeUAV = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                name = "Voxelisation UAV",
                dimension = TextureDimension.Tex3D,
                volumeDepth = resolution,
                //antiAliasing = 8,
                filterMode = FilterMode.Point,
                enableRandomWrite = true
            };
            var volumeOutput = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                name = "Output Voxel Texture",
                dimension = TextureDimension.Tex3D,
                volumeDepth = resolution,
                //antiAliasing = 8,
                filterMode = FilterMode.Point
            };


            var viewRT = RenderTexture.GetTemporary(resolution*16, resolution*16, 0);
            var uav2D = RenderTexture.GetTemporary(resolution, resolution * resolution, 0);
            var outRT = RenderTexture.GetTemporary(resolution, resolution * resolution, 0);
            uav2D.enableRandomWrite = true;

            var uav2DBuf = new ComputeBuffer(resolution * resolution * resolution * sizeof(float) * 4, sizeof(float) * 4, ComputeBufferType.Structured);

            var viewMatrix = new Matrix4x4();
            var b = mesh.bounds;
            var inverseMaxDimension = 1f / Mathf.Max(b.size.x, b.size.y, b.size.z);
            viewMatrix.SetTRS(-b.min * inverseMaxDimension, Quaternion.identity, Vector3.one * inverseMaxDimension);

            var commands = new CommandBuffer();

            commands.ClearRandomWriteTargets();
            commands.SetGlobalTexture("_VoxelUAV", uav2D);
            commands.SetGlobalBuffer("_VoxelUAVBuf", uav2DBuf);
            commands.SetRandomWriteTarget(2, uav2DBuf, true);
            commands.SetRenderTarget(uav2D);
            commands.SetRenderTarget(viewRT);
            commands.ClearRenderTarget(true, true, Color.black);
            commands.DisableScissorRect();
            commands.SetViewMatrix(Matrix4x4.identity);
            commands.SetProjectionMatrix(Matrix4x4.identity);
            commands.DrawMesh(mesh, viewMatrix, material);
            commands.CopyTexture(uav2D, outRT);
            commands.CopyTexture(volumeUAV, volumeOutput);
            Graphics.ExecuteCommandBuffer(commands);

            SaveRTToFile(outRT);
            RenderTexture.ReleaseTemporary(viewRT);
            RenderTexture.ReleaseTemporary(uav2D);
            RenderTexture.ReleaseTemporary(outRT);
            uav2DBuf.Release();
            volumeUAV.Release();
            Graphics.ClearRandomWriteTargets();

            #if UNITY_EDITOR
            //AssetDatabase.CreateAsset(volumeOutput, "Assets/" + "3dtextout" + ".asset");
            #endif

            return volumeOutput;
        }

        public static void SaveRTToFile(RenderTexture rt)
        {
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            RenderTexture.active = null;

            byte[] bytes;
            bytes = tex.EncodeToPNG();

            string path = "Assets/" + "savedout" + ".png";
            System.IO.File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path);
            Debug.Log("Saved to " + path);
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
            var texture = VoxeliseMesh(voxelAsset.sourceMesh, voxelAsset.resolution, voxelAsset.voxelisingMaterial);
            return new VoxelRuntimeRepresentation(bounds, mesh, texture);
        }
    }
}