#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using net.rs64.TexTransTool.TextureAtlas;
using net.rs64.TexTransTool.Utils;
using UnityEditor;
using UnityEngine;

namespace net.rs64.TexTransTool.Migration.V0
{
    [Obsolete]
    internal static class AtlasTextureV0
    {
        public static void MigrationAtlasTextureV0ToV1(AtlasTexture atlasTexture)
        {
            if (atlasTexture == null) { Debug.LogWarning("マイグレーションターゲットが存在しません。"); return; }
            if (atlasTexture.SaveDataVersion != 0) { Debug.Log(atlasTexture.name + " AtlasTexture : マイグレーションのバージョンが違います。"); return; }
            if (atlasTexture.AtlasSettings.Count < 1) { Debug.LogWarning(atlasTexture.name + " AtlasTexture : マイグレーション不可能なアトラステクスチャーです。"); return; }

            var GameObject = atlasTexture.gameObject;

            if (atlasTexture.AtlasSettings.Count == 1 && !atlasTexture.MigrationV0ObsoleteChannelsRef.Where(I => I != null).Any())
            {
                MigrateSettingV0ToV1(atlasTexture, 0, atlasTexture);
            }
            else
            {
                if (atlasTexture.MigrationV0ObsoleteChannelsRef.Where(I => I != null).Any())
                {
                    for (int Count = 0; atlasTexture.AtlasSettings.Count > Count; Count += 1)
                    {
                        if (atlasTexture.MigrationV0ObsoleteChannelsRef.Count <= Count || atlasTexture.MigrationV0ObsoleteChannelsRef[Count] == null)
                        {
                            CreateChannel(Count);
                        }
                        else
                        {
                            var newAtlasTexture = atlasTexture.MigrationV0ObsoleteChannelsRef[Count];
                            MigrateSettingV0ToV1(atlasTexture, Count, newAtlasTexture);
                            EditorUtility.SetDirty(newAtlasTexture);
                        }
                    }

                    if (atlasTexture.AtlasSettings.Count < atlasTexture.MigrationV0ObsoleteChannelsRef.Count)
                    {
                        var RemoveChannelCount = atlasTexture.MigrationV0ObsoleteChannelsRef.Count - atlasTexture.AtlasSettings.Count;
                        var langs = atlasTexture.MigrationV0ObsoleteChannelsRef.Count - 1;
                        for (int count = 0; RemoveChannelCount > count; count += 1)
                        {
                            var RemoveTarget = atlasTexture.MigrationV0ObsoleteChannelsRef[langs - count];
                            UnityEngine.Object.DestroyImmediate(RemoveTarget);
                        }

                        atlasTexture.MigrationV0ObsoleteChannelsRef.RemoveAll(I => I == null);
                    }
                }
                else
                {

                    var texTransParentGroup = GameObject.AddComponent<TexTransGroup>();
                    atlasTexture.MigrationV0ObsoleteChannelsRef = new List<AtlasTexture>() { };

                    for (int Count = 0; atlasTexture.AtlasSettings.Count > Count; Count += 1)
                    {
                        CreateChannel(Count);
                    }

                }
            }

            void CreateChannel(int Count)
            {
                var channelTransform = GameObject.transform.Find("Channel " + Count);
                GameObject channelGameObject;
                if (channelTransform != null)
                {
                    channelGameObject = channelTransform.gameObject;
                }
                else
                {
                    var newGameObject = new GameObject("Channel " + Count);
                    newGameObject.transform.parent = GameObject.transform;
                    channelGameObject = newGameObject;
                }

                var newAtlasTexture = channelGameObject.AddComponent<net.rs64.TexTransTool.TextureAtlas.AtlasTexture>();
                MigrateSettingV0ToV1(atlasTexture, Count, newAtlasTexture);
                atlasTexture.MigrationV0ObsoleteChannelsRef.Add(newAtlasTexture);
                EditorUtility.SetDirty(newAtlasTexture);
            }
        }
        public static void FinalizeMigrationAtlasTextureV0ToV1(AtlasTexture atlasTexture)
        {
            if (atlasTexture.SaveDataVersion == 0)
            {
                UnityEngine.Object.DestroyImmediate(atlasTexture);
            }
        }
        private static void MigrateSettingV0ToV1(AtlasTexture atlasTextureSouse, int atlasSettingIndex, AtlasTexture NewAtlasTextureTarget)
        {
            NewAtlasTextureTarget.TargetRoot = atlasTextureSouse.TargetRoot;
            NewAtlasTextureTarget.AtlasSetting = atlasTextureSouse.AtlasSettings[atlasSettingIndex];
            NewAtlasTextureTarget.AtlasSetting.UseIslandCache = atlasTextureSouse.UseIslandCache;
            NewAtlasTextureTarget.SelectMatList = atlasTextureSouse.MatSelectors
            .Where(I => I.IsTarget && I.AtlasChannel == atlasSettingIndex)
            .Select(I => new TexTransTool.TextureAtlas.AtlasTexture.MatSelector()
            {
                Material = I.Material,
                TextureSizeOffSet = I.TextureSizeOffSet
            }).ToList();
            EditorUtility.SetDirty(NewAtlasTextureTarget);
            if (atlasTextureSouse == NewAtlasTextureTarget)
            {
                MigrationUtility.SetSaveDataVersion(NewAtlasTextureTarget, 1);
            }
        }
    }
}
#endif