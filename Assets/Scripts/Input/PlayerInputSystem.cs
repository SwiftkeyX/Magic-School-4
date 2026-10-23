using UnityEngine;
using UnityEngine.InputSystem;

namespace MagicSchool.Input
{
    // Player can: 
    // 1) left click to drag hero/items - or click one once to pick it up, and once more to put it down (Esc puts it back).
    // 2) right click to inspect hero/items - or, while holding a hero, to rotate its align pattern.
    // 3) spacebar to interact with game's stage e.g. start combat, pause and go on during it, retry, continue to next stage
    // 4) R to quick-retry
    // 5) N (dev tool) to skip the stage as won
    // 6) hold Shift while hovering a placed hero, in Preparation, to see who it goes for and the way it walks instead of its marks;
    //    in a fight, any hero (a debug tool)
    public static class PlayerInputSystem
    {
        public static bool IsPointerDown => Mouse.current.leftButton.isPressed;
        public static bool DragPressedThisFrame => Mouse.current.leftButton.wasPressedThisFrame;
        public static bool DragReleasedThisFrame => Mouse.current.leftButton.wasReleasedThisFrame;
        public static bool SpacePressedThisFrame => Keyboard.current.spaceKey.wasPressedThisFrame;
        public static bool InspectPressedThisFrame => Mouse.current.rightButton.wasPressedThisFrame;
        public static bool RestartPressedThisFrame => Keyboard.current.rKey.wasPressedThisFrame;
        public static bool SkipStagePressedThisFrame => Keyboard.current.nKey.wasPressedThisFrame;
        public static bool CancelPressedThisFrame => Keyboard.current.escapeKey.wasPressedThisFrame;
        public static bool TargetViewHeld => Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
        public static bool FormulaViewHeld => Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed;

        // adjust game's speed using numkey
        public static int SpeedPressedThisFrame()
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) return 1;
            if (Keyboard.current.digit2Key.wasPressedThisFrame) return 2;
            if (Keyboard.current.digit3Key.wasPressedThisFrame) return 3;
            if (Keyboard.current.digit9Key.wasPressedThisFrame) return 9;

            return 0;
        }

        public static Vector2 PointerScreenPosition => Mouse.current.position.ReadValue();

        // Converts the current mouse position to a world point on the plane facing the given camera.
        public static Vector3 GetMouseWorldPosition(Camera cam)
        {
            Vector3 screenPos = Mouse.current.position.ReadValue();
            screenPos.z = -cam.transform.position.z;
            return cam.ScreenToWorldPoint(screenPos);
        }
    }
}
