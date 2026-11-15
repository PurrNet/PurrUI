using PurrNet.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PurrNet.Editor.UI
{
    [InitializeOnLoad]
    static class ProceduralUIPrefabStageRefresher
    {
        static GameObject _queuedRoot;
        static int _queuedPasses;
        static bool _refreshQueued;

        static ProceduralUIPrefabStageRefresher()
        {
            PrefabStage.prefabStageOpened += OnPrefabStageOpened;
            EditorApplication.hierarchyChanged += QueueCurrentPrefabStageRefresh;
            Undo.undoRedoPerformed += QueueCurrentPrefabStageRefresh;
        }

        static void OnPrefabStageOpened(PrefabStage stage)
        {
            QueueRefresh(stage?.prefabContentsRoot, 3);
        }

        static void QueueCurrentPrefabStageRefresh()
        {
            QueueRefresh(PrefabStageUtility.GetCurrentPrefabStage()?.prefabContentsRoot);
        }

        static void QueueRefresh(GameObject root, int passes = 2)
        {
            if (!root)
                return;

            _queuedRoot = root;
            _queuedPasses = Mathf.Max(_queuedPasses, passes);

            if (_refreshQueued)
                return;

            _refreshQueued = true;
            EditorApplication.QueuePlayerLoopUpdate();
            EditorApplication.delayCall += RunQueuedRefresh;
        }

        static void RunQueuedRefresh()
        {
            _refreshQueued = false;

            var root = _queuedRoot;
            if (!root)
                root = PrefabStageUtility.GetCurrentPrefabStage()?.prefabContentsRoot;

            if (root)
                Refresh(root);

            _queuedPasses--;
            if (_queuedPasses <= 0 || !root)
            {
                _queuedRoot = null;
                _queuedPasses = 0;
                return;
            }

            _refreshQueued = true;
            EditorApplication.QueuePlayerLoopUpdate();
            EditorApplication.delayCall += RunQueuedRefresh;
        }

        static void Refresh(GameObject root)
        {
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                var targetCanvas = canvas.rootCanvas ? canvas.rootCanvas : canvas;
                targetCanvas.additionalShaderChannels |= SignedDistanceFieldGraphic.RequiredCanvasChannels;
            }

            foreach (var coloredGraphic in root.GetComponentsInChildren<ColoredGraphic>(true))
                coloredGraphic.Refresh();

            foreach (var glowModifier in root.GetComponentsInChildren<GlowModifier>(true))
                glowModifier.Refresh();

            // Refresh() queues each graphic for SignedDistanceFieldGraphic's shared editor flush, which
            // forces one canvas update for all of them; forcing another here only doubled the cost. This
            // runs on every hierarchyChanged while a prefab stage is open, so it adds up quickly.
            foreach (var graphic in root.GetComponentsInChildren<SignedDistanceFieldGraphic>(true))
                graphic.Refresh();

            SceneView.RepaintAll();
        }
    }
}
