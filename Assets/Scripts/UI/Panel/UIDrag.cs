using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MagicSchool.UI
{
    /// <summary>
    /// Makes a UI element draggable: while the UI is held, a ghost follows the pointer, and on release the
    /// owner is told where it was dropped.  
    ///     canPickUp   may this element be picked up right now?         e.g. not an empty shop slot
    ///     onDrop      it was released here (panel position), now what? e.g. outside the shop = buy
    /// A lot of comment since I never use those event before
    /// </summary>
    // Only the left button drags, so a right-click inspects instead. CapturePointer keeps the move and up events
    // coming after the pointer leaves the element.
    internal class UIDrag
    {
        private readonly VisualElement _ghost;
        private readonly VisualElement _layer;
        private Vector2 _ghostSize;
        private bool _isDragging;

        public UIDrag(VisualElement layer, VisualTreeAsset ghostAsset)
        {
            _layer = layer;
            _ghost = PanelMounter.CloneTemplateRoot(ghostAsset);
        }

        public void MakeDraggable(VisualElement element, Func<bool> canPickUp, Action<Vector2> onDrop)
        {
            OnClick(element, canPickUp);

            OnMove(element);

            OnRelease(element, onDrop);
        }

        private void OnClick(VisualElement element, Func<bool> canPickUp)
        {
            element.RegisterCallback<PointerDownEvent>(pointer =>
            {
                bool isLeftButton = pointer.button != 0;
                if (isLeftButton) return;

                if (!canPickUp()) return;

                _isDragging = true;

                element.CapturePointer(pointer.pointerId);

                ShowGhostAs(element);
                _layer.Add(_ghost);

                MoveGhostTo(pointer.position);
            });
        }

        private void OnMove(VisualElement element)
        {
            element.RegisterCallback<PointerMoveEvent>(pointer =>
            {
                if (!_isDragging) return;

                MoveGhostTo(pointer.position);
            });
        }

        private void OnRelease(VisualElement element, Action<Vector2> onDrop)
        {
            element.RegisterCallback<PointerUpEvent>(pointer =>
            {
                if (!_isDragging) return;

                _isDragging = false;

                element.ReleasePointer(pointer.pointerId);

                _ghost.RemoveFromHierarchy();

                onDrop(pointer.position);
            });
        } 

        private void ShowGhostAs(VisualElement element)
        {
            Label elementLabel = element.Q<Label>();
            Label ghostLabel = _ghost.Q<Label>();
            if (elementLabel != null && ghostLabel != null) ghostLabel.text = elementLabel.text;

            _ghostSize = new Vector2(element.resolvedStyle.width, element.resolvedStyle.height);
            _ghost.style.width = _ghostSize.x;
            _ghost.style.height = _ghostSize.y;
        }

        private void MoveGhostTo(Vector2 panelPosition)
        {
            _ghost.style.left = panelPosition.x - _ghostSize.x / 2f;
            _ghost.style.top = panelPosition.y - _ghostSize.y / 2f;
        }
    }
}
