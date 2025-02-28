using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using System;
using net.rs64.TexTransCore.TransTextureCore.Utils;

namespace net.rs64.TexTransCore.BlendTexture
{
    [Obsolete("Replaced with BlendTypeKey", true)]
    internal enum BlendType
    {
        Normal,
        Mul,
        Screen,
        Overlay,
        HardLight,
        SoftLight,
        ColorDodge,
        ColorBurn,
        LinearBurn,
        VividLight,
        LinearLight,
        Divide,
        Addition,
        Subtract,
        Difference,
        DarkenOnly,
        LightenOnly,
        Hue,
        Saturation,
        Color,
        Luminosity,
        AlphaLerp,
        NotBlend,
    }
    internal enum TTTBlendTypeKeyEnum//これをセーブデータとして使うべきではないから注意、ToStringして使うことを前提として
    {
        Normal,
        Dissolve,
        NotBlend,

        Mul,
        ColorBurn,
        LinearBurn,
        DarkenOnly,
        DarkenColorOnly,

        Screen,
        ColorDodge,
        ColorDodgeGlow,
        Addition,
        AdditionGlow,
        LightenOnly,
        LightenColorOnly,

        Overlay,
        SoftLight,
        HardLight,
        VividLight,
        LinearLight,
        PinLight,
        HardMix,

        Difference,
        Exclusion,
        Subtract,
        Divide,

        Hue,
        Saturation,
        Color,
        Luminosity,

    }
    internal static class TextureBlend
    {
        public static Dictionary<string, Shader> BlendShaders;

        public static void BlendShadersInit()
        {
            BlendTexShader = Shader.Find(BLEND_TEX_SHADER);
            ColorMulShader = Shader.Find(COLOR_MUL_SHADER);
            MaskShader = Shader.Find(MASK_SHADER);
            UnlitColorAlphaShader = Shader.Find(UNLIT_COLOR_ALPHA_SHADER);
            AlphaCopyShader = Shader.Find(ALPHA_COPY_SHADER);

            var stdBlendShader = BlendTexShader;
            var stdBlendShaders = new Dictionary<string, Shader>()
            {
                //特殊な色合成をしない系
                {"Normal",stdBlendShader},//通常
                {"Dissolve",stdBlendShader},//ディザ合成
                {"NotBlend",stdBlendShader},//ほぼTTTの内部処理用の上のレイヤーで置き換えるもの

                //暗くする系
                {"Mul",stdBlendShader},//乗算
                {"ColorBurn",stdBlendShader},//焼きこみカラー
                {"LinearBurn",stdBlendShader},//焼きこみ(リニア)
                {"DarkenOnly",stdBlendShader},//比較(暗)
                {"DarkenColorOnly",stdBlendShader},//カラー比較(暗)

                //明るくする系
                {"Screen",stdBlendShader},//スクリーン
                {"ColorDodge",stdBlendShader},//覆い焼きカラー
                {"ColorDodgeGlow",stdBlendShader},//覆い焼き(発光)
                {"Addition",stdBlendShader},//加算-覆い焼き(リニア)
                {"AdditionGlow",stdBlendShader},//加算(発光)
                {"LightenOnly",stdBlendShader},//比較(明)
                {"LightenColorOnly",stdBlendShader},//カラー比較(明)

                //ライト系
                {"Overlay",stdBlendShader},//オーバーレイ
                {"SoftLight",stdBlendShader},//ソフトライト
                {"HardLight",stdBlendShader},//ハードライト
                {"VividLight",stdBlendShader},//ビビッドライト
                {"LinearLight",stdBlendShader},//リニアライト
                {"PinLight",stdBlendShader},//ピンライト
                {"HardMix",stdBlendShader},//ハードミックス

                //算術系
                {"Difference",stdBlendShader},//差の絶対値
                {"Exclusion",stdBlendShader},//除外
                {"Subtract",stdBlendShader},//減算
                {"Divide",stdBlendShader},//除算

                //視覚的な色調置き換え系
                {"Hue",stdBlendShader},//色相
                {"Saturation",stdBlendShader},//彩度
                {"Color",stdBlendShader},//カラー
                {"Luminosity",stdBlendShader},//輝度
            };

            BlendShaders = new();
            var extensions = InterfaceUtility.GetInterfaceInstance<ITexBlendExtension>();
            foreach (var ext in extensions)
            {
                var (Keywords, shader) = ext.GetExtensionBlender();
                if (Keywords.Any(str => !str.Contains("/"))) { Debug.LogWarning($"TexBlendExtension : {ext.GetType().FullName} \"/\" is not Contained!!!"); }
                foreach (var Keyword in Keywords)
                {
                    if (BlendShaders.ContainsKey(Keyword) || stdBlendShaders.ContainsKey(Keyword))
                    {
                        Debug.LogWarning($"TexBlendExtension : {ext.GetType().FullName} {Keyword} is Contained!!!");
                    }
                    else
                    {
                        BlendShaders[Keyword] = shader;
                    }
                }
            }

            foreach (var kv in stdBlendShaders) { BlendShaders.Add(kv.Key, kv.Value); }

        }
        public const string BL_KEY_DEFAULT = "Normal";
        public const string BLEND_TEX_SHADER = "Hidden/BlendTexture";
        public static Shader BlendTexShader;
        public const string COLOR_MUL_SHADER = "Hidden/ColorMulShader";
        public static Shader ColorMulShader;
        public const string MASK_SHADER = "Hidden/MaskShader";
        public static Shader MaskShader;
        public const string UNLIT_COLOR_ALPHA_SHADER = "Hidden/UnlitColorAndAlpha";
        public static Shader UnlitColorAlphaShader;
        public const string ALPHA_COPY_SHADER = "Hidden/AlphaCopy";
        public static Shader AlphaCopyShader;
        public static void BlendBlit(this RenderTexture baseRenderTexture, Texture Add, string blendTypeKey, bool keepAlpha = false)
        {
            using (new RTActiveSaver())
            {
                var material = new Material(BlendShaders[blendTypeKey]);
                var swap = RenderTexture.GetTemporary(baseRenderTexture.descriptor);
                Graphics.CopyTexture(baseRenderTexture, swap);
                material.SetTexture("_DistTex", swap);
                material.EnableKeyword(EscapeForShaderKeyword(blendTypeKey));

                Graphics.Blit(Add, baseRenderTexture, material);

                if (keepAlpha)
                {
                    var alphaCopyMat = new Material(AlphaCopyShader);
                    var baseSwap = RenderTexture.GetTemporary(baseRenderTexture.descriptor);

                    alphaCopyMat.SetTexture("_AlphaTex", swap);
                    Graphics.CopyTexture(baseRenderTexture, baseSwap);
                    Graphics.Blit(baseSwap, baseRenderTexture, alphaCopyMat);

                    RenderTexture.ReleaseTemporary(baseSwap);
                    UnityEngine.Object.DestroyImmediate(alphaCopyMat);
                }

                RenderTexture.ReleaseTemporary(swap);
                UnityEngine.Object.DestroyImmediate(material);
            }
        }
        private static string EscapeForShaderKeyword(string blendTypeKey) => blendTypeKey.Replace('/', '_');
        public static void BlendBlit<BlendTex>(this RenderTexture baseRenderTexture, BlendTex add, bool keepAlpha = false)
        where BlendTex : IBlendTexturePair
        { baseRenderTexture.BlendBlit(add.Texture, add.BlendTypeKey, keepAlpha); }
        public static void BlendBlit<BlendTex>(this RenderTexture baseRenderTexture, IEnumerable<BlendTex> adds)
        where BlendTex : IBlendTexturePair
        {
            using (new RTActiveSaver())
            {
                var material = new Material(BlendTexShader);
                var temRt = RenderTexture.GetTemporary(baseRenderTexture.descriptor);
                var swap = baseRenderTexture;
                var target = temRt;
                Graphics.Blit(swap, target);

                foreach (var Add in adds)
                {
                    if (Add == null || Add.Texture == null || Add.BlendTypeKey == null) { continue; }
                    if (material.shader != BlendShaders[Add.BlendTypeKey]) { material.shader = BlendShaders[Add.BlendTypeKey]; }
                    material.SetTexture("_DistTex", swap);
                    material.shaderKeywords = new[] { EscapeForShaderKeyword(Add.BlendTypeKey) };
                    Graphics.Blit(Add.Texture, target, material);
                    (swap, target) = (target, swap);
                }

                if (swap != baseRenderTexture)
                {
                    Graphics.Blit(swap, baseRenderTexture);
                }
                RenderTexture.ReleaseTemporary(temRt);
                UnityEngine.Object.DestroyImmediate(material);
            }
        }
        public static RenderTexture BlendBlit(Texture2D baseRenderTexture, Texture add, string blendTypeKey, RenderTexture targetRt = null)
        {
            using (new RTActiveSaver())
            {
                if (targetRt == null) { targetRt = RenderTexture.GetTemporary(baseRenderTexture.width, baseRenderTexture.height, 0); }
                Graphics.Blit(baseRenderTexture, targetRt);
                targetRt.BlendBlit(add, blendTypeKey);
                return targetRt;
            }
        }
        public interface IBlendTexturePair
        {
            public Texture Texture { get; }
            public string BlendTypeKey { get; }

        }
        public struct BlendTexturePair : IBlendTexturePair
        {
            public Texture Texture;
            public string BlendTypeKey;

            public BlendTexturePair(Texture texture, string blendTypeKey)
            {
                Texture = texture;
                BlendTypeKey = blendTypeKey;
            }

            Texture IBlendTexturePair.Texture => Texture;

            string IBlendTexturePair.BlendTypeKey => BlendTypeKey;
        }

        public static RenderTexture CreateMultipliedRenderTexture(Texture mainTex, Color color)
        {
            var mainTexRt = RenderTexture.GetTemporary(mainTex.width, mainTex.height, 0);
            MultipleRenderTexture(mainTexRt, mainTex, color);
            return mainTexRt;
        }
        public static void MultipleRenderTexture(RenderTexture mainTexRt, Texture mainTex, Color color)
        {
            using (new RTActiveSaver())
            {
                var mat = new Material(ColorMulShader);
                mat.SetColor("_Color", color);
                Graphics.Blit(mainTex, mainTexRt, mat);
                UnityEngine.Object.DestroyImmediate(mat);
            }
        }
        public static void MultipleRenderTexture(RenderTexture renderTexture, Color color)
        {
            using (new RTActiveSaver())
            {
                var tempRt = RenderTexture.GetTemporary(renderTexture.descriptor);
                var mat = new Material(ColorMulShader);
                mat.SetColor("_Color", color);
                Graphics.CopyTexture(renderTexture, tempRt);
                Graphics.Blit(tempRt, renderTexture, mat);
                RenderTexture.ReleaseTemporary(tempRt);
                UnityEngine.Object.DestroyImmediate(mat);
            }
        }
        public static void MaskDrawRenderTexture(RenderTexture renderTexture, Texture maskTex)
        {
            using (new RTActiveSaver())
            {
                var tempRt = RenderTexture.GetTemporary(renderTexture.descriptor);
                var mat = new Material(MaskShader);
                mat.SetTexture("_MaskTex", maskTex);
                Graphics.CopyTexture(renderTexture, tempRt);
                Graphics.Blit(tempRt, renderTexture, mat);
                RenderTexture.ReleaseTemporary(tempRt);
                UnityEngine.Object.DestroyImmediate(mat);
            }
        }
        public static void ColorBlit(RenderTexture mulDecalTexture, Color color)
        {
            using (new RTActiveSaver())
            {
                var unlitMat = new Material(UnlitColorAlphaShader);
                unlitMat.SetColor("_Color", color);
                Graphics.Blit(null, mulDecalTexture, unlitMat);
                UnityEngine.Object.DestroyImmediate(unlitMat);
            }
        }
        public static void AlphaOne(RenderTexture rt)
        {
            using (new RTActiveSaver())
            {
                var mat = new Material(AlphaCopyShader);
                mat.SetTexture("_AlphaTex", Texture2D.whiteTexture);
                var swap = RenderTexture.GetTemporary(rt.descriptor);
                Graphics.CopyTexture(rt, swap);
                Graphics.Blit(swap, rt, mat);
                RenderTexture.ReleaseTemporary(swap);
                UnityEngine.Object.DestroyImmediate(mat);
            }
        }
        public static void AlphaCopy(RenderTexture alphaSouse, RenderTexture rt)
        {
            using (new RTActiveSaver())
            {
                var mat = new Material(AlphaCopyShader);
                mat.SetTexture("_AlphaTex", alphaSouse);
                var swap = RenderTexture.GetTemporary(rt.descriptor);
                Graphics.CopyTexture(rt, swap);
                Graphics.Blit(swap, rt, mat);
                RenderTexture.ReleaseTemporary(swap);
                UnityEngine.Object.DestroyImmediate(mat);
            }
        }
    }

    internal struct RTActiveSaver : IDisposable
    {
        readonly RenderTexture PreRT;
        public RTActiveSaver(bool empty = false)
        {
            PreRT = RenderTexture.active;
        }
        public void Dispose()
        {
            RenderTexture.active = PreRT;
        }
    }

}
