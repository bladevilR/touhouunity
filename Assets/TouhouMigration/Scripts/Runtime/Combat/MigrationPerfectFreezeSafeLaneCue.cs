using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    [DisallowMultipleComponent]
    public sealed class MigrationPerfectFreezeSafeLaneCue : MonoBehaviour
    {
        [SerializeField] private float halfAngleDegrees = 22f;
        [SerializeField] private float durationSeconds = 1.05f;
        [SerializeField] private Color cueColor = new Color(1f, 0.54f, 0.18f, 0.3f);
        [SerializeField] private Renderer cueRenderer;

        private float remainingSeconds;

        public bool IsActive { get; private set; }
        public float HalfAngleDegrees => halfAngleDegrees;
        public float DurationSeconds => durationSeconds;
        public Color CueColor => cueColor;
        public Vector3 LastLaneCenter { get; private set; }
        public Vector3 LastTargetPosition { get; private set; }
        public Vector3 LastLaneDirection { get; private set; } = Vector3.forward;
        public int CueEventCount { get; private set; }

        public void ConfigureCue(float halfAngleDegrees, float durationSeconds, Color color)
        {
            this.halfAngleDegrees = Mathf.Clamp(Mathf.Abs(halfAngleDegrees), 0f, 89f);
            this.durationSeconds = Mathf.Max(0f, durationSeconds);
            cueColor = color;
            ApplyVisualMaterial();
        }

        public void BindRenderer(Renderer renderer)
        {
            cueRenderer = renderer;
            ApplyVisualMaterial();
            SetVisualEnabled(IsActive);
        }

        public void ShowCue(Vector3 center, Vector3 targetPosition)
        {
            LastLaneCenter = center;
            LastTargetPosition = targetPosition;
            Vector3 direction = targetPosition - center;
            direction.y = 0f;
            LastLaneDirection = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector3.forward;

            transform.position = center;
            transform.rotation = Quaternion.LookRotation(LastLaneDirection, Vector3.up);
            remainingSeconds = durationSeconds;
            IsActive = true;
            CueEventCount++;
            SetVisualEnabled(true);
        }

        public void Tick(float deltaTime)
        {
            if (!IsActive)
            {
                return;
            }

            remainingSeconds = Mathf.Max(0f, remainingSeconds - Mathf.Max(0f, deltaTime));
            if (remainingSeconds <= 0f)
            {
                HideCue();
            }
        }

        public void HideCue()
        {
            remainingSeconds = 0f;
            IsActive = false;
            SetVisualEnabled(false);
        }

        private void Awake()
        {
            cueRenderer ??= GetComponentInChildren<Renderer>();
            ApplyVisualMaterial();
            SetVisualEnabled(IsActive);
        }

        private void ApplyVisualMaterial()
        {
            Renderer renderer = cueRenderer ?? GetComponentInChildren<Renderer>();
            if (renderer == null)
            {
                return;
            }

            cueRenderer = renderer;
            Material material = renderer.sharedMaterial;
            if (material == null || material.shader == null)
            {
                material = new Material(Shader.Find("Standard"));
            }

            material.color = cueColor;
            renderer.sharedMaterial = material;
        }

        private void SetVisualEnabled(bool enabled)
        {
            Renderer renderer = cueRenderer ?? GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                cueRenderer = renderer;
                renderer.enabled = enabled;
            }
        }
    }
}
