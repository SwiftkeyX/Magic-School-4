using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MagicSchool.Core.Benchmark
{
    /// <summary>
    /// Run a benchmark as fast as the machine allows: fixed steps (so a second of fight is always the same 30 steps),
    /// no frame cap, no vSync, and nothing drawn - then put it all back.
    /// Shared by HeroBenchmark and TeamCompBenchmark.
    /// </summary>
    // Drawing is most of a frame's cost and nobody is watching, so cameras, UI and the heroes' world-space bars are
    // off. Restore is also called when a run is cut short (Play mode stopped mid-way), so the clock is never left
    // fixed or the screen dark.
    internal class BenchmarkSpeed
    {
        private float _savedTimeScale, _savedCaptureDelta;
        private int _savedVSync, _savedFrameRate;
        private readonly List<Camera> _hiddenCameras = new List<Camera>();
        private readonly List<(VisualElement element, StyleEnum<DisplayStyle> display)> _hiddenUI = new List<(VisualElement, StyleEnum<DisplayStyle>)>();
        private bool _fast;

        public void GoFast(float timeStep)
        {
            if (_fast) return;

            _savedTimeScale = Time.timeScale;
            _savedCaptureDelta = Time.captureDeltaTime;
            _savedVSync = QualitySettings.vSyncCount;
            _savedFrameRate = Application.targetFrameRate;

            Time.timeScale = 1f;
            Time.captureDeltaTime = timeStep;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;

            foreach (Camera cam in Camera.allCameras)
            {
                if (!cam.enabled) continue;
                cam.enabled = false;
                _hiddenCameras.Add(cam);
            }

            foreach (UIDocument doc in Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude))
            {
                if (doc.rootVisualElement == null) continue;
                _hiddenUI.Add((doc.rootVisualElement, doc.rootVisualElement.style.display));
                doc.rootVisualElement.style.display = DisplayStyle.None;
            }

            MagicSchool.Combat.Heroes.Hero.IsDrawn = false;

            _fast = true;
        }

        public void GoNormal()
        {
            if (!_fast) return;

            Time.timeScale = _savedTimeScale;
            Time.captureDeltaTime = _savedCaptureDelta;
            QualitySettings.vSyncCount = _savedVSync;
            Application.targetFrameRate = _savedFrameRate;

            MagicSchool.Combat.Heroes.Hero.IsDrawn = true;
            foreach (Camera cam in _hiddenCameras) if (cam != null) cam.enabled = true;
            foreach (var hidden in _hiddenUI) if (hidden.element != null) hidden.element.style.display = hidden.display;
            _hiddenCameras.Clear();
            _hiddenUI.Clear();

            _fast = false;
        }
    }
}
