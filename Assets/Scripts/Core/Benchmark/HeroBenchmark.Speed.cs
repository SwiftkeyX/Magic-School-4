using System.Collections.Generic;
using UnityEngine;

namespace MagicSchool.Core.Benchmark
{
    // speed: fixed steps, no frame cap, nothing drawn to increase render speed
    public partial class HeroBenchmark
    {
        private float _savedTimeScale, _savedCaptureDelta;
        private int _savedVSync, _savedFrameRate;
        private readonly List<Camera> _hiddenCameras = new List<Camera>();
        private readonly List<VisualElementDisplay> _hiddenUI = new List<VisualElementDisplay>();
        private bool _fast;

        private void GoFast()
        {
            _savedTimeScale = Time.timeScale;
            _savedCaptureDelta = Time.captureDeltaTime;
            _savedVSync = QualitySettings.vSyncCount;
            _savedFrameRate = Application.targetFrameRate;

            Time.timeScale = 1f;
            Time.captureDeltaTime = TimeStep;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;

            // drawing is most of a frame's cost, and nobody is watching
            foreach (Camera cam in Camera.allCameras)
            {
                if (!cam.enabled) continue;
                cam.enabled = false;
                _hiddenCameras.Add(cam);
            }

            foreach (UnityEngine.UIElements.UIDocument doc in FindObjectsByType<UnityEngine.UIElements.UIDocument>(FindObjectsInactive.Exclude))
            {
                if (doc.rootVisualElement == null) continue;
                _hiddenUI.Add(new VisualElementDisplay(doc.rootVisualElement));
                doc.rootVisualElement.style.display = UnityEngine.UIElements.DisplayStyle.None;
            }

            _fast = true;
        }

        private void GoNormal()
        {
            if (!_fast) return;

            Time.timeScale = _savedTimeScale;
            Time.captureDeltaTime = _savedCaptureDelta;
            QualitySettings.vSyncCount = _savedVSync;
            Application.targetFrameRate = _savedFrameRate;

            foreach (Camera cam in _hiddenCameras) if (cam != null) cam.enabled = true;
            foreach (VisualElementDisplay hidden in _hiddenUI) hidden.Restore();
            _hiddenCameras.Clear();
            _hiddenUI.Clear();

            _fast = false;
        }

        // a run cut short (Play mode stopped mid-way) must not leave the clock fixed or the screen dark
        private void OnDestroy() => GoNormal();

        private readonly struct VisualElementDisplay
        {
            private readonly UnityEngine.UIElements.VisualElement _element;
            private readonly UnityEngine.UIElements.StyleEnum<UnityEngine.UIElements.DisplayStyle> _display;

            public VisualElementDisplay(UnityEngine.UIElements.VisualElement element)
            {
                _element = element;
                _display = element.style.display;
            }

            public void Restore()
            {
                if (_element != null) _element.style.display = _display;
            }
        }
    }
}
