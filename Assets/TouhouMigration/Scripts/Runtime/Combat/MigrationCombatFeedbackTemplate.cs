using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    public sealed class MigrationCombatFeedbackTemplate : MonoBehaviour
    {
        [SerializeField] private string templateKind = string.Empty;
        [SerializeField] private bool poolingReady;
        [SerializeField] private string layerName = "Default";
        [SerializeField] private float lifetimeSeconds = 4f;
        [SerializeField] private float visualRadius = 0.18f;
        [SerializeField] private Color feedbackColor = new Color(1f, 0.2f, 0.12f, 1f);
        [SerializeField] private bool impactFeedbackEnabled;
        [SerializeField] private bool sweepCollisionEnabled;
        [SerializeField] private bool grazeEnabled;
        [SerializeField] private float grazeRadius;
        [SerializeField] private float perfectGrazeRadius;
        [SerializeField] private string projectileFamily = "enemy_projectile";
        [SerializeField] private bool shatterable;
        [SerializeField] private float shatterHp;
        [SerializeField] private string shatterWeaknesses = string.Empty;
        [SerializeField] private bool reflectable;
        [SerializeField] private bool reflectStunReward;
        [SerializeField] private float reflectStunSeconds;
        [SerializeField] private bool perfectFreezeCycleEnabled;
        [SerializeField] private float perfectFreezeSpraySeconds = 1.6f;
        [SerializeField] private float perfectFreezeFreezeSeconds = 2.4f;
        [SerializeField] private float perfectFreezeSpraySpeed = 4.2f;
        [SerializeField] private float perfectFreezeSprayDamage = 8f;
        [SerializeField] private float perfectFreezeFrozenDamage = 7f;
        [SerializeField] private float perfectFreezeThawSpeed = 8f;
        [SerializeField] private float perfectFreezeThawDamage = 10f;
        [SerializeField] private float perfectFreezeFrozenShatterHp = 20f;
        [SerializeField] private float armDelaySeconds;

        public string TemplateKind => templateKind;
        public bool PoolingReady => poolingReady;
        public string LayerName => layerName;
        public float LifetimeSeconds => lifetimeSeconds;
        public float VisualRadius => visualRadius;
        public Color FeedbackColor => feedbackColor;
        public bool ImpactFeedbackEnabled => impactFeedbackEnabled;
        public bool SweepCollisionEnabled => sweepCollisionEnabled;
        public bool GrazeEnabled => grazeEnabled;
        public float GrazeRadius => grazeRadius;
        public float PerfectGrazeRadius => perfectGrazeRadius;
        public string ProjectileFamily => projectileFamily;
        public bool Shatterable => shatterable;
        public float ShatterHp => shatterHp;
        public string ShatterWeaknesses => shatterWeaknesses;
        public bool Reflectable => reflectable;
        public bool ReflectStunReward => reflectStunReward;
        public float ReflectStunSeconds => reflectStunSeconds;
        public bool PerfectFreezeCycleEnabled => perfectFreezeCycleEnabled;
        public float PerfectFreezeSpraySeconds => perfectFreezeSpraySeconds;
        public float PerfectFreezeFreezeSeconds => perfectFreezeFreezeSeconds;
        public float PerfectFreezeSpraySpeed => perfectFreezeSpraySpeed;
        public float PerfectFreezeSprayDamage => perfectFreezeSprayDamage;
        public float PerfectFreezeFrozenDamage => perfectFreezeFrozenDamage;
        public float PerfectFreezeThawSpeed => perfectFreezeThawSpeed;
        public float PerfectFreezeThawDamage => perfectFreezeThawDamage;
        public float PerfectFreezeFrozenShatterHp => perfectFreezeFrozenShatterHp;
        public float ArmDelaySeconds => armDelaySeconds;

        public void ConfigureTemplate(
            string kind,
            bool poolingReady,
            string layerName,
            float lifetimeSeconds,
            float visualRadius,
            Color feedbackColor,
            bool impactFeedbackEnabled,
            bool sweepCollisionEnabled,
            bool grazeEnabled = false,
            float grazeRadius = 0f,
            float perfectGrazeRadius = 0f,
            string projectileFamily = "enemy_projectile",
            bool shatterable = false,
            float shatterHp = 0f,
            string shatterWeaknesses = "",
            bool perfectFreezeCycleEnabled = false,
            float perfectFreezeSpraySeconds = 1.6f,
            float perfectFreezeFreezeSeconds = 2.4f,
            float perfectFreezeSpraySpeed = 4.2f,
            float perfectFreezeSprayDamage = 8f,
            float perfectFreezeFrozenDamage = 7f,
            float perfectFreezeThawSpeed = 8f,
            float perfectFreezeThawDamage = 10f,
            float perfectFreezeFrozenShatterHp = 20f,
            float armDelaySeconds = 0f,
            bool reflectable = false,
            bool reflectStunReward = false,
            float reflectStunSeconds = 0f)
        {
            templateKind = string.IsNullOrWhiteSpace(kind) ? string.Empty : kind.Trim();
            this.poolingReady = poolingReady;
            this.layerName = string.IsNullOrWhiteSpace(layerName) ? "Default" : layerName.Trim();
            this.lifetimeSeconds = Mathf.Max(0f, lifetimeSeconds);
            this.visualRadius = Mathf.Max(0.01f, visualRadius);
            this.feedbackColor = feedbackColor;
            this.impactFeedbackEnabled = impactFeedbackEnabled;
            this.sweepCollisionEnabled = sweepCollisionEnabled;
            this.grazeEnabled = grazeEnabled;
            this.grazeRadius = Mathf.Max(0f, grazeRadius);
            this.perfectGrazeRadius = Mathf.Min(this.grazeRadius, Mathf.Max(0f, perfectGrazeRadius));
            this.projectileFamily = string.IsNullOrWhiteSpace(projectileFamily) ? string.Empty : projectileFamily.Trim();
            this.shatterable = shatterable;
            this.shatterHp = Mathf.Max(0f, shatterHp);
            this.shatterWeaknesses = string.IsNullOrWhiteSpace(shatterWeaknesses) ? string.Empty : shatterWeaknesses.Trim();
            this.reflectable = reflectable;
            this.reflectStunReward = reflectStunReward;
            this.reflectStunSeconds = Mathf.Max(0f, reflectStunSeconds);
            this.perfectFreezeCycleEnabled = perfectFreezeCycleEnabled;
            this.perfectFreezeSpraySeconds = Mathf.Max(0f, perfectFreezeSpraySeconds);
            this.perfectFreezeFreezeSeconds = Mathf.Max(0f, perfectFreezeFreezeSeconds);
            this.perfectFreezeSpraySpeed = Mathf.Max(0f, perfectFreezeSpraySpeed);
            this.perfectFreezeSprayDamage = Mathf.Max(0f, perfectFreezeSprayDamage);
            this.perfectFreezeFrozenDamage = Mathf.Max(0f, perfectFreezeFrozenDamage);
            this.perfectFreezeThawSpeed = Mathf.Max(0f, perfectFreezeThawSpeed);
            this.perfectFreezeThawDamage = Mathf.Max(0f, perfectFreezeThawDamage);
            this.perfectFreezeFrozenShatterHp = Mathf.Max(0f, perfectFreezeFrozenShatterHp);
            this.armDelaySeconds = Mathf.Max(0f, armDelaySeconds);
            ApplyLayerPolicy(gameObject, this.layerName);
        }

        public static void ApplyLayerPolicy(GameObject target, string layerName)
        {
            if (target == null || string.IsNullOrWhiteSpace(layerName))
            {
                return;
            }

            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
            {
                target.layer = layer;
            }
        }
    }
}
