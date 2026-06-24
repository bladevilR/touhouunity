using System;
using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    public sealed class MigrationEnemyVisualSource : MonoBehaviour
    {
        [SerializeField] private string variantId = string.Empty;
        [SerializeField] private string godotScenePath = string.Empty;
        [SerializeField] private string unityModelAssetPath = string.Empty;
        [SerializeField] private string primaryTextureAssetPath = string.Empty;
        [SerializeField] private string[] textureAssetPaths = Array.Empty<string>();
        [SerializeField] private bool usesFallbackVisual = true;

        public string VariantId => variantId;
        public string GodotScenePath => godotScenePath;
        public string UnityModelAssetPath => unityModelAssetPath;
        public string PrimaryTextureAssetPath => primaryTextureAssetPath;
        public string[] TextureAssetPaths => textureAssetPaths;
        public bool UsesFallbackVisual => usesFallbackVisual;

        public void Configure(
            string sourceVariantId,
            string sourceGodotScenePath,
            string sourceUnityModelAssetPath,
            string[] sourceTextureAssetPaths,
            bool sourceUsesFallbackVisual)
        {
            variantId = string.IsNullOrWhiteSpace(sourceVariantId) ? string.Empty : sourceVariantId.Trim().ToLowerInvariant();
            godotScenePath = string.IsNullOrWhiteSpace(sourceGodotScenePath) ? string.Empty : sourceGodotScenePath.Trim();
            unityModelAssetPath = string.IsNullOrWhiteSpace(sourceUnityModelAssetPath) ? string.Empty : sourceUnityModelAssetPath.Trim();
            textureAssetPaths = sourceTextureAssetPaths == null ? Array.Empty<string>() : (string[])sourceTextureAssetPaths.Clone();
            primaryTextureAssetPath = textureAssetPaths.Length == 0 ? string.Empty : textureAssetPaths[0];
            usesFallbackVisual = sourceUsesFallbackVisual;
        }
    }
}
