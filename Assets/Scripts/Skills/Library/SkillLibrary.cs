using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicSchool.Skills
{
    // this is where the skill was registered in the game.
    public static class SkillLibrary
    {
        // a pair of HeroEnum & SkillDefinition
        // TemplateAction is a skill prefab used by hero, but hero don't know how this TemplateAction work.
        // How the TemplateAction work was put inside SkillDefinition.
        private static readonly Dictionary<SkillIdEnum, Func<TemplateActionRegistrySO, SkillDefinition>> Builders =
            new Dictionary<SkillIdEnum, Func<TemplateActionRegistrySO, SkillDefinition>>
            {
                { SkillIdEnum.Vharn  , VharnSkill.Build   },
                { SkillIdEnum.Sithra , SithraSkill.Build  },
                { SkillIdEnum.Bulwark, BulwarkSkill.Build },
                { SkillIdEnum.Roland , RolandSkill.Build  },
                { SkillIdEnum.Quatre , QuatreSkill.Build  },
                { SkillIdEnum.Solace , SolaceSkill.Build  },
                { SkillIdEnum.Vesper , VesperSkill.Build  },
                { SkillIdEnum.Pip    , PipSkill.Build     },
                { SkillIdEnum.Fang   , FangSkill.Build    },
                { SkillIdEnum.Lyra   , LyraSkill.Build    },
                { SkillIdEnum.Aldric , AldricSkill.Build  },
                { SkillIdEnum.Grimm  , GrimmSkill.Build   },
                { SkillIdEnum.Lumen  , LumenSkill.Build   },
                { SkillIdEnum.Reyn   , ReynSkill.Build    },
                { SkillIdEnum.Sparks , SparksSkill.Build  },
                { SkillIdEnum.Mira   , MiraSkill.Build    },
                { SkillIdEnum.Verity , VeritySkill.Build  },
            };

        /// Return a skill that match skillID's TemplateAction.
        public static SkillDefinition Resolve(SkillIdEnum skillID, TemplateActionRegistrySO registry)
        {
            // no skill at all is normal - a dummy has none
            if (skillID == SkillIdEnum.None) return null;

            if (!Builders.TryGetValue(skillID, out var build))
            {
                Debug.LogError($"[SkillLibrary] {skillID} doesn't exist in the Library.");
                return null;
            }

            if (registry == null)
            {
                Debug.LogError($"[SkillLibrary] {skillID} didn't registry in TemplateActionRegistrySO yet.");
                return null;
            }

            return build(registry);
        }
    }
}
