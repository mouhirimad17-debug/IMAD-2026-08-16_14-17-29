using UnityEngine;

namespace PrankMansion.Systems
{
    // Part 13.1: "أصوات الاصطدام المتنوعة حسب المادة (خشب، معدن، زجاج، جسم بشري):
    // تُحدَّد حسب نوع المادة الأساسية المُعرَّفة على كل مجسم عبر وسم بسيط" - the
    // simple tag itself. Human covers player-vs-environment hard collisions;
    // Wood/Metal/Glass cover props. DECISION: the master document's Part 4 tables
    // don't assign a material to each of the ~100 individual prop entries, so this
    // stage tags at the CLASS level (CarryableObject/FallableProp/PushableProp
    // default to Wood, the most common furniture material) rather than opening
    // every prop's importer to hand-assign one of four materials per item -
    // logged as a reasonable Law 21.2 call. Individual props can still override
    // their own `material` field once/if a finer per-item pass is ever done.
    public enum ImpactMaterial { Wood, Metal, Glass, Human }

    /// <summary>
    /// One placeholder tone per material (Law 0.2/0.5 - no real impact SFX assets
    /// exist, so each material gets a distinct pitch/duration via PlaceholderAudio,
    /// generated once and cached rather than regenerated on every collision).
    /// </summary>
    public static class ImpactSoundLibrary
    {
        // DECISION: pitches chosen to be intuitively distinguishable (low/dull for
        // wood, sharp/metallic-high for metal and glass, warm-mid for a body
        // impact) - no exact Hz values are specified anywhere in the document.
        private static AudioClip wood, metal, glass, human;

        public static AudioClip GetClip(ImpactMaterial material)
        {
            switch (material)
            {
                case ImpactMaterial.Wood:
                    return wood ??= PlaceholderAudio.GenerateTone("Placeholder_ImpactWood", 180f, 0.12f, 0.35f);
                case ImpactMaterial.Metal:
                    return metal ??= PlaceholderAudio.GenerateTone("Placeholder_ImpactMetal", 900f, 0.10f, 0.3f);
                case ImpactMaterial.Glass:
                    return glass ??= PlaceholderAudio.GenerateTone("Placeholder_ImpactGlass", 1400f, 0.08f, 0.3f);
                default:
                    return human ??= PlaceholderAudio.GenerateTone("Placeholder_ImpactHuman", 110f, 0.15f, 0.3f);
            }
        }
    }
}
