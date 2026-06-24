using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    public sealed class MigrationEnemyAnimationSource : MonoBehaviour
    {
        [SerializeField] private string variantId = string.Empty;
        [SerializeField] private string animatorControllerAssetPath = string.Empty;
        [SerializeField] private string idleClipPath = string.Empty;
        [SerializeField] private string moveClipPath = string.Empty;
        [SerializeField] private string attackClipPath = string.Empty;
        [SerializeField] private string projectileClipPath = string.Empty;
        [SerializeField] private string takeDamageClipPath = string.Empty;
        [SerializeField] private string dieClipPath = string.Empty;
        [SerializeField] private bool usesFallbackAnimations = true;

        public string VariantId => variantId;
        public string AnimatorControllerAssetPath => animatorControllerAssetPath;
        public string IdleClipPath => idleClipPath;
        public string MoveClipPath => moveClipPath;
        public string AttackClipPath => attackClipPath;
        public string ProjectileClipPath => projectileClipPath;
        public string TakeDamageClipPath => takeDamageClipPath;
        public string DieClipPath => dieClipPath;
        public bool UsesFallbackAnimations => usesFallbackAnimations;
        public bool HasIdle => !string.IsNullOrEmpty(idleClipPath);
        public bool HasMove => !string.IsNullOrEmpty(moveClipPath);
        public bool HasAttack => !string.IsNullOrEmpty(attackClipPath);
        public bool HasProjectile => !string.IsNullOrEmpty(projectileClipPath);
        public bool HasTakeDamage => !string.IsNullOrEmpty(takeDamageClipPath);
        public bool HasDie => !string.IsNullOrEmpty(dieClipPath);

        public void Configure(
            string sourceVariantId,
            string controllerPath,
            string sourceIdleClipPath,
            string sourceMoveClipPath,
            string sourceAttackClipPath,
            string sourceProjectileClipPath,
            string sourceTakeDamageClipPath,
            string sourceDieClipPath,
            bool sourceUsesFallbackAnimations)
        {
            variantId = string.IsNullOrWhiteSpace(sourceVariantId) ? string.Empty : sourceVariantId.Trim().ToLowerInvariant();
            animatorControllerAssetPath = CleanPath(controllerPath);
            idleClipPath = CleanPath(sourceIdleClipPath);
            moveClipPath = CleanPath(sourceMoveClipPath);
            attackClipPath = CleanPath(sourceAttackClipPath);
            projectileClipPath = CleanPath(sourceProjectileClipPath);
            takeDamageClipPath = CleanPath(sourceTakeDamageClipPath);
            dieClipPath = CleanPath(sourceDieClipPath);
            usesFallbackAnimations = sourceUsesFallbackAnimations;
        }

        private static string CleanPath(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
