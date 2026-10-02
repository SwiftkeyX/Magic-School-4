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
    internal class UIDrag
    {
        private readonly VisualElement _ghost;      // the element that visually got drag together with your mouse e.g. hero sprite.
        private readonly VisualElement _layer;      // the layer the ghost was added to. to make the ghost displayed above others.
        private Vector2 _ghostSize;
        private bool _isDragging;

        public UIDrag(VisualElement layer, VisualTreeAsset ghostAsset)
        {
            _layer = layer;
            _ghost = PanelMounter.CloneTemplateRoot(ghostAsset);
        }

        public void MakeDraggable(VisualElement element, Func<bool> canPickUp, Action<Vector2> onDrop)
        {
            // when click on the element, spawn ghost, move ghost to click point
            OnClick(element, canPickUp);

            // when hold on the element, make ghost follow the pointer, create dragging logic visually
            OnMove(element);

            // when your mouse release from holding, tell the owner where it was released
            OnRelease(element, onDrop);
        }

        // ============================== Pointer Event ====================================
        private void OnClick(VisualElement element, Func<bool> canPickUp)
        {
            // PointerDownEvent = when your mouse hold inside the element's bound
            // pointer = the point you start clicking
            element.RegisterCallback<PointerDownEvent>(pointer =>
            {
                // try pick up
                // e.g.     shop panel = a shop offer can be picked up. 
                if (!canPickUp()) return;

                _isDragging = true;

                // CapturePointer = All event from the element will continue working even though the "pointer" move out of bound
                element.CapturePointer(pointer.pointerId);

                // add ghost to the layer, this make ghost visible
                ShowGhostAs(element);
                _layer.Add(_ghost);

                // move ghost to the point you just click
                MoveGhostTo(pointer.position);
            });
        }

        private void OnMove(VisualElement element)
        {
            // PointerMoveEvent = when you move your mouse inside the element's bound
            // if your mouse exit the bound, the event won't fired, BUT we use CapturePointer() so we can actually move out of bound
            // pointer = the point you holding your mouse, so this pointer can move
            element.RegisterCallback<PointerMoveEvent>(pointer =>
            {
                if (!_isDragging) return;

                // move ghost using pointer
                MoveGhostTo(pointer.position);
            });
        }

        private void OnRelease(VisualElement element, Action<Vector2> onDrop)
        {
            // PointerUpEvent = when your mouse release from holding
            // pointer = the point you release your mouse
            element.RegisterCallback<PointerUpEvent>(pointer =>
            {
                // nothing was picked up, so nothing is dropped
                if (!_isDragging) return;

                _isDragging = false;

                // undo the CapturePointer
                element.ReleasePointer(pointer.pointerId);

                // release ghost from the layer, this make ghost go invisible
                _ghost.RemoveFromHierarchy();

                // drop what is held
                // e.g.     shop panel = drag a shop offer, drop could mean buy      
                onDrop(pointer.position);
            });
        } 

        // ============================== Ghost ====================================
        // show ghost as the same to the dragging element: its label, and its size
        private void ShowGhostAs(VisualElement element)
        {
            Label elementLabel = element.Q<Label>();
            Label ghostLabel = _ghost.Q<Label>();
            if (elementLabel != null && ghostLabel != null) ghostLabel.text = elementLabel.text;

            _ghostSize = new Vector2(element.resolvedStyle.width, element.resolvedStyle.height);
            _ghost.style.width = _ghostSize.x;
            _ghost.style.height = _ghostSize.y;
        }

        // ghost copying the mouse position using "screen panel method"
        private void MoveGhostTo(Vector2 panelPosition)
        {
            _ghost.style.left = panelPosition.x - _ghostSize.x / 2f;
            _ghost.style.top = panelPosition.y - _ghostSize.y / 2f;
        }
    }
}
