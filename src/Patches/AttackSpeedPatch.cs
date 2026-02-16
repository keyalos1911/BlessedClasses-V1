using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace BlessedClasses.src.Patches {

    /// <summary>
    /// modifies melee attack speed for players based on traits and/or held weapon.
    ///
    /// how it works:
    /// - patches StartAttack in CollectibleBehaviorAnimationAuthoritative
    /// - before the attack animation plays, looks up the AnimationMetaData
    ///   for the weapon's hit animation and modifies its AnimationSpeed
    /// - faster AnimationSpeed = faster attack cycle (both visually and mechanically,
    ///   since StepAttack gates re-attacking on animation completion)
    ///
    /// AnimationsByMetaCode is per-entity (cloned), so changes to one player
    /// don't affect others.
    /// </summary>
    [HarmonyPatch(typeof(CollectibleBehaviorAnimationAuthoritative))]
    [HarmonyPatchCategory(BlessedClassesModSystem.AttackSpeedPatchCategory)]
    public class AttackSpeedPatch {

        [HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleBehaviorAnimationAuthoritative.StartAttack))]
        private static void StartAttackPrefix(ItemSlot slot, EntityAgent byEntity) {
            if (byEntity is not EntityPlayer playerEntity) return;

            string anim = slot.Itemstack?.Collectible?.GetHeldTpHitAnimation(slot, byEntity);
            if (anim == null) return;

            if (!byEntity.Properties.Client.AnimationsByMetaCode.TryGetValue(anim, out var animdata)) return;

            // TODO: implement actual speed logic
            //
            // — check player trait:
            //   IPlayer player = playerEntity.Player;
            //   string classcode = player.Entity.WatchedAttributes.GetString("characterClass");
            //   CharacterClass charclass = player.Entity.Api.ModLoader
            //       .GetModSystem<CharacterSystem>()
            //       .characterClasses
            //       .FirstOrDefault(c => c.Code == classcode);
            //   if (charclass != null && charclass.Traits.Contains("placeholderTrait"))
            //       animdata.AnimationSpeed = 2.0f;
            //
            // — check weapon type:
            //   string weaponCode = slot.Itemstack?.Collectible?.Code?.Path ?? "";
            //   if (weaponCode.StartsWith("placeholderType-placeholderItem"))
            //       animdata.AnimationSpeed = 1.8f;
            //
            // — combine both:
            //   if (hasTrait && isDagger)
            //       animdata.AnimationSpeed = 2.5f;
        }
    }
}
