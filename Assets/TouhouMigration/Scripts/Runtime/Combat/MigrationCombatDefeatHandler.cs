using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    [RequireComponent(typeof(MigrationCombatTargetBehaviour))]
    public sealed class MigrationCombatDefeatHandler : MonoBehaviour
    {
        [SerializeField] private bool disableCollidersOnDefeat = true;
        [SerializeField] private bool disableRenderersOnDefeat = true;
        [SerializeField] private float defeatDelaySeconds;
        [SerializeField] private bool deathFeedbackEnabled;
        [SerializeField] private float deathFeedbackRadius = 0.45f;
        [SerializeField] private Color deathFeedbackColor = new Color(1f, 0.35f, 0.12f, 1f);
        [SerializeField] private GameObject deathFeedbackPrefab;

        private MigrationCombatTargetBehaviour target;
        private bool handled;
        private bool defeatPending;
        private float defeatDelayRemaining;
        private float deathFeedbackElapsed;
        private GameObject deathFeedbackObject;
        private ParticleSystem deathFeedbackParticles;

        public int HandledDefeatCount { get; private set; }
        public int PendingDefeatCount { get; private set; }
        public int DeathFeedbackStartedCount { get; private set; }
        public bool IsDefeatPending => defeatPending;
        public float DefeatDelayRemaining => defeatDelayRemaining;
        public float DefeatDelaySeconds => defeatDelaySeconds;
        public bool DeathFeedbackEnabled => deathFeedbackEnabled;
        public bool HasDeathFeedbackPrefab => deathFeedbackPrefab != null;
        public bool HasActiveDeathFeedback { get; private set; }
        public float DeathFeedbackProgress => defeatDelaySeconds <= 0f
            ? (HasActiveDeathFeedback ? 1f : 0f)
            : Mathf.Clamp01(deathFeedbackElapsed / defeatDelaySeconds);

        public void ConfigureDefeatDelay(float delaySeconds)
        {
            defeatDelaySeconds = Mathf.Max(0f, delaySeconds);
            defeatDelayRemaining = 0f;
            defeatPending = false;
        }

        public void ConfigureDeathFeedback(float radius, Color color)
        {
            deathFeedbackEnabled = true;
            deathFeedbackRadius = Mathf.Max(0.05f, radius);
            deathFeedbackColor = color;
            EnsureDeathFeedback();
            SetDeathFeedbackActive(false);
        }

        public void ConfigureDeathFeedbackPrefab(GameObject prefab)
        {
            deathFeedbackPrefab = prefab;
            deathFeedbackEnabled = true;
            EnsureDeathFeedback();
            SetDeathFeedbackActive(false);
        }

        public void BindTarget(MigrationCombatTargetBehaviour target)
        {
            if (this.target == target)
            {
                return;
            }

            if (this.target != null)
            {
                this.target.Defeated -= OnTargetDefeated;
            }

            this.target = target;
            if (this.target != null)
            {
                this.target.Defeated += OnTargetDefeated;
            }
        }

        private void Awake()
        {
            target = GetComponent<MigrationCombatTargetBehaviour>();
        }

        private void OnEnable()
        {
            BindTarget(target != null ? target : GetComponent<MigrationCombatTargetBehaviour>());
        }

        private void OnDisable()
        {
            if (target != null)
            {
                target.Defeated -= OnTargetDefeated;
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (!defeatPending || handled)
            {
                return;
            }

            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            if (HasActiveDeathFeedback)
            {
                deathFeedbackElapsed = Mathf.Min(defeatDelaySeconds, deathFeedbackElapsed + safeDeltaTime);
            }

            defeatDelayRemaining = Mathf.Max(0f, defeatDelayRemaining - safeDeltaTime);
            if (defeatDelayRemaining <= 0.0001f)
            {
                HandleDefeat();
            }
        }

        private void OnTargetDefeated(CombatBridgeResult result)
        {
            if (handled || defeatPending)
            {
                return;
            }

            DisableDamageSources();
            StartDeathFeedback();

            if (defeatDelaySeconds > 0f)
            {
                defeatPending = true;
                defeatDelayRemaining = defeatDelaySeconds;
                PendingDefeatCount++;
                return;
            }

            HandleDefeat();
        }

        private void HandleDefeat()
        {
            if (handled)
            {
                return;
            }

            defeatPending = false;
            defeatDelayRemaining = 0f;
            handled = true;
            HandledDefeatCount++;
            StopDeathFeedback();

            if (disableCollidersOnDefeat)
            {
                foreach (Collider collider in GetComponentsInChildren<Collider>())
                {
                    collider.enabled = false;
                }
            }

            if (disableRenderersOnDefeat)
            {
                foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
                {
                    renderer.enabled = false;
                }
            }
        }

        private void DisableDamageSources()
        {
            foreach (MigrationEnemyDamageSource damageSource in GetComponentsInChildren<MigrationEnemyDamageSource>(true))
            {
                damageSource.SetWindowActive(false);
            }
        }

        private void StartDeathFeedback()
        {
            if (!deathFeedbackEnabled)
            {
                return;
            }

            EnsureDeathFeedback();
            deathFeedbackElapsed = 0f;
            DeathFeedbackStartedCount++;
            SetDeathFeedbackActive(true);
            deathFeedbackParticles.Clear(true);
            deathFeedbackParticles.Play(true);
        }

        private void StopDeathFeedback()
        {
            if (!HasActiveDeathFeedback)
            {
                return;
            }

            if (deathFeedbackParticles != null)
            {
                deathFeedbackParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            SetDeathFeedbackActive(false);
        }

        private void EnsureDeathFeedback()
        {
            if (deathFeedbackObject == null)
            {
                Transform existing = transform.Find("DeathFeedback");
                if (existing != null)
                {
                    deathFeedbackObject = existing.gameObject;
                }
                else if (deathFeedbackPrefab != null)
                {
                    deathFeedbackObject = Instantiate(deathFeedbackPrefab);
                    deathFeedbackObject.name = "DeathFeedback";
                }
                else
                {
                    deathFeedbackObject = new GameObject("DeathFeedback");
                }

                deathFeedbackObject.transform.SetParent(transform, false);
                deathFeedbackObject.transform.localPosition = Vector3.up * 0.35f;
            }

            deathFeedbackParticles = deathFeedbackObject.GetComponent<ParticleSystem>();
            if (deathFeedbackParticles == null)
            {
                deathFeedbackParticles = deathFeedbackObject.AddComponent<ParticleSystem>();
            }

            ParticleSystem.MainModule main = deathFeedbackParticles.main;
            main.loop = false;
            main.duration = Mathf.Max(0.2f, defeatDelaySeconds);
            main.startLifetime = Mathf.Max(0.2f, defeatDelaySeconds * 0.75f);
            main.startSpeed = deathFeedbackRadius * 2.4f;
            main.startSize = deathFeedbackRadius;
            main.startColor = deathFeedbackColor;

            ParticleSystem.EmissionModule emission = deathFeedbackParticles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)16)
            });

            ParticleSystem.ShapeModule shape = deathFeedbackParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = deathFeedbackRadius;

            ParticleSystemRenderer particleRenderer = deathFeedbackObject.GetComponent<ParticleSystemRenderer>();
            if (particleRenderer != null && particleRenderer.sharedMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    Material material = new Material(shader);
                    material.color = deathFeedbackColor;
                    particleRenderer.sharedMaterial = material;
                }
            }
        }

        private void SetDeathFeedbackActive(bool active)
        {
            HasActiveDeathFeedback = active;
            if (deathFeedbackObject != null)
            {
                deathFeedbackObject.SetActive(active);
            }
        }
    }
}
