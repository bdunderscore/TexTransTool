using System;
using System.Collections.Generic;
using UnityEngine;

namespace net.rs64.TexTransTool.TextureAtlas.FineTuning
{
    [Serializable]
    public struct MipMapRemove : ITextureFineTuning
    {
        public PropertyName PropertyNames;
        public PropertySelect Select;

        public MipMapRemove(PropertyName propertyNames, PropertySelect select)
        {
            PropertyNames = propertyNames;
            Select = select;

        }

        public static MipMapRemove Default => new(PropertyName.DefaultValue, PropertySelect.Equal);

        public void AddSetting(List<TexFineTuningTarget> propAndTextures)
        {
            foreach (var target in FineTuningUtil.FilteredTarget(PropertyNames, Select, propAndTextures))
            {
                var mipMapData = target.TuningDataList.Find(I => I is MipMapData) as MipMapData;
                if (mipMapData != null)
                {
                    mipMapData.UseMipMap = false;
                }
                else
                {
                    target.TuningDataList.Add(new MipMapData() { UseMipMap = false });
                }
            }

        }
    }

    internal class MipMapData : ITuningData
    {
        public bool UseMipMap = true;
    }

    internal class MipMapApplicant : ITuningApplicant
    {
        public int Order => -32;

        public void ApplyTuning(List<TexFineTuningTarget> texFineTuningTargets)
        {
            foreach (var texf in texFineTuningTargets)
            {
                var mipMapData = texf.TuningDataList.Find(I => I is MipMapData) as MipMapData;
                if (mipMapData == null) { continue; }
                if (mipMapData.UseMipMap == texf.Texture2D.mipmapCount > 1) { continue; }

                var newTex = new Texture2D(texf.Texture2D.width, texf.Texture2D.height, TextureFormat.RGBA32, mipMapData.UseMipMap, !texf.Texture2D.isDataSRGB);
                var pixelData = texf.Texture2D.GetPixelData<Color32>(0);
                newTex.SetPixelData(pixelData, 0); pixelData.Dispose();
                newTex.Apply();
                newTex.name = texf.Texture2D.name;
                texf.Texture2D = newTex;
            }
        }
    }

}
