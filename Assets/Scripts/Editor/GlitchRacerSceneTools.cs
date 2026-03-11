#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

namespace GlitchRacer.Editor
{
    public static class GlitchRacerSceneTools
    {
        [MenuItem("Tools/Glitch Racer/Rebuild MVP Scene")]
        private static void RebuildScene()
        {
            GlitchRacerRuntimeBootstrap.BuildIntoCurrentScene(true);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
}
#endif
