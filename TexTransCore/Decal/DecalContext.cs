using System.Collections.ObjectModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using net.rs64.TexTransCore.TransTextureCore;
using net.rs64.TexTransCore.TransTextureCore.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Pool;
using UnityEngine.Profiling;

namespace net.rs64.TexTransCore.Decal
{
    internal interface IConvertSpace<UVDimension> : IDisposable
      where UVDimension : struct
    {
        void Input(MeshData meshData);//この MeshData の解放責任は受け取らない
        NativeArray<UVDimension> OutPutUV();
    }
    internal interface ITrianglesFilter<SpaceConverter> : IDisposable
    {
        void SetSpace(SpaceConverter space);
        NativeArray<TriangleIndex> GetFilteredSubTriangle(int subMeshIndex);
    }
    internal class DecalContext<ConvertSpace, TrianglesFilter, UVDimension>
    where ConvertSpace : IConvertSpace<UVDimension>
    where TrianglesFilter : ITrianglesFilter<ConvertSpace>
    where UVDimension : struct
    {
        ConvertSpace _convertSpace;
        TrianglesFilter _trianglesFilter;


        public string TargetPropertyName { get; set; } = "_MainTex";
        public TextureWrap? TextureWarp { get; set; } = null;
        public float DecalPadding { get; set; } = 0.5f;
        public bool HighQualityPadding { get; set; } = false;
        public bool? UseDepthOrInvert { get; set; } = null;

        public DecalContext(ConvertSpace convertSpace, TrianglesFilter trianglesFilter)
        {
            _convertSpace = convertSpace;
            _trianglesFilter = trianglesFilter;
        }

        internal void WriteDecalTexture(Dictionary<Material, RenderTexture> renderTextures, Renderer targetRenderer, Texture sourceTexture)
        {
            if (renderTextures == null) { throw new ArgumentNullException(nameof(renderTextures)); }

            Profiler.BeginSample("GetMeshData");
            var meshData = targetRenderer.Memo(MeshData.GetMeshData, i => i.Dispose());
            Profiler.EndSample();

            Profiler.BeginSample("GetUVs");
            var tUV = meshData.VertexUV;
            Profiler.EndSample();

            Profiler.BeginSample("convertSpace.Input");
            _convertSpace.Input(meshData);
            Profiler.EndSample();

            _trianglesFilter.SetSpace(_convertSpace);


            for (int i = 0; i < meshData.Triangles.Length; i++)
            {
                var targetMat = targetRenderer.sharedMaterials[i];

                if (!targetMat.HasProperty(TargetPropertyName)) { continue; };
                var targetTexture = targetMat.GetTexture(TargetPropertyName);
                if (targetTexture == null) { continue; }

                Profiler.BeginSample("GetFilteredSubTriangle");
                var filteredTriangle = _trianglesFilter.GetFilteredSubTriangle(i);
                Profiler.EndSample();
                if (filteredTriangle.Length == 0) { continue; }

                if (!renderTextures.ContainsKey(targetMat)) { renderTextures[targetMat] = RenderTexture.GetTemporary(targetTexture.width, targetTexture.height, 32); renderTextures[targetMat].Clear(); }

                var sUV = _convertSpace.OutPutUV();

                Profiler.BeginSample("TransTexture.ForTrans");
                TransTexture.ForTrans(
                    renderTextures[targetMat],
                    sourceTexture,
                    new TransTexture.TransData<UVDimension>(filteredTriangle, tUV, sUV),
                    DecalPadding,
                    TextureWarp,
                    HighQualityPadding,
                    UseDepthOrInvert
                );
                Profiler.EndSample();
            }
            _trianglesFilter.Dispose();
            _convertSpace.Dispose();
        }



    }

    public enum PolygonCulling
    {
        Vertex,
        Edge,
        EdgeAndCenterRay,
    }
}
