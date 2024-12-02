using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using System;
using net.rs64.TexTransTool;
using net.rs64.TexTransCore;
using net.rs64.TexTransTool.Utils;
using System.Runtime.InteropServices;
using net.rs64.TexTransCoreEngineForUnity;

namespace net.rs64.TexTransTool
{
    internal static class TransTexture
    {
        public struct TransData
        {
            public NativeArray<TriangleIndex> TrianglesToIndex;
            public NativeArray<Vector2> TargetUV;
            public NativeArray<Vector2> SourceUV;

            public TransData(
                IEnumerable<TriangleIndex> trianglesToIndex,
                IEnumerable<Vector2> targetUV,
                IEnumerable<Vector2> sourceUV
            )
            {
                // TODO - このコンストラクタを呼び出してるところをNativeArrayに切り替える
                TrianglesToIndex = new NativeArray<TriangleIndex>(trianglesToIndex.ToArray(), Allocator.TempJob);
                TargetUV = new NativeArray<Vector2>(targetUV.ToArray(), Allocator.TempJob);
                SourceUV = new NativeArray<Vector2>(sourceUV.ToArray(), Allocator.TempJob);

                var self = this;
                TexTransCoreRuntime.NextUpdateCall += () =>
                {
                    self.TrianglesToIndex.Dispose();
                    self.TargetUV.Dispose();
                    self.SourceUV.Dispose();
                };
            }

            public TransData(NativeArray<TriangleIndex> trianglesToIndex, NativeArray<Vector2> targetUV, NativeArray<Vector2> sourceUV)
            {
                TrianglesToIndex = trianglesToIndex;
                TargetUV = targetUV;
                SourceUV = sourceUV;
            }

            public Mesh GenerateTransMesh()
            {
                var mda = Mesh.AllocateWritableMeshData(1);
                var mda_mesh = mda[0];

                mda_mesh.SetVertexBufferParams(
                    TargetUV.Length,
                    new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                    new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, UnsafeUtility.SizeOf<Vector2>() / 4, stream: 1)
                );
                mda_mesh.SetIndexBufferParams(TrianglesToIndex.Length * 3, IndexFormat.UInt32);

                var pos_array = mda_mesh.GetVertexData<Vector3>(0);
                var uv_array = mda_mesh.GetVertexData<Vector2>(1);
                var dst_triangles = mda_mesh.GetIndexData<int>();

                var job1 = new CopyPos { Source = TargetUV, Destination = pos_array }.Schedule(TargetUV.Length, 64);
                var job2 = new CopyJob<Vector2> { Source = SourceUV, Destination = uv_array }.Schedule(SourceUV.Length, 64, job1);
                var job3 = new UnpackTriangleJob { Source = TrianglesToIndex, Destination = dst_triangles }.Schedule(dst_triangles.Length, 64, job2);

                var mesh = new Mesh();

                job3.Complete();

                mda_mesh.subMeshCount = 1;
                mda_mesh.SetSubMesh(0, new SubMeshDescriptor(0, dst_triangles.Length, MeshTopology.Triangles));

                Mesh.ApplyAndDisposeWritableMeshData(mda, mesh);

                return mesh;
            }
        }

        public static NativeArray<Vector4> PackingTrianglesForFrom(Span<TriangleIndex> triangle, Span<Vector3> fromUV, Allocator allocator)
        {
            var na = new NativeArray<Vector4>(triangle.Length * 3, allocator, NativeArrayOptions.UninitializedMemory);
            var sp = na.AsSpan();
            for (var i = 0; triangle.Length > i; i += 1)
            {
                var tri = triangle[i];
                var i1 = i * 3;
                var i2 = i1 + 1;
                var i3 = i1 + 2;

                sp[i1].x = fromUV[tri[0]].x;
                sp[i1].y = fromUV[tri[0]].y;
                sp[i1].z = fromUV[tri[0]].z;
                sp[i1].w = 0f;

                sp[i2].x = fromUV[tri[1]].x;
                sp[i2].y = fromUV[tri[1]].y;
                sp[i2].z = fromUV[tri[1]].z;
                sp[i2].w = 0f;

                sp[i3].x = fromUV[tri[2]].x;
                sp[i3].y = fromUV[tri[2]].y;
                sp[i3].z = fromUV[tri[2]].z;
                sp[i3].w = 0f;
            }
            return na;
        }
        public static NativeArray<Vector4> PackingTrianglesForFrom(Span<TriangleIndex> triangle, Span<Vector2> fromUV, Allocator allocator)
        {
            var na = new NativeArray<Vector4>(triangle.Length * 3, allocator, NativeArrayOptions.UninitializedMemory);
            var sp = na.AsSpan();
            for (var i = 0; triangle.Length > i; i += 1)
            {
                var tri = triangle[i];
                var i1 = i * 3;
                var i2 = i1 + 1;
                var i3 = i1 + 2;

                sp[i1].x = fromUV[tri[0]].x;
                sp[i1].y = fromUV[tri[0]].y;
                sp[i1].z = 0f;
                sp[i1].w = 0f;

                sp[i2].x = fromUV[tri[1]].x;
                sp[i2].y = fromUV[tri[1]].y;
                sp[i2].z = 0f;
                sp[i2].w = 0f;

                sp[i3].x = fromUV[tri[2]].x;
                sp[i3].y = fromUV[tri[2]].y;
                sp[i3].z = 0f;
                sp[i3].w = 0f;
            }
            return na;
        }
        public static NativeArray<Vector2> PackingTrianglesForTo(Span<TriangleIndex> triangle, Span<Vector2> toUV, Allocator allocator)
        {
            var na = new NativeArray<Vector2>(triangle.Length * 3, allocator, NativeArrayOptions.UninitializedMemory);
            var sp = na.AsSpan();
            for (var i = 0; triangle.Length > i; i += 1)
            {
                var tri = triangle[i];
                var i1 = i * 3;
                var i2 = i1 + 1;
                var i3 = i1 + 2;

                sp[i1].x = toUV[tri[0]].x;
                sp[i1].y = toUV[tri[0]].y;

                sp[i2].x = toUV[tri[1]].x;
                sp[i2].y = toUV[tri[1]].y;

                sp[i3].x = toUV[tri[2]].x;
                sp[i3].y = toUV[tri[2]].y;
            }
            return na;
        }
        [UsedImplicitly]
        private static void BurstInstantiate()
        {
            new CopyJob<Vector2>().Schedule(1, 1);
            new CopyJob<Vector3>().Schedule(1, 1);
            new CopyJob<Vector4>().Schedule(1, 1);
        }

        [BurstCompile]
        struct UnpackTriangleJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<TriangleIndex> Source;
            [WriteOnly] public NativeArray<int> Destination;

            public void Execute(int index)
            {
                var tri_index = index / 3;
                var coord = index % 3;

                Destination[index] = Source[tri_index][coord];
            }
        }

        [BurstCompile]
        struct CopyPos : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Vector2> Source;
            [WriteOnly] public NativeArray<Vector3> Destination;

            public void Execute(int index)
            {
                Destination[index] = Source[index];
            }
        }

        [BurstCompile]
        struct CopyJob<T> : IJobParallelFor where T : struct
        {
            [ReadOnly] public NativeArray<T> Source;
            [WriteOnly] public NativeArray<T> Destination;

            public void Execute(int index)
            {
                Destination[index] = Source[index];
            }
        }
        [TexTransInitialize]
        public static void Init()
        {
            s_transShader = Shader.Find(TRANS_SHADER);
            s_depthShader = Shader.Find(DEPTH_WRITER_SHADER);
        }

        public const string TRANS_SHADER = "Hidden/TransTexture";
        static Shader s_transShader;
        public const string DEPTH_WRITER_SHADER = "Hidden/DepthWriter";
        static Shader s_depthShader;


        static Material s_transMat;
        static Material s_depthMat;
        public static void ForTrans(
            RenderTexture targetTexture,
            Texture sourceTexture,
            TransData transUVData,
            float? padding = null,
            TextureWrap? argTexWrap = null,
            bool highQualityPadding = false,
            bool? depthInvert = null,
            bool NotTileNormalize = false
            )
        {
            Profiler.BeginSample("GenerateTransMesh");
            var mesh = transUVData.GenerateTransMesh();
            Profiler.EndSample();

            var preWarp = sourceTexture.wrapMode;

            RenderTexture depthRt = null;

            try
            {
                if (argTexWrap == null) { argTexWrap = TextureWrap.NotWrap; }
                var texWrap = argTexWrap.Value;
                sourceTexture.wrapMode = texWrap.ConvertTextureWrapMode;




                Profiler.BeginSample("Material Setup");
                if (s_transMat == null) { s_transMat = new Material(s_transShader); }
                s_transMat.shaderKeywords = Array.Empty<string>();
                s_transMat.SetTexture("_MainTex", sourceTexture);
                if (padding.HasValue) s_transMat.SetFloat("_Padding", padding.Value);
                if (padding.HasValue && highQualityPadding)
                {
                    mesh.TTNormalCal();
                    s_transMat.EnableKeyword("HighQualityPadding");
                }

                if (texWrap.WarpRange != null)
                {
                    s_transMat.EnableKeyword("WarpRange");
                    s_transMat.SetFloat("_WarpRangeX", texWrap.WarpRange.Value.x);
                    s_transMat.SetFloat("_WarpRangeY", texWrap.WarpRange.Value.y);
                }

                if (NotTileNormalize)
                {
                    s_transMat.EnableKeyword("UnTileNormalize");
                }
                Profiler.EndSample();


                if (depthInvert.HasValue)
                {
                    depthRt = new RenderTexture(targetTexture.width, targetTexture.height, 32, RenderTextureFormat.RFloat);// TODO : I should create TTDepthRt.Get()
                    depthRt.name = $"TransTexture-depthTempRt-{depthRt.width}x{depthRt.height}";

                    s_transMat.EnableKeyword(depthInvert.Value ? "InvertDepth" : "DepthDecal");

                    using (new RTActiveSaver())
                    {
                        if (s_depthMat == null) { s_depthMat = new Material(s_depthShader); }
                        RenderTexture.active = depthRt;

                        s_depthMat.SetPass(0);
                        Profiler.BeginSample("depthInvert DrawMeshNow");
                        Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
                        Profiler.EndSample();
                    }

                    s_transMat.SetTexture("_DepthTex", depthRt);
                }
                else
                {
                    s_transMat.EnableKeyword("NotDepth");
                }




                using (new RTActiveSaver())
                {
                    RenderTexture.active = targetTexture;
                    Profiler.BeginSample("DrawMeshNow");
                    s_transMat.SetPass(0);
                    Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
                    Profiler.EndSample();
                    if (padding != null)
                    {
                        Profiler.BeginSample("DrawMeshNow - padding");
                        s_transMat.SetPass(1);
                        Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
                        Profiler.EndSample();
                    }

                }

            }
            finally
            {
                sourceTexture.wrapMode = preWarp;
                UnityEngine.Object.DestroyImmediate(mesh);
                if (depthRt != null) { UnityEngine.Object.DestroyImmediate(depthRt); }
            }
        }
        public static void ForTrans(
            RenderTexture targetTexture,
            Texture sourceTexture,
            IEnumerable<TransData> transUVDataEnumerable,
            float? padding = null,
            TextureWrap? warpRange = null
            )
        {
            foreach (var transUVData in transUVDataEnumerable)
            {
                ForTrans(targetTexture, sourceTexture, transUVData, padding, warpRange);
            }
        }

        public static void TTNormalCal(this Mesh mesh)
        {
            var vertices = mesh.vertices;
            var posDict = new Dictionary<Vector3, List<Triangle>>();
            var posDictNormal = new Dictionary<Vector3, Vector3>();
            var normal = new Vector3[vertices.Length];


            foreach (var tri in mesh.GetTriangleIndex())
            {
                foreach (var i in tri)
                {
                    if (posDict.ContainsKey(vertices[i]))
                    {
                        posDict[vertices[i]].Add(new Triangle(tri, MemoryMarshal.Cast<Vector3, System.Numerics.Vector3>(vertices.AsSpan())));
                    }
                    else
                    {
                        posDict.Add(vertices[i], new List<Triangle>() { new Triangle(tri, MemoryMarshal.Cast<Vector3, System.Numerics.Vector3>(vertices.AsSpan())) });
                    }
                }
            }

            RagNormalCalTask(posDict, posDictNormal).Wait();

            for (var i = 0; vertices.Length > i; i += 1)
            {
                if (!posDictNormal.ContainsKey(vertices[i])) { continue; }
                normal[i] = posDictNormal[vertices[i]].normalized;
            }

            mesh.normals = normal;


        }

        private static async Task RagNormalCalTask(Dictionary<Vector3, List<Triangle>> posDict, Dictionary<Vector3, Vector3> posDictNormal)
        {
            var tasks = new Dictionary<Vector3, ConfiguredTaskAwaitable<Vector3>>(posDict.Count);
            foreach (var posAndTri in posDict)
            {
                tasks.Add(posAndTri.Key, Task.Run<Vector3>(LocalRagNormalCal).ConfigureAwait(false));
                Vector3 LocalRagNormalCal()
                {
                    return RagNormalCal(posAndTri.Key.ToTTCore(), posAndTri.Value);
                }
            }
            foreach (var task in tasks)
            {
                posDictNormal.Add(task.Key, await task.Value);
            }
        }

        static Vector3 RadToVector3(float rad) => new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0);

        static Vector3 RagNormalCal(System.Numerics.Vector3 pos, List<Triangle> triangle)
        {

            var vecTriList = new List<(System.Numerics.Vector3 VecZero, System.Numerics.Vector3 VecOne)>();
            foreach (var tri in triangle)
            {
                var trList = tri.ToList();
                trList.Remove(pos);
                trList[0] = trList[0] - pos;
                trList[1] = trList[1] - pos;
                if (Mathf.Abs(System.Numerics.Vector3.Cross(trList[0], trList[1]).Z) < float.Epsilon) { continue; }
                if (System.Numerics.Vector3.Cross(trList[0], trList[1]).Z > 0)
                {
                    vecTriList.Add((trList[0], trList[1]));
                }
                else
                {
                    vecTriList.Add((trList[1], trList[0]));
                }
            }

            var ragTriList = new List<(float TriRadZero, float TriRadOne)>();
            foreach (var tri in vecTriList)
            {
                var ragZero = Mathf.Atan2(tri.VecZero.Y, tri.VecZero.X);
                var ragOne = Mathf.Atan2(tri.VecOne.Y, tri.VecOne.X);

                if (!(ragZero > 0 && ragOne < 0)) { ragTriList.Add((ragZero, ragOne)); continue; }

                ragTriList.Add((ragZero, 181 * Mathf.Deg2Rad));
                ragTriList.Add((-181 * Mathf.Deg2Rad, ragOne));
            }


            var rangeList = new List<(float inRad, float OutRad)>();
            var inRadFlag = false;
            (float inRad, float OutRad) temp = (0, 0);
            for (int i = -180; 180 >= i; i += 1)
            {
                var rad = i * Mathf.Deg2Rad;
                var isIn = false;

                foreach (var range in ragTriList)
                {
                    isIn = IsIn(range.TriRadZero, range.TriRadOne, rad);
                    if (isIn) { break; }
                }

                if (!inRadFlag)
                {
                    if (isIn) { continue; }

                    temp.inRad = rad;
                    inRadFlag = true;
                }
                else
                {
                    if (!isIn) { continue; }

                    temp.OutRad = rad;
                    rangeList.Add(temp);
                    temp = (0, 0);
                    inRadFlag = false;
                }
            }
            if (inRadFlag)
            {
                temp.OutRad = 180 * Mathf.Deg2Rad;
                rangeList.Add(temp);
                temp = (0, 0);
                inRadFlag = false;
            }


            rangeList.RemoveAll(I => (I.OutRad - I.inRad) < 3 * Mathf.Deg2Rad);
            if (rangeList.Count == 0) { return Vector3.forward; }

            if (rangeList.Count == 1)
            {
                var range = rangeList[0];
                return RadToVector3(FromMiddle(range.inRad, range.OutRad));
            }

            if (Mathf.Approximately(rangeList[0].inRad, -180 * Mathf.Deg2Rad))
            {
                var first = rangeList[0];
                var last = rangeList[rangeList.Count - 1];
                if (Mathf.Approximately(last.OutRad, 180 * Mathf.Deg2Rad))
                {
                    rangeList.RemoveAt(rangeList.Count - 1);
                    rangeList.RemoveAt(0);
                    rangeList.Add((last.inRad + (-360 * Mathf.Deg2Rad), first.OutRad));
                    if (rangeList.Count == 1)
                    {
                        var Range = rangeList[0];
                        return RadToVector3(FromMiddle(Range.inRad, Range.OutRad));
                    }
                }
            }


            rangeList.Sort((L, R) => Mathf.RoundToInt((Mathf.Abs(L.OutRad - L.inRad) - Mathf.Abs(R.OutRad - R.inRad)) * 100));
            var maxRange = rangeList[rangeList.Count - 1];
            return RadToVector3(FromMiddle(maxRange.inRad, maxRange.OutRad));
        }

        public static bool IsIn(float v1, float v2, float t)
        {
            var min = Mathf.Min(v1, v2);
            var max = Mathf.Max(v1, v2);
            return min < t && t < max;
        }

        public static float FromMiddle(float v1, float v2)
        {
            var min = Mathf.Min(v1, v2);
            var max = Mathf.Max(v1, v2);
            return min + ((max - min) / 2);
        }



    }
}
