using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace c0nd3v
{
    public class CanvasIssueFix : MonoBehaviour
    {
        [InitializeOnLoadMethod]
        public static void InitializeOnLoad()
        {
            EditorApplication.update += Update1;
        }

        private static void Update1()
        {
            // 1. Open game view
            var gameView = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));

            EditorApplication.update -= Update1;

            if (!EditorApplication.isPlaying)
            {
                EditorApplication.update += Update2;
            }
        }

        private static void Update2()
        {
            // 2. Open scene view
            var sceneView = EditorWindow.GetWindow(typeof(SceneView));

            // 3. Reload scene
            var scene = SceneManager.GetActiveScene();

            if (!EditorApplication.isPlaying)
            {
                EditorSceneManager.OpenScene(scene.path);
            }

            EditorApplication.update -= Update2;
        }
    }
}
