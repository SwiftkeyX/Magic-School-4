using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

// Runs every EditMode test from inside the open Editor, through Coplay execute_script.
// The call returns at once; the results land in Temp/test-results.txt when the run ends.
public static class RunEditModeTests
{
    public const string Out = "Temp/test-results.txt";   // relative to the project folder; Temp is not in git

    public static string Execute()
    {
        // a dirty scene makes the Test Runner open a modal "save?" dialog, which freezes the Editor for Coplay.
        // The flag is often set with nothing changed; then saving writes the same bytes and is harmless.
        // A scene that really differs from disk is the user's work: refuse instead.
        for (int i = 0; i < UnityEditor.SceneManagement.EditorSceneManager.sceneCount; i++)
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(i);
            if (!scene.isDirty) continue;
            const string copy = "Temp/SceneInMemory.unity";
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, copy, saveAsCopy: true);
            bool same = File.Exists(scene.path) && File.ReadAllText(copy) == File.ReadAllText(scene.path);
            File.Delete(copy);
            if (!same) return $"refused: {scene.path} has unsaved changes, the Test Runner would block on a save dialog";
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }
        if (File.Exists(Out)) File.Delete(Out);
        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new Collector());
        api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
        return "started";
    }

    private class Collector : ICallbacks
    {
        private readonly StringBuilder _sb = new StringBuilder();
        public void RunStarted(ITestAdaptor testsToRun) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.HasChildren) return;
            _sb.AppendLine($"{result.TestStatus} {result.FullName}");
            if (result.TestStatus == TestStatus.Failed) _sb.AppendLine("    " + result.Message?.Replace("\n", "\n    "));
        }
        public void RunFinished(ITestResultAdaptor result)
        {
            _sb.Insert(0, $"TOTAL passed={result.PassCount} failed={result.FailCount} skipped={result.SkipCount}\n");
            File.WriteAllText(Out, _sb.ToString());
        }
    }
}
