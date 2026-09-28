using UnityEngine;

namespace MagicSchool.Engine
{
    /// <summary>
    /// Scene operations, wrapped so the plain (non-MonoBehaviour) classes don't call UnityEngine
    /// directly.
    /// </summary>
    public static class SceneHelper
    {
        // instantiate the new object, return it
        public static GameObject Instantiate(GameObject prefab) => Object.Instantiate(prefab);
    }
}
