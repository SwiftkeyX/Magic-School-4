using System;
using UnityEngine;

namespace MagicSchool.Skills
{
    [Serializable]
    internal struct Sticky
    {
        [SerializeField] internal bool IsSticky;
        internal Transform Source;      // what to sit on top of - null once it stops existing
    }
}