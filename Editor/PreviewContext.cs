using System;
using System.Collections.Generic;
using System.Linq;
using net.rs64.TexTransTool.Build;
using net.rs64.TexTransTool.CustomPreview;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace net.rs64.TexTransTool
{
    internal class PreviewContext : ScriptableSingleton<PreviewContext>
    {
        [SerializeField]
        private Object previweing = null;
        private Object lastPreviweing = null;

        protected PreviewContext()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= ExitPreview;
            AssemblyReloadEvents.beforeAssemblyReload += ExitPreview;
            EditorSceneManager.sceneClosing -= ExitPreview;
            EditorSceneManager.sceneClosing += ExitPreview;
            DestroyCall.OnDestroy -= DestroyObserve;
            DestroyCall.OnDestroy += DestroyObserve;
        }

        private void OnEnable()
        {
            EditorApplication.update += () =>
            {
                if (!AnimationMode.InAnimationMode()) previweing = null;
            };
        }

        public static bool IsPreviewing(TexTransBehavior transformer) => transformer == instance.previweing;
        public static bool IsPreviewContains => instance.previweing != null;
        public static bool LastPreviewClear() => instance.lastPreviweing = null;

        private void DrawApplyAndRevert<T>(T target, Action<T> apply)
            where T : Object
        {
            if (target == null) return;
            if (previweing == null && AnimationMode.InAnimationMode())
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Button("Common:PreviewNotAvailable".Glc());
                EditorGUI.EndDisabledGroup();
            }
            else if (previweing == null)
            {
                if (GUILayout.Button("Common:Preview".Glc()))
                {
                    StartPreview(target, apply);
                }
            }
            else if (previweing == target)
            {
                if (GUILayout.Button("Common:ExitPreview".Glc()))
                {
                    ExitPreview();
                }
            }
            else
            {
                if (GUILayout.Button("Common:OverridePreviewThis".Glc()))
                {
                    ExitPreview();
                    StartPreview(target, apply);
                }
            }
        }
        public void DrawApplyAndRevert(TexTransBehavior target)
        {
            DrawApplyAndRevert(target, TexTransBehaviorApply);
        }

        void StartPreview<T>(T target, Action<T> applyAction) where T : Object
        {
            previweing = target;
            AnimationMode.StartAnimationMode();
            try
            {
                applyAction(target);
            }
            catch
            {
                AnimationMode.StopAnimationMode();
                EditorUtility.ClearProgressBar();
                previweing = null;
                throw;
            }
        }
        static void TexTransBehaviorApply(TexTransBehavior targetTTBehavior)
        {
            AnimationMode.BeginSampling();
            try
            {
                RenderersDomain previewDomain = null;
                var marker = DomainMarkerFinder.FindMarker(targetTTBehavior.gameObject);
                if (marker != null) { previewDomain = new AvatarDomain(marker, true, false, true); }
                else { previewDomain = new RenderersDomain(targetTTBehavior.GetRenderers, true, false, true); }

                if (!TTTCustomPreviewUtility.TryExecutePreview(targetTTBehavior, previewDomain)) { targetTTBehavior.Apply(previewDomain); }

                previewDomain.EditFinish();
            }
            finally
            {
                AnimationMode.EndSampling();
            }

        }

        public void ExitPreview()
        {
            if (previweing == null) { return; }
            lastPreviweing = previweing;
            previweing = null;
            AnimationMode.StopAnimationMode();
        }
        public void ExitPreview(UnityEngine.SceneManagement.Scene scene, bool removingScene)
        {
            ExitPreview();
        }
        public void DestroyObserve(TexTransBehavior texTransBehavior)
        {
            if (IsPreviewing(texTransBehavior)) { instance.ExitPreview(); }
        }

        internal void RePreview()
        {
            if (!IsPreviewContains)
            {
                if (lastPreviweing is TexTransBehavior texTransBehavior) { StartPreview(texTransBehavior, TexTransBehaviorApply); lastPreviweing = null; }
            }
            else
            {
                if (previweing is not TexTransBehavior texTransBehavior) { return; }
                var target = texTransBehavior;
                ExitPreview();
                StartPreview(target, TexTransBehaviorApply);
            }
        }
    }
}
