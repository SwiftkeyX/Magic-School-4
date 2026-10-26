using System;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    /// Everything a template action can call back to the skill that played it. Read each action for more detail.
    public class TemplateActionCallbacks
    {
        public static readonly TemplateActionCallbacks None = new TemplateActionCallbacks();

        public Action<Vector3> OnExpired;  
        
        public Action<Vector3> OnHit;                
        
        public Action<ICombatant> OnSkillHit;       
    }
}
