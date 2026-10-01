using System;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    /// Everything a template action can call back to the skill that played it. Read each action for more detail.
    public class TemplateActionCallbacks
    {
        public static readonly TemplateActionCallbacks None = new TemplateActionCallbacks();

        // a template action die (destroyed).
        // if there's template action wait for OnExpired, play that action next.
        public Action<Vector3> OnExpired;  
        
        // a "projectile" landed on something. 
        // if there's template action wait for OnHit, play that action next.
        public Action<Vector3> OnHit;                
        
        // a template action hit on something.
        // report who it hit.
        public Action<ICombatant> OnSkillHit;       
        
        // add new action
        // ...
    }
}
