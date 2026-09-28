using UnityEngine;
using UnityEngine.InputSystem;

namespace MagicSchool.Input
{
    // Player can: 
    // 1) left click to drag hero/items.
    // 2) right click to inspect hero/items.
    // 3) spacebar to interact with game's stage e.g. start combat, retry, continue to next stage
    // 4) R to quick-retry
    public static class PlayerInputSystem
    {
        public static bool IsPointerDown => Mouse.current.leftButton.isPressed;
        public static bool DragPressedThisFrame => Mouse.current.leftButton.wasPressedThisFrame;
        public static bool DragReleasedThisFrame => Mouse.current.leftButton.wasReleasedThisFrame;
        public static bool SpacePressedThisFrame => Keyboard.current.spaceKey.wasPressedThisFrame;
        public static bool InspectPressedThisFrame => Mouse.current.rightButton.wasPressedThisFrame;
        public static bool RestartPressedThisFrame => Keyboard.current.rKey.wasPressedThisFrame;

        // adjust game's speed using numkey
        public static int SpeedPressedThisFrame()
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) return 1;
            if (Keyboard.current.digit2Key.wasPressedThisFrame) return 2;
            if (Keyboard.current.digit3Key.wasPressedThisFrame) return 3;

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
