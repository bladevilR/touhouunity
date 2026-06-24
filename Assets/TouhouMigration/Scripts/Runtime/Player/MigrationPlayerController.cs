using UnityEngine;
using TouhouMigration.Runtime.Cooking;
using TouhouMigration.Runtime.UI;

namespace TouhouMigration.Runtime.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class MigrationPlayerController : MonoBehaviour
    {
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float runMultiplier = 1.5f;
        [SerializeField] private float turnSpeedDegrees = 540f;
        [SerializeField] private float gravity = -24f;
        [SerializeField] private float jumpHeight = 1.4f;
        [SerializeField] private float dashCooldown = 0.75f;

        private CharacterController characterController;
        private CookingBuffService cookingBuffService;
        private float verticalVelocity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        private void Start()
        {
            if (cookingBuffService == null)
            {
                BindCookingBuffs(MigrationGlobalUiController.FindCookingBuffService());
            }
        }

        private void Update()
        {
            if (MigrationGlobalUiController.IsGameplayInputBlocked())
            {
                return;
            }

            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            input = Vector2.ClampMagnitude(input, 1f);

            Vector3 movement = new Vector3(input.x, 0f, input.y);
            if (movement.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(movement, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    turnSpeedDegrees * Time.deltaTime);
            }

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (characterController.isGrounded && Input.GetButtonDown("Jump"))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            verticalVelocity += gravity * Time.deltaTime;

            float speed = Input.GetKey(KeyCode.LeftShift) ? GetModifiedRunSpeed() : GetModifiedWalkSpeed();
            Vector3 velocity = movement * speed;
            velocity.y = verticalVelocity;

            characterController.Move(velocity * Time.deltaTime);
        }

        public void BindCookingBuffs(CookingBuffService buffs)
        {
            cookingBuffService = buffs;
        }

        public float GetModifiedWalkSpeed()
        {
            return walkSpeed * GetSpeedMultiplier();
        }

        public float GetModifiedRunSpeed()
        {
            return walkSpeed * runMultiplier * GetSpeedMultiplier();
        }

        public float GetModifiedDashCooldown()
        {
            float offset = cookingBuffService?.GetDashCooldownOffset() ?? 0f;
            return Mathf.Max(0.1f, dashCooldown - offset);
        }

        public float GetModifiedDashDistanceMultiplier()
        {
            float multiplier = 1f;
            if (cookingBuffService != null && cookingBuffService.HasSpecialEffect("dash_distance_15"))
            {
                multiplier *= 1.15f;
            }

            if (cookingBuffService != null && cookingBuffService.HasSpecialEffect("dash_bonus_100"))
            {
                multiplier *= 1.3f;
            }

            return multiplier;
        }

        public float GetModifiedJumpHeight()
        {
            if (cookingBuffService != null &&
                (cookingBuffService.HasSpecialEffect("jump_boost_20") ||
                 cookingBuffService.HasDrinkEffect("jump_boost_20")))
            {
                return jumpHeight * 1.2f;
            }

            return jumpHeight;
        }

        public float GetModifiedAttackDamage(float baseDamage, string attackType)
        {
            float damage = baseDamage * GetDamageMultiplier();
            string normalizedAttackType = string.IsNullOrWhiteSpace(attackType)
                ? string.Empty
                : attackType.Trim().ToLowerInvariant();

            if (cookingBuffService != null)
            {
                if (normalizedAttackType == "heavy" &&
                    cookingBuffService.IsThresholdActive("atk", 6))
                {
                    damage *= 1.08f;
                }

                if (normalizedAttackType == "skill" &&
                    cookingBuffService.IsThresholdActive("spi", 6))
                {
                    damage *= 1.05f;
                }

                if (normalizedAttackType == "heavy" &&
                    cookingBuffService.IsThresholdActive("atk", 10))
                {
                    damage *= 1.08f;
                }
            }

            return damage;
        }

        public float GetDamageMultiplier()
        {
            return cookingBuffService?.GetDamageMultiplier() ?? 1f;
        }

        public float GetDamageReduction()
        {
            return cookingBuffService?.GetDamageReduction() ?? 0f;
        }

        public float GetModifiedSpiritChargeMultiplier()
        {
            return cookingBuffService?.GetSpiritChargeMultiplier() ?? 1f;
        }

        public bool HasCookingSpecialEffect(string effectId)
        {
            return cookingBuffService != null &&
                (cookingBuffService.HasSpecialEffect(effectId) || cookingBuffService.HasDrinkEffect(effectId));
        }

        private float GetSpeedMultiplier()
        {
            return cookingBuffService?.GetSpeedMultiplier() ?? 1f;
        }
    }
}
