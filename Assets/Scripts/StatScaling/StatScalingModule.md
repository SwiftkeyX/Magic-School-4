# StatScaling Module - it is a module that build considering asmdef boundary
MagicSchool.StatScaling is a "leaf" module.
1) reference to MagicSchool.Contracts
2) was referenced by MagicSchool.Modifiers/Skills

It answers one question: given a list of StatRatio and a hero's stats, what is this worth.
That question belongs to no one in particular - a skill's damage asks it, a heal asks it, and a
modifier's bonus asks it - so it does not live with any of them.

The folder is StatScaling rather than Scaling because the namespace follows the folder, and a
namespace MagicSchool.Scaling holding a class Scaling cannot be used unqualified by anyone.
