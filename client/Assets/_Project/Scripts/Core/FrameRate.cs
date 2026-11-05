using UnityEngine;

namespace StillWater.Core
{
    /// <summary>Android caps games at 30 fps unless asked; jumps read much smoother at 60.</summary>
    public static class FrameRate
    {
        private const int Target = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply() => Application.targetFrameRate = Target;
    }
}
