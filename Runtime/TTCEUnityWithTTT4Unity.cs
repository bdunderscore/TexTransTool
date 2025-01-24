#nullable enable
using System;
using net.rs64.TexTransCore;
using net.rs64.TexTransCoreEngineForUnity;
using net.rs64.TexTransTool.MultiLayerImage;
using net.rs64.TexTransTool.Utils;
using Unity.Collections;
using UnityEngine;

namespace net.rs64.TexTransTool
{
    internal class TTCEUnityWithTTT4Unity : TTCEUnity, ITexTransToolForUnity
    {
        ITexTransUnityDiskUtil _diskUtil;
        public TTCEUnityWithTTT4Unity(ITexTransUnityDiskUtil diskUtil)
        {
            _diskUtil = diskUtil;
        }

        public void UploadTexture<T>(ITTRenderTexture uploadTarget, ReadOnlySpan<T> bytes, TexTransCoreTextureFormat format) where T : unmanaged
        {
            var tex = new Texture2D(uploadTarget.Width, uploadTarget.Hight, format.ToUnityTextureFormat(uploadTarget.ContainsChannel), false);

            using var na = new NativeArray<T>(bytes.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            bytes.CopyTo(na);
            tex.LoadRawTextureData(na);

            tex.Apply();
            Graphics.Blit(tex, uploadTarget.Unwrap());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        public virtual ITTRenderTexture UploadTexture(RenderTexture renderTexture)
        {
            var urt = UnityRenderTexture.RefFromTTRt2(renderTexture);
            if (urt is not null)// TTT が所有している物は Liner だけど中身が gamma なので気を付けないといけない。
            {
                var rt = CreateRenderTexture(renderTexture.width, renderTexture.height, urt.ContainsChannel);
                CopyRenderTexture(rt, urt);
                return rt;
            }
            else
            {
                var rt = CreateRenderTexture(renderTexture.width, renderTexture.height);
                Graphics.Blit(renderTexture, rt.Unwrap());
                return rt;
            }
        }

        public void DownloadTexture<T>(Span<T> dataDist, TexTransCoreTextureFormat format, ITTRenderTexture renderTexture) where T : unmanaged
        {
            if (renderTexture.Unwrap().graphicsFormat == format.ToUnityGraphicsFormat(renderTexture.ContainsChannel))
            {
                renderTexture.Unwrap().DownloadFromRenderTexture(dataDist);
            }
            else
            {
                var cfRt = new RenderTexture(renderTexture.Width, renderTexture.Hight, 0, format.ToUnityGraphicsFormat(renderTexture.ContainsChannel));
                Graphics.Blit(renderTexture.Unwrap(), cfRt);
                cfRt.DownloadFromRenderTexture(dataDist);
            }
        }


        public ITTBlendKey QueryBlendKey(string blendKeyName)
        {
            return ComputeObjectUtility.BlendingObject[blendKeyName];
        }

        public ITTDiskTexture Wrapping(Texture2D texture2D)
        {
            return _diskUtil.Wrapping(texture2D);
        }

        public ITTDiskTexture Wrapping(TTTImportedImage importImage)
        {
            return _diskUtil.Wrapping(importImage);
        }

        public void LoadTexture(ITTRenderTexture writeTarget, ITTDiskTexture diskTexture)
        {
            _diskUtil.LoadTexture(this, writeTarget, diskTexture);
        }
    }
}
