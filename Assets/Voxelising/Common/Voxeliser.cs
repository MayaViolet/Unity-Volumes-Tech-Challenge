using UnityEngine;

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