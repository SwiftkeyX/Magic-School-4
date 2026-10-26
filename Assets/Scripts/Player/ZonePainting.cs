using UnityEngine;
using MagicSchool.Core;
using MagicSchool.Combat.Placements;
using MagicSchool.Input;

namespace MagicSchool.Player
{
    // Use with Stage editor (dev tool): painting the deploy zone with the pointer.
    // Click on an empty hex to toggle it to deploy zone.
    // Ticked after Dragging: a press that picked something up, or put a click-held one down, never paints.
    internal class ZonePainting
    {
        private readonly Camera _cam;
        private readonly Dragging _dragging;

        public ZonePainting(Camera cam, Dragging dragging)
        {
            _cam = cam;
            _dragging = dragging;
        }

        public void Tick()
        {
            StageEditor editor = GameManager.Instance.StageEditor;
            if (editor == null || !editor.CanEditNow) return;

            if (!PlayerInputSystem.DragPressedThisFrame) return;

            if (_dragging.IsHolding || _dragging.UsedPressThisFrame) return;

            Vector3 worldPos = PlayerInputSystem.GetMouseWorldPosition(_cam);
            Hex hex = Picker.At<Hex>(worldPos);

            if (hex != null) editor.TryToggleDeployHex(hex);
        }
    }
}
