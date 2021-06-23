using System.IO;
using UnityEngine;
using UnityEditor;

namespace VoxelChallenge
{
    [CustomEditor(typeof(VoxelAsset)), CanEditMultipleObjects]
    public class VoxelAssetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Create blank voxelising material"))
            {
                foreach (VoxelAsset asset in targets)
                {
                    CreateBlankVoxeliseMaterial(asset);
                }
            }

            if (GUILayout.Button("Bake Texture"))
            {
                foreach (VoxelAsset asset in targets)
                {
                    BakeTexture(asset);
                }
            }

            GUI.color = Color.red;
            foreach (VoxelAsset asset in targets)
            {
                if (!asset.isValid)
                {
                    string problemText = asset.GetProblemDescription();
                    GUILayout.Label(asset.name + " is invalid! "+problemText);
                }
            }
        }

        private string GetAppendedPath(VoxelAsset asset, string ending)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            var pathDirectory = Path.GetDirectoryName(path);
            var pathFilename = Path.GetFileNameWithoutExtension(path);
            return Path.Combine(pathDirectory, pathFilename + ending);
        }

        private void BakeTexture(VoxelAsset asset)
        {
            if (!asset.isValid)
            {
                return;
            }
            var bakedTexture = Voxeliser.VoxeliseMesh(asset.sourceMesh, asset.resolution, asset.voxelisingMaterial);
            var pngData = bakedTexture.EncodeToPNG();

            var path = GetAppendedPath(asset, "_baked.png");

            File.WriteAllBytes(path, pngData);
            AssetDatabase.ImportAsset(path);
            Debug.Log("Saved to " + path);
            
            int metaRes = (int)Mathf.Sqrt(asset.resolution);
            var importer = TextureImporter.GetAtPath(path) as TextureImporter;
            var settings = new TextureImporterSettings();
            settings.textureShape = TextureImporterShape.Texture3D;
            settings.flipbookColumns = metaRes;
            settings.flipbookRows = metaRes;
            settings.alphaSource = TextureImporterAlphaSource.FromInput;
            settings.wrapMode = TextureWrapMode.Clamp;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            asset.voxelTexture = AssetDatabase.LoadAssetAtPath<Texture3D>(path);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        private void CreateBlankVoxeliseMaterial(VoxelAsset asset)
        {
            var path = GetAppendedPath(asset, "_voxelise.mat");
            if (File.Exists(path))
            {
                return;
            }

            Material material = new Material(Shader.Find("Voxels/VoxeliseToBuffer"));
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.ImportAsset(path);
            asset.voxelisingMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }
    }
}