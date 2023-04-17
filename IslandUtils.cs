#if UNITY_EDITOR
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
namespace Rs.TexturAtlasCompiler
{

    public static class IslandUtils
    {
        /*
                public static List<Island> IlandCla1(List<int[]> traiangle, List<Vector2> SourceUV)
                {
                    var sercheList = new List<int[]>(traiangle);
                    var ailandChase = new List<int[]>();
                    var ailandChasePosisonts = new List<Vector2>();
                    var PostIland = new List<(List<int[]>, (Vector2, Vector2))>();

                    bool Firest = true;
                    while (sercheList.Count > 1)
                    {
                        if (Firest)
                        {
                            ailandChase.Add(sercheList[0]);
                            ailandChasePosisonts.AddRange(sercheList[0].ToList().ConvertAll<Vector2>(i => SourceUV[i]));
                            sercheList.RemoveAt(0);
                            Firest = false;
                        }
                        List<int> Deletachase = new List<int>();
                        int cout = 0;
                        foreach (var serchitrainagke in sercheList)
                        {
                            var SertiPositons = serchitrainagke.ToList().ConvertAll<Vector2>(i => SourceUV[i]);
                            bool Exit = false;
                            foreach (var pos in SertiPositons)
                            {
                                if (ailandChasePosisonts.Any(i1 => i1 == pos))
                                {
                                    Exit = true;
                                }
                            }
                            if (Exit)
                            {
                                ailandChase.Add(serchitrainagke);
                                ailandChasePosisonts.AddRange(serchitrainagke.ToList().ConvertAll<Vector2>(i => SourceUV[i]));
                                Deletachase.Add(cout);
                            }
                        }

                        Deletachase.Reverse();
                        Deletachase.ForEach(i => sercheList.RemoveAt(i));

                        //PostIland.Add((new List<int[]>(ailandChase), BoxCal(ailandChasePosisonts))); #

                        ailandChase.Clear();
                        ailandChasePosisonts.Clear();


                    }
                    return PostIland;
                }
        */
        public static List<Island> UVtoIsland(List<TraiangleIndex> traiangles, List<Vector2> UV, int Repeat = 4)
        {
            var Islands = traiangles.ConvertAll<Island>(i => new Island(i));

            int RepCount = -1;
            while (Repeat > RepCount)
            {
                RepCount += 1;
                Islands = IslandCrawling(Islands, UV);
            }
            /*
                    foreach (var PostIland in IlandsPerad)
                    {
                        var PostIlandPoss = new List<Vector2>();
                        foreach (var peatrainagel2 in PostIland)
                        {
                            PostIlandPoss.Add(SourceUV[peatrainagel2[0]]);
                            PostIlandPoss.Add(SourceUV[peatrainagel2[1]]);
                            PostIlandPoss.Add(SourceUV[peatrainagel2[2]]);
                        }
                        Texture2D UVPreView = new Texture2D(512, 512); List<Vector2> PureUV = new List<Vector2>(); DorwUV(PostIlandPoss, UVPreView, Color.green); string AssetPath = "Assets/test3" + Guid.NewGuid().ToString() + ".png"; File.WriteAllBytes(AssetPath, UVPreView.EncodeToPNG());
                    }
                    */
            /*
                        var Ilands = new List<Island>();
                        int coutt = 0;
                        foreach (var Iland in IslandPool)
                        {
                            var Positons = new List<Vector2>();
                            foreach (var Indexs in Iland.trainagels)
                            {
                                Positons.Add(UV[Indexs[0]]);
                                Positons.Add(UV[Indexs[1]]);
                                Positons.Add(UV[Indexs[2]]);
                            }
                            //Ilands.Add((Iland, BoxCal(Positons))); #
                            //Debug.Log(coutt.ToString() + " " + BoxCal(Positons).Item1.ToString() + " " + BoxCal(Positons).Item2.ToString());
                            coutt += 1;
                        }
                        Debug.Log("IlandCout " + Ilands.Count.ToString());
            */
            Islands.ForEach(i => i.BoxCurriculation(UV));
            return Islands;
        }

        public static List<Island> IslandCrawling(List<Island> IslandPool, List<Vector2> UV)
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

                    foreach (var pos in IslandVartPos)
                    {
                        if (CrawlingIslandVartPos.Contains(pos))
                        {
                            IlandJoinIndex = IlandCout;
                            break;
                        }
                    }
                    if (IlandJoinIndex != -1) break;

                }

                if (IlandJoinIndex == -1)
                {
                    CrawlingdIslandPool.Add(Iland);
                }
                else
                {
                    CrawlingdIslandPool[IlandJoinIndex].trainagels.AddRange(Iland.trainagels);
                }

            }
            return CrawlingdIslandPool;
        }

        public static IslandPool IslandPoolEvenlySpaced(IslandPool TargetPool)
        {
            var MovedIslandPool = new IslandPool();
            Vector2 MaxIslandSize = TargetPool.GetLargest().island.GetSize;
            var GridSize = Mathf.CeilToInt(Mathf.Sqrt(TargetPool.IslandPoolList.Count));// Debug.Log("GridSize " + GridSize);
            var CellSize = 1f / GridSize; //Debug.Log("CellSize " + CellSize);
            int Count = 0;
            foreach (var CellIndex in Utils.Reange2d(new Vector2Int(GridSize, GridSize)))
            {
                var CellPos = (Vector2)CellIndex / GridSize; //Debug.Log(maspos.x +" /"+ maspos.y);
                int MapIndex;
                int IslandIndex;
                Island Island;
                if (TargetPool.IslandPoolList.Count > Count)
                {
                    var Target = TargetPool.IslandPoolList[Count];
                    Island = new Island(Target.island);
                    MapIndex = Target.MapIndex;
                    IslandIndex = Target.IslandIndex;
                }
                else
                {
                    break;
                }

                var IslandBox = Island.GetSize;
                Island.MinIlandBox = CellPos;

                var IslandMaxRanege = IslandBox.y < IslandBox.x ? IslandBox.x : IslandBox.y;
                if (IslandMaxRanege > CellSize)
                {
                    IslandBox *= (CellSize / IslandMaxRanege);// Debug.Log("boxsize " + cout2 + "/" + IlandBox.x + "/" + IlandBox.y);
                    IslandBox *= 0.95f;
                }
                Island.MaxIlandBox = CellPos + IslandBox;// Debug.Log("min " + Iland.Item2.Item2.Item1 + "max " + Iland.Item2.Item2.Item2 + "scile " + IlandBox + " " + (maspos + IlandBox).x + "/" + (maspos + IlandBox).y);


                MovedIslandPool.IslandPoolList.Add(new IslandPool.IslandAndIndex(Island, MapIndex, IslandIndex));
                Count += 1;
            }
            return MovedIslandPool;
        }

        public static List<List<Vector2>> UVsMove(List<List<Vector2>> UVs, IslandPool Original, IslandPool Moved)
        {
            List<List<Vector2>> MovedUV = CloneUVs(UVs);

            foreach (var Index in Enumerable.Range(0, Moved.IslandPoolList.Count))
            {
                var MapIndex = Moved.IslandPoolList[Index].MapIndex;
                var MovedIsland = Moved.IslandPoolList[Index];

                var VertexIndex = MovedIsland.island.GetVertexIndex();
                var NotMovedIsland = Original.IslandPoolList.Find(i => i.MapIndex == MovedIsland.MapIndex && i.IslandIndex == MovedIsland.IslandIndex);

                float RelativeScaile = MovedIsland.island.GetSize.sqrMagnitude / NotMovedIsland.island.GetSize.sqrMagnitude;

                foreach (var TrinagleIndex in VertexIndex)
                {
                    var VertPos = UVs[MapIndex][TrinagleIndex];
                    var RelativeVertPos = VertPos - NotMovedIsland.island.MinIlandBox;
                    RelativeVertPos *= RelativeScaile;
                    var MovedVertPos = MovedIsland.island.MinIlandBox + RelativeVertPos;
                    MovedUV[MapIndex][TrinagleIndex] = MovedVertPos;
                    //Debug.Log("not " + notmoved.Item1 + "moved " + moved.Item1 + "f " + uvvart + "scaile " + movedscaile + "rera " + uvverrera + "e " + uvvermoved);
                }
            }

            return MovedUV;
        }
        public static async Task<List<List<Vector2>>> UVsMoveAsync(List<List<Vector2>> UVs, IslandPool Original, IslandPool Moved)
        {
            List<List<Vector2>> MovedUV = CloneUVs(UVs);
            List<ConfiguredTaskAwaitable> Tasks = new List<ConfiguredTaskAwaitable>();

            foreach (var Index in Enumerable.Range(0, Moved.IslandPoolList.Count))
            {
                Tasks.Add(Task.Run(() => MoveUV(UVs, Original, Moved, MovedUV, Index)).ConfigureAwait(false));
            }
            foreach (var task in Tasks)
            {
                await task;
            }
            return MovedUV;




        }
        static void MoveUV(List<List<Vector2>> UVs, IslandPool Original, IslandPool Moved, List<List<Vector2>> MovedUV, int Index)
        {
            var MapIndex = Moved.IslandPoolList[Index].MapIndex;
            var MovedIsland = Moved.IslandPoolList[Index];

            var VertexIndex = MovedIsland.island.GetVertexIndex();
            var NotMovedIsland = Original.IslandPoolList.Find(i => i.MapIndex == MovedIsland.MapIndex && i.IslandIndex == MovedIsland.IslandIndex);

            float RelativeScaile = MovedIsland.island.GetSize.sqrMagnitude / NotMovedIsland.island.GetSize.sqrMagnitude;

            foreach (var TrinagleIndex in VertexIndex)
            {
                var VertPos = UVs[MapIndex][TrinagleIndex];
                var RelativeVertPos = VertPos - NotMovedIsland.island.MinIlandBox;
                RelativeVertPos *= RelativeScaile;
                var MovedVertPos = MovedIsland.island.MinIlandBox + RelativeVertPos;
                MovedUV[MapIndex][TrinagleIndex] = MovedVertPos;
                //Debug.Log("not " + notmoved.Item1 + "moved " + moved.Item1 + "f " + uvvart + "scaile " + movedscaile + "rera " + uvverrera + "e " + uvvermoved);
            }
        }

        public static IslandPool GeneretIslandPool(this CompileData Data)
        {
            var IslandPool = new IslandPool();

            int MapCount = -1;
            foreach (var data in Data.meshes)
            {
                MapCount += 1;
                var UV = new List<Vector2>();
                data.GetUVs(0, UV);
                var Triangle = AtlasMapper.ToList(data.triangles);
                IslandPool.IslandPoolList.AddRange(GeneretIslandAndIndex(UV, Triangle, MapCount));
            }
            return IslandPool;
        }

        public static async Task<IslandPool> AsyncGeneretIslandPool(this CompileData Data)
        {
            var IslandPool = new IslandPool();

            int MapCount = -1;
            List<ConfiguredTaskAwaitable<List<IslandPool.IslandAndIndex>>> Tesks = new List<ConfiguredTaskAwaitable<List<IslandPool.IslandAndIndex>>>();
            foreach (var data in Data.meshes)
            {
                MapCount += 1;
                var mapcount = MapCount;//Asyncな奴に投げている関係かこうしないとばぐるたぶん
                var UV = new List<Vector2>();
                data.GetUVs(0, UV);
                var Triangle = AtlasMapper.ToList(data.triangles);
                Tesks.Add(Task.Run<List<IslandPool.IslandAndIndex>>(() => GeneretIslandAndIndex(UV, Triangle, mapcount)).ConfigureAwait(false));
            }
            foreach (var task in Tesks)
            {
                IslandPool.IslandPoolList.AddRange(await task);
            }

            return IslandPool;

        }
        static List<IslandPool.IslandAndIndex> GeneretIslandAndIndex(List<Vector2> UV, List<TraiangleIndex> traiangles, int MapCount)
        {
            Debug.Log(MapCount);
            var Islanads = IslandUtils.UVtoIsland(traiangles, UV);
            var IslandPoolList = new List<IslandPool.IslandAndIndex>();
            int IlandIndex = -1;
            foreach (var Islnad in Islanads)
            {
                IlandIndex += 1;
                IslandPoolList.Add(new IslandPool.IslandAndIndex(Islnad, MapCount, IlandIndex));
            }
            return IslandPoolList;
        }
        public static List<List<Vector2>> GetUVs(this CompileData Data, int UVindex = 0)
        {
            var UVs = new List<List<Vector2>>();

            foreach (var Mesh in Data.meshes)
            {
                var UV = new List<Vector2>();
                Mesh.GetUVs(UVindex, UV);
                UVs.Add(UV);
            }
            return UVs;
        }
        public static void SetUVs(this CompileData Data, List<List<Vector2>> UVs, int UVindex = 0)
        {
            int Count = -1;
            foreach (var Mesh in Data.meshes)
            {
                Count += 1;
                Mesh.SetUVs(UVindex, UVs[Count]);
            }
        }


        public static List<List<Vector2>> CloneUVs(List<List<Vector2>> UVs)
        {
            var Clone = new List<List<Vector2>>();

            foreach (var uv in UVs)
            {
                Clone.Add(new List<Vector2>(uv));
            }
            return Clone;
        }
    }

    public class IslandPool
    {
        public List<IslandAndIndex> IslandPoolList = new List<IslandAndIndex>();
        public class IslandAndIndex
        {
            public IslandAndIndex(Island island, int mapIndex, int islandInx)
            {
                this.island = new Island(island);
                MapIndex = mapIndex;
                IslandIndex = islandInx;
            }

            public IslandAndIndex(IslandAndIndex Souse)
            {
                this.island = Souse.island;
                MapIndex = Souse.MapIndex;
                IslandIndex = Souse.MapIndex;
            }

            public Island island { get; set; }
            public int MapIndex { get; set; }
            public int IslandIndex { get; set; }
        }

        public IslandAndIndex GetLargest()
        {
            int GetIndex = -1;
            int Count = -1;
            Vector2 Cash = new Vector2(0, 0);
            foreach (var islandandi in IslandPoolList)
            {
                Count += 1;
                if (Cash.sqrMagnitude < islandandi.island.GetSize.sqrMagnitude)
                {
                    Cash = islandandi.island.GetSize;
                    GetIndex = Count;
                }
            }
            if (GetIndex != -1)
            {
                return IslandPoolList[GetIndex];
            }
            else
            {
                return null;
            }
        }
    }
    public class Island
    {
        public List<TraiangleIndex> trainagels = new List<TraiangleIndex>();
        public Vector2 MinIlandBox;
        public Vector2 MaxIlandBox;
        public Vector2 GetSize { get => MaxIlandBox - MinIlandBox; }

        public Island(Island Souse)
        {
            trainagels = new List<TraiangleIndex>(Souse.trainagels);
            MinIlandBox = Souse.MinIlandBox;
            MaxIlandBox = Souse.MaxIlandBox;
        }
        public Island(TraiangleIndex traiangleIndex)
        {
            trainagels.Add(traiangleIndex);
        }
        public Island()
        {

        }
        public List<int> GetVertexIndex()
        {
            var IndexList = new List<int>();
            foreach (var traiangle in trainagels)
            {
                IndexList.AddRange(traiangle.ToArray());
            }
            return IndexList;
        }
        public List<Vector2> GetVertexPos(List<Vector2> SouseUV)
        {
            var VIndexs = GetVertexIndex();
            return VIndexs.ConvertAll<Vector2>(i => SouseUV[i]);
        }
        public void BoxCurriculation(List<Vector2> SouseUV)
        {
            var VartPoss = GetVertexPos(SouseUV);
            var Box = AtlasMapper.BoxCal(VartPoss);
            MinIlandBox = Box.Item1;
            MaxIlandBox = Box.Item2;
        }

    }


    public static class IlandUtilsDebug
    {
        public static void DorwUV(List<Vector2> UV, Texture2D TargetTextur, Color WriteColor)
        {
            foreach (var uvpos in UV)
            {
                int x = Mathf.RoundToInt(uvpos.x * TargetTextur.width);
                int y = Mathf.RoundToInt(uvpos.y * TargetTextur.height);
                TargetTextur.SetPixel(x, y, WriteColor);
            }
        }
    }
}
#endif