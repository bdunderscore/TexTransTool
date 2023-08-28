#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using net.rs64.TexTransTool.TextureAtlas;
using System.Collections;
using net.rs64.TexTransTool;

namespace net.rs64.TexTransTool.Island
{

    public static class IslandUtils
    {
        public static List<Island> CachingUVtoIsland(IReadOnlyList<TriangleIndex> triangles, IReadOnlyList<Vector2> UV)
        {
            CacheGet(out var CacheIslands, out var diffCacheIslands);

            var IslandPool = UVtoIsland(triangles, UV, CacheIslands);

            CacheSave(CacheIslands, diffCacheIslands);
            return IslandPool;
        }

        public static void CacheGet(out List<IslandCacheObject> CacheIslands, out List<IslandCacheObject> diffCacheIslands)
        {
            CacheIslands = AssetSaveHelper.LoadAssets<IslandCache>().ConvertAll(i => i.CacheObject);
            diffCacheIslands = new List<IslandCacheObject>(CacheIslands);
        }
        public static void CacheSave(List<IslandCacheObject> CacheIslands, List<IslandCacheObject> diffCacheIslands)
        {
            AssetSaveHelper.SaveAssets(CacheIslands.Except(diffCacheIslands).Select(i =>
            {
                var NI = ScriptableObject.CreateInstance<IslandCache>();
                NI.CacheObject = i; NI.name = "IslandCache";
                return NI;
            }));
        }

        public static List<Island> UVtoIsland(IReadOnlyList<TriangleIndex> triangles, IReadOnlyList<Vector2> UV, List<IslandCacheObject> Caches = null)
        {
            var NawHash = IslandCacheObject.GenerateHash(triangles, UV);
            if (Caches != null)
            {
                foreach (var Cache in Caches)
                {
                    if (Cache.Hash.SequenceEqual(NawHash))
                    {
                        //Debug.Log("Use Cache!");
                        return Cache.Islands;
                    }
                }
            }

            var Islands = triangles.Select(i => new Island(i)).ToList();

            bool Continue = true;
            while (Continue)
            {
                Continue = false;
                Islands = IslandCrawling(Islands, UV, ref Continue);
            }
            Islands.ForEach(i => i.BoxCurriculation(UV));

            if (Caches != null)
            {
                var NewCache = new IslandCacheObject(NawHash, Islands);

                Caches.Add(NewCache);
            }

            return Islands;
        }

        public static List<Island> IslandCrawling(IReadOnlyList<Island> IslandPool, IReadOnlyList<Vector2> UV, ref bool IsJoin)
        {

            var CrawlingdIslandPool = new List<Island>();

            foreach (var Iland in IslandPool)
            {
                var IslandVartPos = Iland.GetVertexPos(UV);


                int IlandCout = -1;
                int IlandJoinIndex = -1;

                foreach (var CrawlingdIsland in CrawlingdIslandPool)
                {
                    IlandCout += 1;

                    var CrawlingIslandVartPos = CrawlingdIsland.GetVertexPos(UV);


                    if (IslandVartPos.Intersect(CrawlingIslandVartPos).Any())
                    {
                        IlandJoinIndex = IlandCout;
                        break;
                    }

                }

                if (IlandJoinIndex == -1)
                {
                    CrawlingdIslandPool.Add(Iland);
                }
                else
                {
                    CrawlingdIslandPool[IlandJoinIndex].triangles.AddRange(Iland.triangles);
                    IsJoin = true;
                }

            }
            return CrawlingdIslandPool;
        }
        public static void IslandMoveUV(List<Vector2> UV, List<Vector2> MoveUV, Island OriginIsland, Island MovedIsland)
        {
            if (OriginIsland.Is90Ratation == MovedIsland.Is90Ratation)
            {
                var mSize = MovedIsland.Size;
                var nmSize = OriginIsland.Size;

                var RelativeScale = new Vector2(mSize.x / nmSize.x, mSize.y / nmSize.y);
                RelativeScale.x = float.IsNaN(RelativeScale.x) ? 0 : RelativeScale.x;
                RelativeScale.y = float.IsNaN(RelativeScale.y) ? 0 : RelativeScale.y;

                foreach (var VartIndex in OriginIsland.GetVertexIndex())
                {
                    var VertPos = UV[VartIndex];
                    var RelativeVertPos = VertPos - OriginIsland.Pivot;

                    RelativeVertPos.x *= RelativeScale.x;
                    RelativeVertPos.y *= RelativeScale.y;

                    var MovedVertPos = MovedIsland.Pivot + RelativeVertPos;
                    MoveUV[VartIndex] = MovedVertPos;
                }
            }
            else
            {
                var mSize = MovedIsland.Is90Ratation ? new Vector2(MovedIsland.Size.y, MovedIsland.Size.x) : MovedIsland.Size;
                var nmSize = OriginIsland.Is90Ratation ? new Vector2(OriginIsland.Size.y, OriginIsland.Size.x) : OriginIsland.Size;

                var RelativeScale = new Vector2(mSize.x / nmSize.x, mSize.y / nmSize.y);
                var IsRotRight = MovedIsland.Is90Ratation;
                var Rotate = Quaternion.Euler(0, 0, IsRotRight ? -90 : 90);

                foreach (var VartIndex in OriginIsland.GetVertexIndex())
                {
                    var VertPos = UV[VartIndex];
                    var RelativeVertPos = VertPos - OriginIsland.Pivot;

                    RelativeVertPos.x *= RelativeScale.x;
                    RelativeVertPos.y *= RelativeScale.y;

                    RelativeVertPos = Rotate * RelativeVertPos;

                    var MovedVertPos = MovedIsland.Pivot + RelativeVertPos;

                    if (IsRotRight) { MovedVertPos.y += MovedIsland.Size.y; }
                    else { MovedVertPos.x += MovedIsland.Size.x; }

                    MoveUV[VartIndex] = MovedVertPos;
                }
            }
        }
        public static void IslandPoolMoveUV<T>(List<Vector2> UV, List<Vector2> MoveUV, TagIslandPool<T> OriginPool, TagIslandPool<T> MovedPool)
        {
            if (UV.Count != MoveUV.Count) throw new Exception("UV.Count != MoveUV.Count 中身が同一頂点数のUVではありません。");
            foreach (var island in MovedPool)
            {
                var OriginIsland = OriginPool.FindTag(island.tag);
                IslandMoveUV(UV, MoveUV, OriginIsland.island, island.island);
            }
        }

        public static void IslandPoolSizeOffset<T>(this TagIslandPool<T> IslandPool, float Offset)
        {
            foreach (var islandI in IslandPool)
            {
                var island = islandI.island;
                island.Size *= Offset;
            }
        }



    }
    public static class IslandSorting
    {
        public enum IslandSortingType
        {
            EvenlySpaced,
            NextFitDecreasingHeight,
            NextFitDecreasingHeightPlusFloorCeilineg,
        }
        public static void GenerateMovedIlands<T>(IslandSortingType SortingType, TagIslandPool<T> IslandPool, float Padding = 0.01f)
        {
            switch (SortingType)
            {
                case IslandSortingType.EvenlySpaced:
                    {
                        IslandSorting.IslandPoolEvenlySpaced(IslandPool);
                        break;
                    }
                case IslandSortingType.NextFitDecreasingHeight:
                    {
                        IslandSorting.IslandPoolNextFitDecreasingHeight(IslandPool, Padding);
                        break;
                    }
                case IslandSortingType.NextFitDecreasingHeightPlusFloorCeilineg:
                    {
                        IslandSorting.IslandPoolNextFitDecreasingHeightPlusFloorCeilineg(IslandPool, Padding);
                        break;
                    }

                default: throw new ArgumentException();
            }
        }
        public static TagIslandPool<T> IslandPoolNextFitDecreasingHeight<T>(TagIslandPool<T> TargetPool, float IslanadsPadding = 0.01f, float ClorreScale = 0.01f, float MinHeight = 0.75f, int MaxLoopCount = 128)//NFDH
        {
            var Islands = TargetPool.Islands;
            if (!Islands.Any()) return TargetPool;
            foreach (var Island in Islands) { if (Island.Size.y > Island.Size.x) { Island.Rotate90(); } }
            Islands.Sort((l, r) => Mathf.RoundToInt((r.Size.y - l.Size.y) * 100));
            bool Success = false;
            float NawScale = 1f;
            int loopCount = -1;

            while (!Success && MaxLoopCount > loopCount)
            {
                loopCount += 1;
                Success = true;

                var NawPos = new Vector2(IslanadsPadding, IslanadsPadding);
                float FirstHeight = Islands[0].island.Size.y;
                var NawHeight = IslanadsPadding + FirstHeight + IslanadsPadding;

                foreach (var islandandIndex in Islands)
                {
                    var Island = islandandIndex.island;
                    var NawSize = Island.Size;
                    var NawMaxPos = NawPos + NawSize;
                    var IsOutOfX = (NawMaxPos.x + IslanadsPadding) > 1;

                    if (IsOutOfX)
                    {
                        NawPos.y = NawHeight;
                        NawPos.x = IslanadsPadding;

                        NawHeight += IslanadsPadding + NawSize.y;

                        if (NawHeight > 1)
                        {

                            Success = false;

                            ScaleAppry(1 - ClorreScale);
                            break;
                        }
                    }

                    Island.Pivot = NawPos;

                    NawPos.x += IslanadsPadding + NawSize.x;
                }

                if (Success && MinHeight > NawHeight)
                {
                    Success = false;
                    ScaleAppry(1 + ClorreScale);
                }

            }

            return TargetPool;

            void ScaleAppry(float Scale)
            {
                foreach (var islandandIndex in Islands)
                {
                    var Island = islandandIndex.island;
                    Island.Size *= Scale;
                }
                NawScale *= Scale;
            }
        }

        public static TagIslandPool<T> IslandPoolNextFitDecreasingHeightPlusFloorCeilineg<T>(TagIslandPool<T> TargetPool, float IslanadsPadding = 0.01f, float ClorreScale = 0.01f, float MinHeight = 0.75f, int MaxLoopCount = 128)//NFDH
        {
            var Islands = TargetPool.Islands;
            if (!Islands.Any()) return TargetPool;
            foreach (var Island in Islands) { if (Island.Size.y > Island.Size.x) { Island.Rotate90(); } }
            Islands.Sort((l, r) => Mathf.RoundToInt((r.Size.y - l.Size.y) * 100));
            bool Success = false;
            float NawScale = 1f;
            int loopCount = -1;

            while (!Success && MaxLoopCount > loopCount)
            {
                loopCount += 1;
                Success = true;

                var Boxs = new List<UVWithBox>();


                foreach (var islandandIndex in Islands)
                {
                    var Result = false;
                    foreach (var withbox in Boxs)
                    {
                        Result = withbox.TrySetBox(islandandIndex);
                        if (Result) { break; }

                    }
                    if (!Result)
                    {
                        var Floor = Boxs.Any() ? Boxs.Last().Ceil + IslanadsPadding : IslanadsPadding;
                        var Ceil = islandandIndex.island.Size.y + Floor;
                        var newWithBox = new UVWithBox(Ceil, Floor, IslanadsPadding);
                        var res = newWithBox.TrySetBox(islandandIndex);
                        Boxs.Add(newWithBox);
                    }
                }

                var LastHeigt = Boxs.Last().Ceil + IslanadsPadding;
                Success = LastHeigt < 1;

                if (!Success)
                {
                    ScaleAppry(1 - ClorreScale);
                }

                if (Success && MinHeight > LastHeigt)
                {
                    Success = false;
                    ScaleAppry(1 + ClorreScale);
                }


            }

            return TargetPool;

            void ScaleAppry(float Scale)
            {
                foreach (var islandandIndex in Islands)
                {
                    var Island = islandandIndex.island;
                    Island.Size *= Scale;
                }
                NawScale *= Scale;
            }
        }

        private class UVWithBox
        {
            public float with = 1;
            public float Padding;
            public float Ceil;
            public float Floor;
            public float Haight => Ceil - Floor;
            public List<Island> Upper = new List<Island>();
            public List<Island> Lower = new List<Island>();

            public UVWithBox(float height, float floor, float padding)
            {
                Ceil = height;
                Floor = floor;
                Padding = padding;
            }

            public bool TrySetBox(Island Box)
            {
                var Island = Box;
                if (Haight + 0.01f < Island.Size.y) return false;


                var withMin = Lower.Any() ? Lower.Last().GetMaxPos.x : 0;
                var withMax = GetCeilWithEmpty(Mathf.Clamp(Floor + Island.Size.y + Padding, Floor, Ceil));
                var withSize = withMax - withMin;
                if (withSize > Padding + Island.Size.x + Padding)
                {
                    Island.Pivot = new Vector2(withMin + Padding, Floor);
                    Lower.Add(Box);
                    return true;
                }


                withMin = GetFloorWithEmpty(Mathf.Clamp(Ceil - Island.Size.y - Padding, Floor, Ceil));
                withMax = Upper.Any() ? Upper.Last().Pivot.x : with;
                withSize = withMax - withMin;
                if (withSize > Padding + Island.Size.x + Padding)
                {
                    Island.Pivot = new Vector2(withMax - Island.Size.x - Padding, Ceil - Island.Size.y);
                    Upper.Add(Box);
                    return true;
                }


                return false;
            }

            public float GetFloorWithEmpty(float TargetHeight)
            {
                if (!Utils.InRange(Floor, Ceil, TargetHeight)) { throw new Exception("TargetHeight is not in range!"); }

                var MinWith = 0f;

                foreach (var Box in Lower)
                {
                    var Island = Box;
                    if (Utils.InRange(Island.Pivot.y, Island.GetMaxPos.y, TargetHeight))
                    {
                        if (MinWith < Island.GetMaxPos.x) { MinWith = Island.GetMaxPos.x; }
                    }
                }



                return MinWith;
            }
            public float GetCeilWithEmpty(float TargetHeight)
            {
                if (!Utils.InRange(Floor, Ceil, TargetHeight)) throw new Exception("TargetHeight is not in range!");

                var MaxWith = with;

                foreach (var Box in Upper)
                {
                    var Island = Box;
                    if (Utils.InRange(Island.Pivot.y, Island.GetMaxPos.y, TargetHeight))
                    {
                        if (Island.GetMaxPos.x < MaxWith) { MaxWith = Island.GetMaxPos.x; }
                    }
                }

                return MaxWith;
            }
        }

        public static TagIslandPool<T> IslandPoolEvenlySpaced<T>(TagIslandPool<T> TargetPool)
        {
            Vector2 MaxIslandSize = TargetPool.GetLargest().island.Size;
            var GridSize = Mathf.CeilToInt(Mathf.Sqrt(TargetPool.Islands.Count));
            var CellSize = 1f / GridSize;
            int Count = 0;
            foreach (var CellIndex in Utils.Reange2d(new Vector2Int(GridSize, GridSize)))
            {
                var CellPos = (Vector2)CellIndex / GridSize;
                Island Island;
                if (TargetPool.Islands.Count > Count)
                {
                    var Target = TargetPool.Islands[Count];
                    Island = Target.island;
                }
                else
                {
                    break;
                }

                var IslandBox = Island.Size;
                Island.Pivot = CellPos;

                var IslandMaxRanege = IslandBox.y < IslandBox.x ? IslandBox.x : IslandBox.y;
                if (IslandMaxRanege > CellSize)
                {
                    IslandBox *= (CellSize / IslandMaxRanege);
                    IslandBox *= 0.95f;
                }
                Island.Size = IslandBox;

                Count += 1;
            }
            return TargetPool;
        }
    }
    public class TagIslandPool<Tag> : IEnumerable<TagIsland<Tag>>
    {
        public List<TagIsland<Tag>> Islands = new List<TagIsland<Tag>>();

        public TagIsland<Tag> this[int Index] => Islands[Index];

        public HashSet<Tag> GetTag()
        {
            HashSet<Tag> tags = new HashSet<Tag>();
            foreach (var island in Islands)
            {
                tags.Add(island.tag);
            }
            return tags;
        }
        public void AddIsland(TagIsland<Tag> item)
        {
            Islands.Add(item);
        }
        public void AddIsland(Island item, Tag tag)
        {
            Islands.Add(new TagIsland<Tag>(item, tag, false));
        }
        public void AddRangeIsland(List<TagIsland<Tag>> items)
        {
            foreach (var item in items)
            {
                AddIsland(item);
            }
        }
        public void AddRangeIsland(List<Island> items, Tag tag)
        {
            foreach (var item in items)
            {
                AddIsland(item, tag);
            }
        }
        public void AddRangeIsland(TagIslandPool<Tag> nawChannnelAtlasIslandPool)
        {
            foreach (var item in nawChannnelAtlasIslandPool)
            {
                AddIsland(item);
            }
        }
        public int RemoveAll(Tag tag)
        {
            return Islands.RemoveAll(I => I.tag.Equals(tag));
        }
        public TagIsland<Tag> FindTag(Tag tag)
        {
            return Islands.Find(I => I.tag.Equals(tag));
        }
        public IEnumerator<TagIsland<Tag>> GetEnumerator()
        {
            return Islands.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Islands.GetEnumerator();
        }

        public TagIslandPool<Tag> Clone()
        {
            var NewPool = new TagIslandPool<Tag>();
            foreach (var island in Islands)
            {
                NewPool.AddIsland(new TagIsland<Tag>(island.island, island.tag, true));
            }
            return NewPool;
        }


        public TagIsland<Tag> GetLargest()
        {
            int GetIndex = -1;
            int Count = -1;
            Vector2 Cash = new Vector2(0, 0);
            foreach (var islandandI in Islands)
            {
                Count += 1;
                var Island = islandandI;
                if (Cash.sqrMagnitude < Island.Size.sqrMagnitude)
                {
                    Cash = islandandI.Size;
                    GetIndex = Count;
                }
            }
            if (GetIndex != -1)
            {
                return Islands[GetIndex];
            }
            else
            {
                return null;
            }
        }


    }
    public class TagIsland<Tag> : Island
    {
        public Tag tag;
        /// <summary>
        /// 互換性のため
        /// </summary>
        public Island island => this;
        public TagIsland(Island Souse, Tag tag, bool DeepClone = false)
        {
            if (DeepClone)
            {
                triangles = new List<TriangleIndex>(Souse.triangles);
            }
            else
            {
                triangles = Souse.triangles;
            }
            Pivot = Souse.Pivot;
            Size = Souse.Size;
            this.tag = tag;
        }
        public TagIsland(TagIsland<Tag> Souse, bool DeepClone = false)
        {
            if (DeepClone)
            {
                triangles = new List<TriangleIndex>(Souse.triangles);
            }
            else
            {
                triangles = Souse.triangles;
            }
            Pivot = Souse.Pivot;
            Size = Souse.Size;
            tag = Souse.tag;
        }
        public TagIsland()
        {

        }
    }

    [Serializable]
    public class Island
    {
        public List<TriangleIndex> triangles = new List<TriangleIndex>();
        public Vector2 Pivot;
        public Vector2 Size;
        public bool Is90Ratation;

        public Vector2 GetMaxPos => Pivot + Size;

        public Island(Island Souse)
        {
            triangles = new List<TriangleIndex>(Souse.triangles);
            Pivot = Souse.Pivot;
            Size = Souse.Size;
            Is90Ratation = Souse.Is90Ratation;
        }
        public Island(TriangleIndex triangleIndex)
        {
            triangles.Add(triangleIndex);
        }
        public Island()
        {

        }
        public List<int> GetVertexIndex()
        {
            var IndexList = new List<int>();
            foreach (var triangle in triangles)
            {
                IndexList.AddRange(triangle.ToArray());
            }
            return IndexList;
        }
        public List<Vector2> GetVertexPos(IReadOnlyList<Vector2> SouseUV)
        {
            var VIndexs = GetVertexIndex();
            return VIndexs.ConvertAll<Vector2>(i => SouseUV[i]);
        }
        public void BoxCurriculation(IReadOnlyList<Vector2> SouseUV)
        {
            var VartPoss = GetVertexPos(SouseUV);
            var Box = TransMapper.BoxCal(VartPoss);
            Pivot = Box.Item1;
            Size = Box.Item2 - Box.Item1;
        }

        public bool BoxInOut(Vector2 TargetPos)
        {
            var RelaTargetPos = TargetPos - Pivot;
            return !((RelaTargetPos.x < 0 || RelaTargetPos.y < 0) || (RelaTargetPos.x > Size.x || RelaTargetPos.y > Size.y));
        }
        public List<Vector2> GenerateRectVart(float padding = 0)
        {
            padding = Mathf.Abs(padding);
            var Varts = new List<Vector2>();
            if (!Is90Ratation)
            {
                Varts.Add(Pivot + new Vector2(-padding, -padding));
                Varts.Add(new Vector2(Pivot.x, Pivot.y + Size.y) + new Vector2(-padding, padding));
                Varts.Add(Pivot + Size + new Vector2(padding, padding));
                Varts.Add(new Vector2(Pivot.x + Size.x, Pivot.y) + new Vector2(padding, -padding));
            }
            else
            {
                Varts.Add(new Vector2(Pivot.x, Pivot.y + Size.y) + new Vector2(-padding, padding));
                Varts.Add(Pivot + Size + new Vector2(padding, padding));
                Varts.Add(new Vector2(Pivot.x + Size.x, Pivot.y) + new Vector2(padding, -padding));
                Varts.Add(Pivot + new Vector2(-padding, -padding));
            }
            return Varts;
        }

        public void Rotate90()
        {
            Is90Ratation = !Is90Ratation;
            (Size.x, Size.y) = (Size.y, Size.x);
        }

    }


    public static class IslandUtilsDebug
    {
        public static void DorwUV(List<Vector2> UV, Texture2D TargetTextur, Color WriteColor)
        {
            foreach (var uvpos in UV)
            {
                if (0 <= uvpos.x && uvpos.x <= 1 && 0 <= uvpos.y && uvpos.y <= 1) continue;
                int x = Mathf.RoundToInt(uvpos.x * TargetTextur.width);
                int y = Mathf.RoundToInt(uvpos.y * TargetTextur.height);
                TargetTextur.SetPixel(x, y, WriteColor);
            }
        }
        public static void DrawerIlandBox<T>(TagIslandPool<T> Pool, Texture2D TargetTextur, Color WriteColor)
        {
            foreach (var island in Pool.Islands)
            {
                var minpos = new Vector2Int(Mathf.RoundToInt(island.island.Pivot.x * TargetTextur.width), Mathf.RoundToInt(island.island.Pivot.y * TargetTextur.height));
                var maxpos = new Vector2Int(Mathf.RoundToInt(island.island.GetMaxPos.x * TargetTextur.width), Mathf.RoundToInt(island.island.GetMaxPos.y * TargetTextur.height));
                Vector2Int pos = minpos;
                while (maxpos.x > pos.x)
                {
                    TargetTextur.SetPixel(pos.x, pos.y, WriteColor);
                    pos.x += 1;
                }
                pos.x = minpos.x;
                pos.y = maxpos.y;
                while (maxpos.x > pos.x)
                {
                    TargetTextur.SetPixel(pos.x, pos.y, WriteColor);
                    pos.x += 1;
                }

                pos = minpos;
                while (maxpos.y > pos.y)
                {
                    TargetTextur.SetPixel(pos.x, pos.y, WriteColor);
                    pos.y += 1;
                }
                pos.x = maxpos.x;
                pos.y = minpos.y;
                while (maxpos.y > pos.y)
                {
                    TargetTextur.SetPixel(pos.x, pos.y, WriteColor);
                    pos.y += 1;
                }
            }
        }
    }
}
#endif
