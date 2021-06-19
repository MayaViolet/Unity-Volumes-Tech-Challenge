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
                    GUILayout.Label(asset.name + " is invalid!");
                }
            }
        }

        private void BakeTexture(VoxelAsset asset)
        {
            if (!asset.isValid)
            {
                return;
            }
            var bakedTexture = Voxeliser.VoxeliseMesh(asset.sourceMesh, asset.resolution, asset.voxelisingMaterial);
            var pngData = bakedTexture.EncodeToPNG();

            var path = AssetDatabase.GetAssetPath(asset);
            var pathDirectory = Path.GetDirectoryName(path);
            var pathFilename = Path.GetFileNameWithoutExtension(path);
            path = Path.Combine(pathDirectory, pathFilename + "_baked.png");

            var doFirstSetup = !File.Exists(path);

            File.WriteAllBytes(path, pngData);
            AssetDatabase.ImportAsset(path);
            Debug.Log("Saved to " + path);

            if (doFirstSetup)
            {
                int metaRes = (int)Mathf.Sqrt(asset.resolution);
                var importer = TextureImporter.GetAtPath(path) as TextureImporter;
                var settings = new TextureImporterSettings();
                settings.textureShape = TextureImporterShape.Texture3D;
                settings.flipbookColumns = metaRes;
                settings.flipbookRows = metaRes;
                settings.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }
    }
}