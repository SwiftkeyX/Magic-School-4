using System;
using System.Collections.Generic;

namespace MagicSchool.Skills
{
    /// <summary>
    /// It exists so a SkillFactory is easier to read:
    /// </summary>
    public readonly struct PartSet
    {
        private readonly SkillPart[] _parts;

        public IReadOnlyList<SkillPart> Parts => _parts ?? Array.Empty<SkillPart>();

        public PartSet(params SkillPart[] parts)
        {
            _parts = parts;
        }

        public static implicit operator PartSet(SkillPart part) => new PartSet(part);
    }
}
