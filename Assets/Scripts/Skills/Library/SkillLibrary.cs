using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicSchool.Skills
{
    // this is where the skill was registered in the game.
    public static class SkillLibrary
    {
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
                { SkillIdEnum.Bandit        , BanditSkill.Build         },
                { SkillIdEnum.FrostWitch    , FrostWitchSkill.Build     },
                { SkillIdEnum.Swordsman     , SwordsmanSkill.Build      },
                { SkillIdEnum.Lich          , LichSkill.Build           },
                { SkillIdEnum.Dragon        , DragonSkill.Build         },
                { SkillIdEnum.Ettin         , EttinSkill.Build          },
                { SkillIdEnum.Harpy         , HarpySkill.Build          },
                { SkillIdEnum.Husk          , HuskSkill.Build           },
                { SkillIdEnum.RockGolem     , RockGolemSkill.Build      },
                { SkillIdEnum.Templar       , TemplarSkill.Build        },
                { SkillIdEnum.Archangel     , ArchangelSkill.Build      },
                { SkillIdEnum.WarCleric     , WarClericSkill.Build      },
                { SkillIdEnum.Troll         , TrollSkill.Build          },
                { SkillIdEnum.Warlock       , WarlockSkill.Build        },
                { SkillIdEnum.Rogue         , RogueSkill.Build          },
                { SkillIdEnum.GoblinArcher  , GoblinArcherSkill.Build   },
                { SkillIdEnum.Fencer        , FencerSkill.Build         },
                { SkillIdEnum.Priest        , PriestSkill.Build         },

                { SkillIdEnum.Minotaur      , MinotaurSkill.Build       },
                { SkillIdEnum.BigSlime      , BigSlimeSkill.Build       },
                { SkillIdEnum.Basilisk      , BasiliskSkill.Build       },
                { SkillIdEnum.GoblinSlinger , GoblinSlingerSkill.Build  },
                { SkillIdEnum.KoboldSkirmisher, KoboldSkirmisherSkill.Build },
                { SkillIdEnum.MinotaurKing  , MinotaurKingSkill.Build   },
            };

        public static SkillDefinition Resolve(SkillIdEnum skillID, TemplateActionRegistrySO registry)
        {
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
