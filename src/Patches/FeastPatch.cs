using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using BlessedClasses.src.Blocks;

namespace BlessedClasses.src.Patches {

    /// <summary>
    /// gives meals cooked in metal pots a satiety bonus and slower spoilage.
    ///
    /// pattern:
    /// 1. hook into DoSmelt to detect when cooking completes in a metal pot
    /// 2. tag the ingredient content stacks with a feast attribute
    /// 3. hook into GetNutritionHealthMul to apply the satiety bonus when eaten
    /// 4. hook into GetTransitionRateMul to reduce spoilage for tagged food
    ///
    /// tagging content stacks (not the container) ensures both bonuses survive
    /// through every serving path: placed pot, held pot, crock storage, placed bowls, etc.
    ///
    /// spoilage reduction works in two cases:
    /// - content stacks in crocks/pots: the stack itself has the tag (checked directly)
    /// - meals in bowls: the bowl perishes as a whole (check its content stacks)
    ///
    /// buff comes from the pot itself.
    /// metal pot recipes are already gated by the "gourmand" trait.
    /// </summary>
    [HarmonyPatchCategory(BlessedClassesModSystem.FeastPatchCategory)]
    public class FeastPatch {

        /// <summary>
        /// runs AFTER cooking completes in any firepit/oven.
        /// if the output is a metal pot, tags all ingredient content stacks
        /// with the feast satiety bonus. these tags survive through all
        /// downstream serving paths because content stacks are preserved
        /// by reference through SetContents/GetContents and Clone.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(BlockCookingContainer), nameof(BlockCookingContainer.DoSmelt))]
        private static void FeastDoSmeltPostfix(
            IWorldAccessor world,
            ItemSlot outputSlot) {

            if (outputSlot?.Itemstack == null) return;
            if (outputSlot.Itemstack.Block is not BlockMetalPotCooked metalPot) return;

            ItemStack[] contents = metalPot.GetNonEmptyContents(world, outputSlot.Itemstack);
            if (contents == null) return;

            foreach (var stack in contents) {
                if (stack != null) {
                    stack.Attributes.SetFloat(
                        BlessedClassesModSystem.FeastPotMealAttribute,
                        BlessedClassesModSystem.FeastPotSatietyBonus);
                }
            }
        }

        /// <summary>
        /// nutrition bonus when eating a meal with tagged content stacks.
        /// runs AFTER BlockMeal.GetNutritionHealthMul returns.
        /// checks the meal's ingredient stacks for the feast tag.
        /// applies the bonus once regardless of how many ingredients are tagged.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(BlockMeal), nameof(BlockMeal.GetNutritionHealthMul))]
        private static void FeastMealNutritionPostfix(
            BlockMeal __instance,
            ref float[] __result,
            BlockPos pos,
            ItemSlot slot,
            EntityAgent forEntity) {

            if (slot?.Itemstack == null) return;

            IWorldAccessor world = forEntity?.World ?? BlessedClassesModSystem.Api?.World;
            if (world == null) return;

            ItemStack[] contents = __instance.GetNonEmptyContents(world, slot.Itemstack);
            if (contents == null || contents.Length == 0) return;

            foreach (var stack in contents) {
                if (stack?.Attributes?.HasAttribute(BlessedClassesModSystem.FeastPotMealAttribute) == true) {
                    float bonus = stack.Attributes.GetFloat(
                        BlessedClassesModSystem.FeastPotMealAttribute,
                        1f);
                    __result[0] *= bonus;
                    break; // apply bonus once, not per ingredient
                }
            }
        }

        /// <summary>
        /// persistent spoilage reduction for food cooked in metal pots.
        /// runs AFTER CollectibleObject.GetTransitionRateMul returns.
        ///
        /// handles two cases:
        /// 1. the item itself has the feast tag (content stack inside a crock or pot)
        /// 2. the item is a bowl whose content stacks have the feast tag
        ///
        /// applies once per item regardless of how many ingredients are tagged.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.GetTransitionRateMul))]
        private static void FeastSpoilagePostfix(
            ref float __result,
            IWorldAccessor world,
            ItemSlot inSlot,
            EnumTransitionType transType) {

            if (transType != EnumTransitionType.Perish) return;
            if (inSlot?.Itemstack == null) return;

            // case 1: the item itself has the feast tag
            // (content stack inside a crock, pot, or other container)
            if (inSlot.Itemstack.Attributes?.HasAttribute(BlessedClassesModSystem.FeastPotMealAttribute) == true) {
                __result *= BlessedClassesModSystem.FeastPotPerishMultiplier;
                return;
            }

            // case 2: the item is a meal/container with feast-tagged content stacks
            // (bowl of food where the meal itself perishes as a whole)
            if (inSlot.Itemstack.Block is BlockContainer container) {
                ItemStack[] contents = container.GetNonEmptyContents(world, inSlot.Itemstack);
                if (contents == null) return;

                foreach (var stack in contents) {
                    if (stack?.Attributes?.HasAttribute(BlessedClassesModSystem.FeastPotMealAttribute) == true) {
                        __result *= BlessedClassesModSystem.FeastPotPerishMultiplier;
                        return;
                    }
                }
            }
        }
    }
}
