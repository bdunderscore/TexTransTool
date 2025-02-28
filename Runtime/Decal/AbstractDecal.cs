using UnityEngine;
using System.Collections.Generic;
using net.rs64.TexTransCore.TransTextureCore;
using net.rs64.TexTransCore.TransTextureCore.Utils;
using System;
using net.rs64.TexTransCore.BlendTexture;
using net.rs64.TexTransTool.Utils;
using net.rs64.TexTransCore.Island;
using UnityEngine.Pool;

namespace net.rs64.TexTransTool.Decal
{
    [ExecuteInEditMode]
    public abstract class AbstractDecal : TexTransRuntimeBehavior
    {
        public List<Renderer> TargetRenderers = new List<Renderer> { null };
        public bool MultiRendererMode = false;
        [BlendTypeKey] public string BlendTypeKey = TextureBlend.BL_KEY_DEFAULT;

        public Color Color = Color.white;
        public PropertyName TargetPropertyName = PropertyName.DefaultValue;
        public float Padding = 5;
        public bool HighQualityPadding = false;

        #region V1SaveData
        [Obsolete("Replaced with BlendTypeKey", true)][HideInInspector][SerializeField] internal BlendType BlendType = BlendType.Normal;
        #endregion
        #region V0SaveData
        [Obsolete("V0SaveData", true)][HideInInspector] public bool MigrationV0ClearTarget;
        [Obsolete("V0SaveData", true)][HideInInspector] public GameObject MigrationV0DataMatAndTexSeparatorGameObject;
        [Obsolete("V0SaveData", true)][HideInInspector] public MatAndTexUtils.MatAndTexRelativeSeparator MigrationV0DataMatAndTexSeparator;
        [Obsolete("V0SaveData", true)][HideInInspector] public AbstractDecal MigrationV0DataAbstractDecal;
        [Obsolete("V0SaveData", true)][HideInInspector] public bool IsSeparateMatAndTexture;
        [Obsolete("V0SaveData", true)][HideInInspector] public bool FastMode = true;
        #endregion
        internal virtual TextureWrap GetTextureWarp { get => TextureWrap.NotWrap; }

        internal override List<Renderer> GetRenderers => TargetRenderers;

        internal override TexTransPhase PhaseDefine => TexTransPhase.AfterUVModification;

        internal override void Apply(IDomain domain)
        {
            if (!IsPossibleApply)
            {
                TTTRuntimeLog.Error(GetType().Name + ":error:TTTNotExecutable");
                return;
            }

            domain.ProgressStateEnter("AbstractDecal");

            domain.ProgressUpdate("DecalCompile", 0.25f);

            var decalCompiledTextures = CompileDecal(domain.GetTextureManager(), domain.GetIslandCacheManager(), DictionaryPool<Material, RenderTexture>.Get());

            domain.ProgressUpdate("AddStack", 0.75f);

            foreach (var matAndTex in decalCompiledTextures)
            {
                domain.AddTextureStack(matAndTex.Key.GetTexture(TargetPropertyName) as Texture2D, new TextureBlend.BlendTexturePair(matAndTex.Value, BlendTypeKey));
            }

            DictionaryPool<Material, RenderTexture>.Release(decalCompiledTextures);

            domain.ProgressUpdate("End", 1);
            domain.ProgressStateExit();
        }


        internal abstract Dictionary<Material, RenderTexture> CompileDecal(ITextureManager textureManager, IIslandCache islandCacheManager, Dictionary<Material, RenderTexture> decalCompiledRenderTextures = null);

        internal static RenderTexture GetMultipleDecalTexture(ITextureManager textureManager, Texture2D souseDecalTexture, Color color)
        {
            RenderTexture mulDecalTexture;

            if (souseDecalTexture != null)
            {
                var decalSouseSize = textureManager.GetOriginalTextureSize(souseDecalTexture);
                mulDecalTexture = RenderTexture.GetTemporary(decalSouseSize, decalSouseSize, 0);
            }
            else { mulDecalTexture = RenderTexture.GetTemporary(32, 32, 0); }
            mulDecalTexture.Clear();
            if (souseDecalTexture != null)
            {
                var tempRt = textureManager.GetOriginTempRt(souseDecalTexture);
                TextureBlend.MultipleRenderTexture(mulDecalTexture, tempRt, color);
                RenderTexture.ReleaseTemporary(tempRt);
            }
            else
            {
                TextureBlend.ColorBlit(mulDecalTexture, color);
            }
            return mulDecalTexture;
        }

        // TODO : リアルタイムプレビューの改修と同時に何とかする

        // [NonSerialized] public bool ThisIsForces = false;
        // private void Update()
        // {
        //     if (ThisIsForces && RealTimePreviewManager.instance.RealTimePreviews.ContainsKey(this))
        //     {
        //         RealTimePreviewManager.instance.UpdateAbstractDecal(this);
        //     }
        //     ThisIsForces = false;
        // }

    }
}
