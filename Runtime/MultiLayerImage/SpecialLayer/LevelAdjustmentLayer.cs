#nullable enable
using System;
using net.rs64.TexTransCore.MultiLayerImageCanvas;
using UnityEngine;
namespace net.rs64.TexTransTool.MultiLayerImage
{

    [AddComponentMenu(TexTransBehavior.TTTName + "/" + MenuPath)]
    public class LevelAdjustmentLayer : AbstractGrabLayer
    {
        internal const string ComponentName = "TTT LevelAdjustmentLayer";
        internal const string MenuPath = MultiLayerImageCanvas.FoldoutName + "/" + ComponentName;
        public Level RGB = new();
        public Level Red = new();
        public Level Green = new();
        public Level Blue = new();
        internal override LayerObject<TTCE4U> GetLayerObject<TTCE4U>(TTCE4U engine)
        {
            return new GrabBlendingAsLayer<TTCE4U>(Visible, GetAlphaMask(engine), Clipping, engine.QueryBlendKey(BlendTypeKey), new LevelAdjustment(RGB.ToTTCoreLevelData(), Red.ToTTCoreLevelData(), Green.ToTTCoreLevelData(), Blue.ToTTCoreLevelData()));
        }

        [Serializable]
        public class Level
        {
            [Range(0, 0.99f)] public float InputFloor = 0;
            [Range(0.01f, 1)] public float InputCeiling = 1;

            [Range(0.1f, 9.9f)] public float Gamma = 1;

            [Range(0, 1)] public float OutputFloor = 0;
            [Range(0, 1)] public float OutputCeiling = 1;

            internal void SetMaterialProperty(Material material)
            {
                material.SetFloat("_InputFloor", InputFloor);
                material.SetFloat("_InputCeiling", InputCeiling);
                material.SetFloat("_Gamma", Gamma);
                material.SetFloat("_OutputFloor", OutputFloor);
                material.SetFloat("_OutputCeiling", OutputCeiling);
            }

            public LevelData ToTTCoreLevelData()
            {
                var levelData = new LevelData();
                levelData.InputFloor = InputFloor;
                levelData.InputCeiling = InputCeiling;
                levelData.Gamma = Gamma;
                levelData.OutputFloor = OutputFloor;
                levelData.OutputCeiling = OutputCeiling;
                return levelData;
            }
        }
    }
}
