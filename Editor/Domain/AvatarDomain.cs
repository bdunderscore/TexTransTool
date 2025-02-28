using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using net.rs64.TexTransTool.TextureStack;
using net.rs64.TexTransCore.Island;

namespace net.rs64.TexTransTool
{
    /// <summary>
    /// This is an IDomain implementation that applies to whole the specified GameObject.
    ///
    /// If <see cref="Previewing"/> is true, This will call <see cref="AnimationMode.AddPropertyModification"/>
    /// everytime modifies some property so you can revert those changes with <see cref="AnimationMode.StopAnimationMode"/>.
    /// This class doesn't call <see cref="AnimationMode.BeginSampling"/> and <see cref="AnimationMode.EndSampling"/>
    /// so user must call those if needed.
    /// </summary>
    internal class AvatarDomain : RenderersDomain
    {
        static readonly HashSet<Type> s_ignoreTypes = new HashSet<Type> { typeof(Transform), typeof(SkinnedMeshRenderer), typeof(MeshRenderer) };

        public AvatarDomain(GameObject avatarRoot, bool previewing, bool saveAsset = false, bool progressDisplay = false)
        : this(avatarRoot, previewing, saveAsset ? new AssetSaver() : null, progressDisplay) { }
        public AvatarDomain(GameObject avatarRoot, bool previewing, IAssetSaver assetSaver, bool progressDisplay = false)
        : base(avatarRoot.GetComponentsInChildren<Renderer>(true).ToList(), previewing, assetSaver, progressDisplay)
        {
            _avatarRoot = avatarRoot;
            _previewing = previewing;
            _isObjectReplaceInvoke = TTTConfig.IsObjectReplaceInvoke;
        }
        public AvatarDomain(GameObject avatarRoot, bool previewing,
                            IAssetSaver saver,
                            IProgressHandling progressHandler,
                            ITextureManager textureManager,
                            IStackManager stackManager,
                            IIslandCache islandCache,
                            bool? isObjectReplaceInvoke = null
                            ) : base(avatarRoot.GetComponentsInChildren<Renderer>(true).ToList(), previewing, saver, progressHandler, textureManager, stackManager, islandCache)
        {
            _avatarRoot = avatarRoot;
            _previewing = previewing;
            _isObjectReplaceInvoke = isObjectReplaceInvoke ?? TTTConfig.IsObjectReplaceInvoke;
        }

        bool _previewing;

        [SerializeField] GameObject _avatarRoot;
        public GameObject AvatarRoot => _avatarRoot;
        bool _isObjectReplaceInvoke;

        public override void EditFinish()
        {
            base.EditFinish();

            if (_previewing) { return; }

            var modMap = _objectMap.GetMapping;

#if NDMF_1_3_x
            foreach (var replaceKV in modMap)
            {
                nadena.dev.ndmf.ObjectRegistry.RegisterReplacedObject(replaceKV.Key, replaceKV.Value);
            }
#endif
            if (_isObjectReplaceInvoke)
            {
                SerializedObjectCrawler.ReplaceSerializedObjects(_avatarRoot, modMap);
            }
        }
    }

    internal class FlatMapDict<TKeyValue>
    {
        Dictionary<TKeyValue, TKeyValue> _dict = new();
        Dictionary<TKeyValue, TKeyValue> _reverseDict = new();
        HashSet<TKeyValue> _invalidObjects = new();

        public void Add(TKeyValue old, TKeyValue now)
        {
            if (_invalidObjects.Contains(old)) { return; }

            if (_reverseDict.TryGetValue(old, out var tKey))
            {
                //Mapping Update
                _dict[tKey] = now;
                _reverseDict.Remove(old);
                _reverseDict.Add(now, tKey);
            }
            else if (!_dict.ContainsKey(old))
            {
                //Mapping Add
                _dict.Add(old, now);
                _reverseDict.Add(now, old);
            }
            else
            {
                //InvalidMapping
                _invalidObjects.Add(old);

                _reverseDict.Remove(_dict[old]);
                _dict.Remove(old);
            }
        }
        public Dictionary<TKeyValue, TKeyValue> GetMapping => _dict;
    }

}
