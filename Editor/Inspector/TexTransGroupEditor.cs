using UnityEngine;
using UnityEditor;
using net.rs64.TexTransTool.Decal;
using net.rs64.TexTransTool.Editor.Decal;
using net.rs64.TexTransTool.MatAndTexUtils;
using net.rs64.TexTransTool.Editor.MatAndTexUtils;
using net.rs64.TexTransTool.TextureAtlas;
using net.rs64.TexTransTool.TextureAtlas.Editor;
using net.rs64.TexTransTool.Utils;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using UnityEditor.UIElements;

namespace net.rs64.TexTransTool.Editor
{
    [CustomEditor(typeof(TexTransGroup))]
    internal class TexTransGroupEditor : UnityEditor.Editor
    {
        internal static Dictionary<Type, Func<TexTransBehavior, VisualElement>> s_summary = new();
        internal static StyleSheet s_style;

        public override VisualElement CreateInspectorGUI()
        {
            LoadStyle();

            var rootVE = new VisualElement();
            var previewButton = new IMGUIContainer(() => PreviewContext.instance.DrawApplyAndRevert(target as TexTransGroup));

            rootVE.hierarchy.Add(previewButton);
            rootVE.styleSheets.Add(s_style);

            CreateGroupElements(rootVE, target as TexTransGroup, false);

            return rootVE;
        }

        internal static void LoadStyle() { s_style ??= AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath("9d80dcf21bff21f4cb110fff304f5622")); }

        internal static void CreateGroupElements(VisualElement rootVE, TexTransGroup group, bool addPhaseDefinition = false)
        {
            if (group == null) { return; }
            foreach (var ttb in group.Targets)
            {
                var ttbSummaryElement = CreateSummaryBase(ttb);

                if (ttb is TexTransGroup texTransGroup)
                {
                    if (texTransGroup is PhaseDefinition && !addPhaseDefinition) { continue; }
                    CreateNestedTexTransGroupSummary(ttbSummaryElement, texTransGroup, addPhaseDefinition);
                }
                else { CreateSummary(ttbSummaryElement, ttb); }

                rootVE.hierarchy.Add(ttbSummaryElement);
            }
        }

        private static VisualElement CreateSummaryBase(TexTransBehavior ttb)
        {
            var ttbSummaryElement = new VisualElement();
            ttbSummaryElement.AddToClassList("SummaryElementRoot");
            ttbSummaryElement.hierarchy.Add(SummaryBase(ttb));
            return ttbSummaryElement;
        }

        private static void CreateNestedTexTransGroupSummary(VisualElement ttbSummaryElement, TexTransGroup texTransGroup, bool addPhaseDefinition = false)
        {
            var nextGroup = new VisualElement();
            nextGroup.style.paddingLeft = 8f;
            CreateGroupElements(nextGroup, texTransGroup, addPhaseDefinition);
            ttbSummaryElement.hierarchy.Add(nextGroup);
        }

        private static void CreateSummary(VisualElement ttbSummaryElement, TexTransBehavior ttb)
        {
            if (s_summary.TryGetValue(ttb.GetType(), out var generator))
            {
                var summaryContainer = new VisualElement();
                summaryContainer.AddToClassList("SummaryContainer");
                summaryContainer.hierarchy.Add(generator.Invoke(ttb));
                ttbSummaryElement.hierarchy.Add(summaryContainer);
            }
            else
            {
                var noneLabel = new Label("TexTransGroup:label:SummaryNone".GetLocalize());
                noneLabel.AddToClassList("SummaryContainer");
                ttbSummaryElement.hierarchy.Add(noneLabel);
            }
        }

        private static VisualElement SummaryBase(TexTransBehavior ttb)
        {
            var sObj = new SerializedObject(ttb.gameObject);
            var sActive = sObj.FindProperty("m_IsActive");

            var ttbSummaryBase = new VisualElement();
            ttbSummaryBase.style.flexDirection = FlexDirection.Row;

            var goButton = new Toggle();
            goButton.BindProperty(sActive);
            goButton.AddToClassList("ActiveToggle");
            goButton.label = "";
            goButton.text = "TexTransGroup:prop:GOEnable".GetLocalize();
            ttbSummaryBase.hierarchy.Add(goButton);

            var componentField = new ObjectField();
            componentField.value = ttb;
            componentField.label = "";
            componentField.SetEnabled(false);
            componentField.style.opacity = 0.9f;
            componentField.AddToClassList("TexTransBehaviorRef");
            ttbSummaryBase.hierarchy.Add(componentField);

            return ttbSummaryBase;
        }

        private void OnHierarchyChange() { Repaint(); }


    }
}
