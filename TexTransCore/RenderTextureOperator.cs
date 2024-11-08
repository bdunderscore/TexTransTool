#nullable enable
using System;

namespace net.rs64.TexTransCore
{
    public static class RenderTextureOperator
    {
        // 引数の並びについて、
        // target , source の順にすること、

        /// <summary>
        /// バイリニアで適当にリスケールする、いずれまともなものに置き換えるようにね、
        /// あと同一解像度の場合は例外を投げるよ
        /// </summary>
        public static void BilinearReScaling<TTCE>(this TTCE engine, ITTRenderTexture targetTexture, ITTRenderTexture sourceTexture)
        where TTCE : ITexTransComputeKeyQuery, ITexTransGetComputeHandler
        {
            if (sourceTexture.Width == targetTexture.Width && sourceTexture.Hight == targetTexture.Hight) { throw new ArgumentException(); }

            using var computeHandler = engine.GetComputeHandler(engine.StandardComputeKey.BilinearReScaling);

            var sourceTexID = computeHandler.NameToID("SourceTex");
            var targetTexID = computeHandler.NameToID("TargetTex");
            var gvBufId = computeHandler.NameToID("gv");

            Span<uint> gvBuf = stackalloc uint[4];
            gvBuf[0] = (uint)sourceTexture.Width;//SourceTexSize.x
            gvBuf[1] = (uint)sourceTexture.Hight;//SourceTexSize.y
            gvBuf[2] = (uint)targetTexture.Width;//TargetTexSize.x
            gvBuf[3] = (uint)targetTexture.Hight;//TargetTexSize.y
            computeHandler.UploadCBuffer<uint>(gvBufId, gvBuf);

            computeHandler.SetTexture(sourceTexID, sourceTexture);
            computeHandler.SetTexture(targetTexID, targetTexture);

            computeHandler.DispatchWithTextureSize(targetTexture);
        }

        public static void AlphaFill<TTCE>(this TTCE engine, ITTRenderTexture renderTexture, float alpha)
        where TTCE : ITexTransComputeKeyQuery, ITexTransGetComputeHandler
        {
            using var computeHandler = engine.GetComputeHandler(engine.StandardComputeKey.AlphaFill);

            var texID = computeHandler.NameToID("Tex");
            var gvBufId = computeHandler.NameToID("gv");

            Span<float> gvBuf = stackalloc float[1];
            gvBuf[0] = alpha;
            computeHandler.UploadCBuffer<float>(gvBufId, gvBuf);

            computeHandler.SetTexture(texID, renderTexture);

            computeHandler.DispatchWithTextureSize(renderTexture);
        }
        public static void AlphaMultiply<TTCE>(this TTCE engine, ITTRenderTexture renderTexture, float value)
        where TTCE : ITexTransComputeKeyQuery, ITexTransGetComputeHandler
        {
            using var computeHandler = engine.GetComputeHandler(engine.StandardComputeKey.AlphaMultiply);

            var texID = computeHandler.NameToID("Tex");
            var gvBufId = computeHandler.NameToID("gv");

            Span<float> gvBuf = stackalloc float[1];
            gvBuf[0] = value;
            computeHandler.UploadCBuffer<float>(gvBufId, gvBuf);

            computeHandler.SetTexture(texID, renderTexture);

            computeHandler.DispatchWithTextureSize(renderTexture);
        }
        /// <summary>
        /// target.a = target.a * source.a
        /// 同じ大きさでないといけない。
        /// </summary>
        public static void AlphaMultiplyWithTexture<TTCE>(this TTCE engine, ITTRenderTexture target, ITTRenderTexture source)
        where TTCE : ITexTransComputeKeyQuery, ITexTransGetComputeHandler
        {
            if (source.Width != target.Width || source.Hight != target.Hight) { throw new ArgumentException(); }
            using var computeHandler = engine.GetComputeHandler(engine.StandardComputeKey.AlphaMultiplyWithTexture);

            var sourceTexID = computeHandler.NameToID("SourceTex");
            var targetTexID = computeHandler.NameToID("TargetTex");

            computeHandler.SetTexture(sourceTexID, source);
            computeHandler.SetTexture(targetTexID, target);

            computeHandler.DispatchWithTextureSize(target);
        }

        /// <summary>
        /// 同じ大きさでないといけない。
        /// </summary>
        public static void AlphaCopy<TTCE>(this TTCE engine, ITTRenderTexture target, ITTRenderTexture source)
        where TTCE : ITexTransComputeKeyQuery, ITexTransGetComputeHandler
        {
            if (source.Width != target.Width || source.Hight != target.Hight) { throw new ArgumentException(); }
            using var computeHandler = engine.GetComputeHandler(engine.StandardComputeKey.AlphaCopy);

            var sourceTexID = computeHandler.NameToID("SourceTex");
            var targetTexID = computeHandler.NameToID("TargetTex");

            computeHandler.SetTexture(sourceTexID, source);
            computeHandler.SetTexture(targetTexID, target);

            computeHandler.DispatchWithTextureSize(target);
        }




        /// <summary>
        /// 一色でそのレンダーテクスチャーを染めます。
        /// </summary>
        public static void ColorFill<TTCE>(this TTCE engine, ITTRenderTexture target, Color color)
        where TTCE : ITexTransComputeKeyQuery, ITexTransGetComputeHandler
        {
            using var computeHandler = engine.GetComputeHandler(engine.StandardComputeKey.ColorFill);

            var texID = computeHandler.NameToID("Tex");
            var gvBufId = computeHandler.NameToID("gv");

            Span<Color> gvBuf = stackalloc Color[1];
            gvBuf[0] = color;
            computeHandler.UploadCBuffer<Color>(gvBufId, gvBuf);

            computeHandler.SetTexture(texID, target);

            computeHandler.DispatchWithTextureSize(target);
        }
        /// <summary>
        /// その色でレンダーテクスチャーを乗算します
        /// </summary>
        public static void ColorMultiply<TTCE>(this TTCE engine, ITTRenderTexture target, Color color)
        where TTCE : ITexTransComputeKeyQuery, ITexTransGetComputeHandler
        {
            using var computeHandler = engine.GetComputeHandler(engine.StandardComputeKey.ColorMultiply);

            var texID = computeHandler.NameToID("Tex");
            var gvBufId = computeHandler.NameToID("gv");

            Span<Color> gvBuf = stackalloc Color[1];
            gvBuf[0] = color;
            computeHandler.UploadCBuffer<Color>(gvBufId, gvBuf);

            computeHandler.SetTexture(texID, target);

            computeHandler.DispatchWithTextureSize(target);
        }

        public static void GammaToLinear<TTCE>(this TTCE engine, ITTRenderTexture target)
        where TTCE : ITexTransComputeKeyQuery, ITexTransGetComputeHandler
        {
            using var computeHandler = engine.GetComputeHandler(engine.StandardComputeKey.GammaToLinear);

            var texID = computeHandler.NameToID("Tex");
            var gvBufId = computeHandler.NameToID("gv");

            computeHandler.SetTexture(texID, target);
            computeHandler.DispatchWithTextureSize(target);
        }
        public static void LinearToGamma<TTCE>(this TTCE engine, ITTRenderTexture target)
        where TTCE : ITexTransComputeKeyQuery, ITexTransGetComputeHandler
        {
            using var computeHandler = engine.GetComputeHandler(engine.StandardComputeKey.LinearToGamma);

            var texID = computeHandler.NameToID("Tex");
            var gvBufId = computeHandler.NameToID("gv");

            computeHandler.SetTexture(texID, target);
            computeHandler.DispatchWithTextureSize(target);
        }

    }
}
