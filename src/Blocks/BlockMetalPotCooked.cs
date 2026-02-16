using Vintagestory.GameContent;

namespace BlessedClasses.src.Blocks {

    /// <summary>
    /// cooked variant of the metal cooking pot.
    /// serves as a marker type so the DoSmelt postfix can identify meals
    /// cooked in metal pots and tag their content stacks with the feast buff.
    ///
    /// spoilage reduction is handled persistently via content stack tags
    /// (in FeastPatch.FeastSpoilagePostfix), not via container modifiers,
    /// so the bonus follows the food through bowls, crocks, etc.
    /// </summary>
    public class BlockMetalPotCooked : BlockCookedContainer { }
}
