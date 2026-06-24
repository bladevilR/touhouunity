using System;
using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    public sealed class MigrationPhoenixGaugeRuntime
    {
        private float currentValue = 50f;
        private float maxValue = 300f;
        private float segmentSize = 100f;
        private float grazeSoftCapPerSecond = 45f;
        private float hitLoss = 20f;
        private float grazeWindowAmount;
        private float grazeWindowTimer;
        private int lastSegments = -1;

        public event Action<float, float, float, string> GaugeChanged;
        public event Action<int, int> SegmentChanged;

        public float CurrentValue => currentValue;
        public float MaxValue => maxValue;
        public float SegmentSize => segmentSize;
        public float GrazeSoftCapPerSecond => grazeSoftCapPerSecond;
        public float HitLoss => hitLoss;
        public int FilledSegments => Mathf.FloorToInt(currentValue / Mathf.Max(1f, segmentSize));
        public int SegmentCount => Mathf.CeilToInt(maxValue / Mathf.Max(1f, segmentSize));
        public float LastDelta { get; private set; }
        public string LastReason { get; private set; } = string.Empty;
        public int ChangeEventCount { get; private set; }
        public int SegmentEventCount { get; private set; }

        public void Configure(float maxValue, float segmentSize, float grazeSoftCapPerSecond, float hitLoss)
        {
            this.maxValue = Mathf.Max(1f, maxValue);
            this.segmentSize = Mathf.Max(1f, segmentSize);
            this.grazeSoftCapPerSecond = Mathf.Max(0f, grazeSoftCapPerSecond);
            this.hitLoss = Mathf.Max(0f, hitLoss);
            currentValue = Mathf.Clamp(currentValue, 0f, this.maxValue);
            lastSegments = FilledSegments;
        }

        public void Reset(float value = 50f)
        {
            currentValue = Mathf.Clamp(value, 0f, maxValue);
            grazeWindowAmount = 0f;
            grazeWindowTimer = 0f;
            lastSegments = FilledSegments;
            LastDelta = 0f;
            LastReason = "reset";
        }

        public void Tick(float deltaTime)
        {
            if (grazeWindowTimer <= 0f)
            {
                grazeWindowAmount = 0f;
                return;
            }

            grazeWindowTimer = Mathf.Max(0f, grazeWindowTimer - Mathf.Max(0f, deltaTime));
            if (grazeWindowTimer <= 0f)
            {
                grazeWindowAmount = 0f;
            }
        }

        public float AddAttack(float amount, string reason = "attack")
        {
            return AddValue(amount, reason);
        }

        public float AddGraze(float amount, string reason = "graze")
        {
            if (grazeWindowTimer <= 0f)
            {
                grazeWindowTimer = 1f;
                grazeWindowAmount = 0f;
            }

            float remaining = Mathf.Max(0f, grazeSoftCapPerSecond - grazeWindowAmount);
            float accepted = Mathf.Min(Mathf.Max(0f, amount), remaining);
            grazeWindowAmount += accepted;
            return AddValue(accepted, reason);
        }

        public float LoseOnHit(string reason = "hit")
        {
            return AddValue(-hitLoss, reason);
        }

        public bool CanSpend(float amount)
        {
            return currentValue + 0.001f >= Mathf.Max(0f, amount);
        }

        public bool Spend(float amount, string reason = "spend")
        {
            float resolved = Mathf.Max(0f, amount);
            if (!CanSpend(resolved))
            {
                return false;
            }

            AddValue(-resolved, reason);
            return true;
        }

        private float AddValue(float amount, string reason)
        {
            if (Mathf.Abs(amount) <= 0.0001f)
            {
                return 0f;
            }

            float before = currentValue;
            currentValue = Mathf.Clamp(currentValue + amount, 0f, maxValue);
            float applied = currentValue - before;
            if (Mathf.Abs(applied) <= 0.0001f)
            {
                return 0f;
            }

            LastDelta = applied;
            LastReason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            ChangeEventCount++;

            int segments = FilledSegments;
            if (segments != lastSegments)
            {
                lastSegments = segments;
                SegmentEventCount++;
                SegmentChanged?.Invoke(segments, SegmentCount);
            }

            GaugeChanged?.Invoke(currentValue, maxValue, applied, LastReason);
            return applied;
        }
    }
}
