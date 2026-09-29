using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicSchool.Skills
{
    // this is where the skill was registered in the game.
    public static class SkillLibrary
    {
        // a pair of SkillIdEnum & SkillDefinition
        // TemplateAction is a skill prefab used by hero, but hero don't know how this TemplateAction work.
        // How the TemplateAction work was put inside SkillDefinition.
        private static readonly Dictionary<SkillIdEnum, Func<TemplateActionRegistrySO, SkillDefinition>> Builders =
            new Dictionary<SkillIdEnum, Func<TemplateActionRegistrySO, SkillDefinition>>
            {
                { SkillIdEnum.Werewolf      , WerewolfSkill.Build       },
                { SkillIdEnum.Naga          , NagaSkill.Build           },
                { SkillIdEnum.ShieldKnight  , ShieldKnightSkill.Build   },
                { SkillIdEnum.OrcBlademaster, OrcBlademasterSkill.Build },
                { SkillIdEnum.Ranger        , RangerSkill.Build         },
                { SkillIdEnum.Elf           , ElfSkill.Build            },
                { SkillIdEnum.DireWolf      , DireWolfSkill.Build       },
                { SkillIdEnum.Dryad         , DryadSkill.Build          },
                { SkillIdEnum.Knight        , KnightSkill.Build         },
                { SkillIdEnum.Centaur       , CentaurSkill.Build        },
                { SkillIdEnum.Dwarf         , DwarfSkill.Build          },
                { SkillIdEnum.SkeletonArcher, SkeletonArcherSkill.Build },
                { SkillIdEnum.Imp           , ImpSkill.Build            },
                { SkillIdEnum.Reaper        , ReaperSkill.Build         },
                { SkillIdEnum.Myconid       , MyconidSkill.Build        },
                { SkillIdEnum.Monk          , MonkSkill.Build           },
                { SkillIdEnum.Blacksmith    , BlacksmithSkill.Build     },
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
