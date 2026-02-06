using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Frost9.VFX
{
    /// <summary>
    /// Pool manager responsible for creating, tracking, and recycling VFX instances.
    /// </summary>
    public class VfxPoolManager : MonoBehaviour
    {
        private sealed class VfxPoolBucket
        {
            public VfxId Id;
            public VfxCatalogEntry Entry;
            public int MaxInstances;
            public ObjectPool<PooledVfxInstance> Pool;
            public readonly List<PooledVfxInstance> ActiveInstances = new List<PooledVfxInstance>();
            public int RecycleCount;
        }

        private sealed class PooledVfxInstance
        {
            public int SlotIndex;
            public uint Generation;
            public GameObject GameObject;
            public IVfxPlayable Playable;
            public VfxPoolBucket Bucket;
            public VfxId Id;
            public VfxChannel Channel;
            public GameObject Owner;
            public bool AutoRelease;
            public bool IsActive;
            public bool IsReleasing;
            public bool HasAutoReleaseDeadline;
            public float AutoReleaseAtTime;
            public AttachMode AttachMode;
            public Transform AttachTarget;
            public bool IgnoreTargetScale;
            public Vector3 AttachedBaseLocalScale;
        }

        private readonly Dictionary<VfxId, VfxPoolBucket> buckets = new Dictionary<VfxId, VfxPoolBucket>();
        private readonly Dictionary<int, PooledVfxInstance> instancesBySlot = new Dictionary<int, PooledVfxInstance>();
        private readonly Dictionary<IVfxPlayable, PooledVfxInstance> instancesByPlayable = new Dictionary<IVfxPlayable, PooledVfxInstance>();
        private readonly List<PooledVfxInstance> pendingTimedRelease = new List<PooledVfxInstance>();
        private readonly List<PooledVfxInstance> pendingDestroyedCleanup = new List<PooledVfxInstance>();
        private readonly List<PooledVfxInstance> pendingTargetLostRelease = new List<PooledVfxInstance>();

        private VfxCatalog catalog;
        private VfxSystemConfiguration configuration;
        private Transform poolRoot;
        private int nextSlotIndex = 1;
        private int activeCount;
        private bool initialized;

        /// <summary>
        /// Initializes the manager with catalog and configuration.
        /// </summary>
        /// <param name="catalog">Catalog source.</param>
        /// <param name="configuration">Runtime configuration.</param>
        public void Initialize(VfxCatalog catalog, VfxSystemConfiguration configuration)
        {
            if (initialized)
            {
                return;
            }

            this.catalog = catalog;
            this.configuration = configuration ?? VfxSystemConfiguration.GetOrCreateDefault();
            poolRoot = EnsurePoolRoot(this.configuration);
            initialized = true;
        }

        /// <summary>
        /// Attempts to play an effect instance.
        /// </summary>
        /// <param name="args">Spawn arguments.</param>
        /// <param name="handle">Resulting handle.</param>
        /// <returns>True when spawn succeeded.</returns>
        public bool TryPlay(in VfxSpawnArgs args, out VfxHandle handle)
        {
            handle = VfxHandle.Invalid;
            if (!initialized)
            {
                WarnOnceLogger.Log("vfx_not_initialized", "[VfxPoolManager] Pool manager not initialized.");
                return false;
            }

            if (catalog == null)
            {
                WarnOnceLogger.Log("vfx_missing_catalog", "[VfxPoolManager] No VFX catalog is assigned.");
                return false;
            }

            if (!catalog.TryGetEntry(args.Id, out var entry))
            {
                WarnOnceLogger.Log($"vfx_missing_id_{args.Id}", $"[VfxPoolManager] Missing VFX id in catalog: {args.Id}");
                return false;
            }

            if (entry.Prefab == null)
            {
                WarnOnceLogger.Log($"vfx_missing_prefab_{args.Id}", $"[VfxPoolManager] Missing prefab for VFX id: {args.Id}");
                return false;
            }

            if (args.AttachMode != AttachMode.WorldLocked && args.Parent == null)
            {
                WarnOnceLogger.Log(
                    $"vfx_missing_target_{args.Id}",
                    $"[VfxPoolManager] Attach mode '{args.AttachMode}' requires a valid target transform for {args.Id}.");
                return false;
            }

            if (activeCount >= configuration.MaxActiveInstances)
            {
                WarnOnceLogger.Log(
                    "vfx_global_active_cap_reached",
                    $"[VfxPoolManager] Global active VFX limit reached ({configuration.MaxActiveInstances}).");
                return false;
            }

            var bucket = GetOrCreateBucket(args.Id, entry);
            if (!entry.AllowPoolExpansion && bucket.ActiveInstances.Count >= bucket.MaxInstances)
            {
                WarnOnceLogger.Log(
                    $"vfx_bucket_cap_{args.Id}",
                    $"[VfxPoolManager] Pool limit reached for {args.Id} ({bucket.MaxInstances}).");
                return false;
            }

            var instance = bucket.Pool.Get();
            if (instance == null)
            {
                WarnOnceLogger.Log($"vfx_instance_null_{args.Id}", $"[VfxPoolManager] Could not obtain pooled instance for {args.Id}");
                return false;
            }

            instance.Generation++;
            if (instance.Generation == 0)
            {
                instance.Generation = 1;
            }

            instance.Id = args.Id;
            instance.Channel = args.Options.Channel;
            instance.Owner = args.Options.Owner;
            instance.AutoRelease = args.Options.HasAutoReleaseOverride ? args.Options.AutoRelease : entry.AutoReleaseByDefault;
            instance.IsActive = true;
            instance.IsReleasing = false;
            instance.HasAutoReleaseDeadline = false;
            instance.AutoReleaseAtTime = 0f;
            instance.AttachMode = NormalizeAttachMode(args.AttachMode);
            instance.AttachTarget = instance.AttachMode == AttachMode.WorldLocked ? null : args.Parent;
            instance.IgnoreTargetScale = args.Options.IgnoreTargetScale;
            instance.AttachedBaseLocalScale = Vector3.one;

            var autoReleaseLifetime = ResolveAutoReleaseLifetime(args);
            if (instance.AutoRelease && autoReleaseLifetime > 0f)
            {
                instance.HasAutoReleaseDeadline = true;
                instance.AutoReleaseAtTime = Time.time + autoReleaseLifetime;
            }

            bucket.ActiveInstances.Add(instance);
            activeCount++;

            instance.GameObject.SetActive(true);
            instance.Playable.Reset(args);
            instance.AttachedBaseLocalScale = instance.GameObject.transform.localScale;

            if (!TryApplyAttachment(instance))
            {
                ReleaseInstance(instance, VfxStopMode.StopEmittingAndClear, callStop: true);
                return false;
            }

            instance.Playable.Play();

            handle = new VfxHandle(instance.SlotIndex, instance.Generation);
            return true;
        }

        /// <summary>
        /// Stops a specific active instance by handle.
        /// </summary>
        /// <param name="handle">Handle to stop.</param>
        /// <returns>True when stopped.</returns>
        public bool Stop(VfxHandle handle)
        {
            if (!TryGetActiveInstance(handle, out var instance))
            {
                return false;
            }

            ReleaseInstance(instance, VfxStopMode.StopEmittingAndClear, callStop: true);
            return true;
        }

        /// <summary>
        /// Stops all active instances matching a filter.
        /// </summary>
        /// <param name="filter">Stop filter.</param>
        /// <returns>Number of stopped instances.</returns>
        public int StopAll(in VfxStopFilter filter)
        {
            if (!initialized)
            {
                return 0;
            }

            var toStop = new List<PooledVfxInstance>();
            foreach (var pair in instancesBySlot)
            {
                var instance = pair.Value;
                if (!instance.IsActive)
                {
                    continue;
                }

                if (!filter.Matches(instance.Id, instance.Channel, instance.Owner))
                {
                    continue;
                }

                toStop.Add(instance);
            }

            for (var i = 0; i < toStop.Count; i++)
            {
                ReleaseInstance(toStop[i], VfxStopMode.StopEmittingAndClear, callStop: true);
            }

            return toStop.Count;
        }

        /// <summary>
        /// Attempts to update an active instance by handle.
        /// </summary>
        /// <param name="handle">Target handle.</param>
        /// <param name="parameters">New parameters.</param>
        /// <returns>True when updated.</returns>
        public bool TryUpdate(VfxHandle handle, in VfxParams parameters)
        {
            if (!TryGetActiveInstance(handle, out var instance))
            {
                return false;
            }

            instance.Playable.Apply(parameters);
            instance.AttachedBaseLocalScale = instance.GameObject.transform.localScale;
            if (!TryApplyAttachment(instance))
            {
                ReleaseInstance(instance, VfxStopMode.StopEmittingAndClear, callStop: true);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets current statistics snapshot.
        /// </summary>
        /// <returns>Pool snapshot.</returns>
        public VfxStatsSnapshot GetStats()
        {
            var byId = new VfxIdStats[buckets.Count];
            var i = 0;
            var pooledInstances = 0;
            var totalCreated = 0;
            var totalRecycled = 0;

            foreach (var pair in buckets)
            {
                var bucket = pair.Value;
                var inactiveCount = bucket.Pool.CountInactive;
                var createdCount = bucket.Pool.CountAll;
                pooledInstances += inactiveCount;
                totalCreated += createdCount;
                totalRecycled += bucket.RecycleCount;

                byId[i++] = new VfxIdStats(
                    bucket.Id,
                    bucket.ActiveInstances.Count,
                    inactiveCount,
                    createdCount,
                    bucket.RecycleCount);
            }

            return new VfxStatsSnapshot(activeCount, pooledInstances, totalCreated, totalRecycled, byId);
        }

        /// <summary>
        /// Clears all active and inactive pooled instances.
        /// </summary>
        public void ClearAll()
        {
            var allActive = new List<PooledVfxInstance>();
            foreach (var pair in instancesBySlot)
            {
                if (pair.Value.IsActive)
                {
                    allActive.Add(pair.Value);
                }
            }

            for (var i = 0; i < allActive.Count; i++)
            {
                ReleaseInstance(allActive[i], VfxStopMode.StopEmittingAndClear, callStop: true);
            }

            foreach (var pair in buckets)
            {
                pair.Value.Pool.Clear();
                pair.Value.ActiveInstances.Clear();
            }

            buckets.Clear();
            instancesByPlayable.Clear();
            instancesBySlot.Clear();
            nextSlotIndex = 1;
            activeCount = 0;
        }

        private bool TryGetActiveInstance(VfxHandle handle, out PooledVfxInstance instance)
        {
            instance = null;
            if (!handle.IsValid)
            {
                return false;
            }

            if (!instancesBySlot.TryGetValue(handle.SlotIndex, out instance))
            {
                return false;
            }

            if (!instance.IsActive || instance.Generation != handle.Generation)
            {
                instance = null;
                return false;
            }

            return true;
        }

        private VfxPoolBucket GetOrCreateBucket(VfxId id, VfxCatalogEntry entry)
        {
            if (buckets.TryGetValue(id, out var existing))
            {
                return existing;
            }

            var initialSize = Mathf.Max(0, entry.InitialPoolSize > 0 ? entry.InitialPoolSize : configuration.DefaultInitialPoolSize);
            var maxSize = Mathf.Max(initialSize > 0 ? initialSize : 1, entry.MaxPoolSize > 0 ? entry.MaxPoolSize : configuration.DefaultMaxPoolSize);

            var bucket = new VfxPoolBucket
            {
                Id = id,
                Entry = entry,
                MaxInstances = maxSize
            };

            bucket.Pool = new ObjectPool<PooledVfxInstance>(
                () => CreateInstance(bucket),
                OnTakeFromPool,
                OnReturnedToPool,
                OnDestroyPoolObject,
                collectionCheck: true,
                defaultCapacity: initialSize,
                maxSize: maxSize);

            buckets.Add(id, bucket);
            return bucket;
        }

        private PooledVfxInstance CreateInstance(VfxPoolBucket bucket)
        {
            var instanceObject = Instantiate(bucket.Entry.Prefab, poolRoot);
            instanceObject.SetActive(false);

            var playable = instanceObject.GetComponent<IVfxPlayable>();
            if (playable == null)
            {
                playable = instanceObject.GetComponentInChildren<IVfxPlayable>(true);
            }

            if (playable == null)
            {
                playable = instanceObject.AddComponent<PrefabVfxPlayable>();
                WarnOnceLogger.Log(
                    $"vfx_missing_playable_{bucket.Id}",
                    $"[VfxPoolManager] Added {nameof(PrefabVfxPlayable)} to '{bucket.Entry.Prefab.name}' because no IVfxPlayable component was found.");
            }

            var instance = new PooledVfxInstance
            {
                SlotIndex = nextSlotIndex++,
                GameObject = instanceObject,
                Playable = playable,
                Bucket = bucket
            };

            playable.Completed += OnPlayableCompleted;
            instancesBySlot[instance.SlotIndex] = instance;
            instancesByPlayable[playable] = instance;
            return instance;
        }

        private void OnTakeFromPool(PooledVfxInstance instance)
        {
            instance.GameObject.SetActive(true);
        }

        private void OnReturnedToPool(PooledVfxInstance instance)
        {
            instance.GameObject.transform.SetParent(poolRoot, false);
            instance.GameObject.SetActive(false);
            instance.Owner = null;
            instance.Channel = VfxChannel.Gameplay;
            instance.AutoRelease = true;
            instance.IsActive = false;
            instance.HasAutoReleaseDeadline = false;
            instance.AutoReleaseAtTime = 0f;
            instance.AttachMode = AttachMode.WorldLocked;
            instance.AttachTarget = null;
            instance.IgnoreTargetScale = false;
            instance.AttachedBaseLocalScale = Vector3.one;
        }

        private void OnDestroyPoolObject(PooledVfxInstance instance)
        {
            if (instance == null)
            {
                return;
            }

            if (instance.Playable != null)
            {
                instance.Playable.Completed -= OnPlayableCompleted;
                instancesByPlayable.Remove(instance.Playable);
            }

            instancesBySlot.Remove(instance.SlotIndex);

            if (instance.GameObject != null)
            {
                Destroy(instance.GameObject);
            }
        }

        private void OnPlayableCompleted(IVfxPlayable playable)
        {
            if (!instancesByPlayable.TryGetValue(playable, out var instance))
            {
                return;
            }

            if (!instance.IsActive || instance.IsReleasing || !instance.AutoRelease)
            {
                return;
            }

            ReleaseInstance(instance, VfxStopMode.StopEmittingAndClear, callStop: false);
        }

        private void ReleaseInstance(PooledVfxInstance instance, VfxStopMode stopMode, bool callStop)
        {
            if (instance == null || !instance.IsActive || instance.IsReleasing)
            {
                return;
            }

            instance.IsReleasing = true;
            try
            {
                if (callStop)
                {
                    instance.Playable.Stop(stopMode);
                }

                instance.IsActive = false;
                instance.Bucket.ActiveInstances.Remove(instance);
                instance.Bucket.RecycleCount++;
                activeCount = Mathf.Max(0, activeCount - 1);
                instance.Bucket.Pool.Release(instance);
            }
            finally
            {
                instance.IsReleasing = false;
            }
        }

        private Transform EnsurePoolRoot(VfxSystemConfiguration config)
        {
            var rootName = string.IsNullOrWhiteSpace(config.PoolRootName) ? "VFXSystem_PoolRoot" : config.PoolRootName;
            var root = transform.Find(rootName);
            if (root == null)
            {
                var rootObject = new GameObject(rootName);
                rootObject.transform.SetParent(transform, false);
                root = rootObject.transform;
                if (config.DontDestroyPoolRootOnLoad)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }

            return root;
        }

        private void OnDestroy()
        {
            ClearAll();
        }

        private void Update()
        {
            if (!initialized || instancesBySlot.Count == 0)
            {
                return;
            }

            pendingTimedRelease.Clear();
            pendingDestroyedCleanup.Clear();
            pendingTargetLostRelease.Clear();

            foreach (var pair in instancesBySlot)
            {
                var instance = pair.Value;
                if (instance == null || !instance.IsActive)
                {
                    continue;
                }

                if (instance.GameObject == null || instance.Playable == null)
                {
                    pendingDestroyedCleanup.Add(instance);
                    continue;
                }

                if (instance.AttachMode != AttachMode.WorldLocked)
                {
                    if (!TryApplyAttachment(instance))
                    {
                        pendingTargetLostRelease.Add(instance);
                        continue;
                    }
                }

                if (instance.AutoRelease &&
                    instance.HasAutoReleaseDeadline &&
                    Time.time >= instance.AutoReleaseAtTime)
                {
                    pendingTimedRelease.Add(instance);
                }
            }

            for (var i = 0; i < pendingTimedRelease.Count; i++)
            {
                ReleaseInstance(pendingTimedRelease[i], VfxStopMode.StopEmittingAndClear, callStop: true);
            }

            for (var i = 0; i < pendingTargetLostRelease.Count; i++)
            {
                ReleaseInstance(pendingTargetLostRelease[i], VfxStopMode.StopEmittingAndClear, callStop: true);
            }

            for (var i = 0; i < pendingDestroyedCleanup.Count; i++)
            {
                CleanupDestroyedActiveInstance(pendingDestroyedCleanup[i]);
            }
        }

        private void CleanupDestroyedActiveInstance(PooledVfxInstance instance)
        {
            if (instance == null || !instance.IsActive)
            {
                return;
            }

            instance.IsActive = false;
            instance.IsReleasing = false;
            instance.HasAutoReleaseDeadline = false;
            instance.AutoReleaseAtTime = 0f;
            instance.Owner = null;
            instance.AttachMode = AttachMode.WorldLocked;
            instance.AttachTarget = null;
            instance.IgnoreTargetScale = false;
            instance.AttachedBaseLocalScale = Vector3.one;

            instance.Bucket?.ActiveInstances.Remove(instance);
            activeCount = Mathf.Max(0, activeCount - 1);

            if (instance.Playable != null)
            {
                instance.Playable.Completed -= OnPlayableCompleted;
                instancesByPlayable.Remove(instance.Playable);
            }

            instancesBySlot.Remove(instance.SlotIndex);
        }

        private static AttachMode NormalizeAttachMode(AttachMode mode)
        {
#pragma warning disable CS0618
            return mode == AttachMode.AttachToTransform ? AttachMode.FollowTransform : mode;
#pragma warning restore CS0618
        }

        private static bool TryApplyAttachment(PooledVfxInstance instance)
        {
            if (instance == null || instance.GameObject == null)
            {
                return false;
            }

            if (instance.AttachMode == AttachMode.WorldLocked)
            {
                return true;
            }

            var target = instance.AttachTarget;
            if (target == null)
            {
                return false;
            }

            var instanceTransform = instance.GameObject.transform;
            switch (instance.AttachMode)
            {
                case AttachMode.FollowTransform:
                    instanceTransform.position = target.position;
                    instanceTransform.rotation = target.rotation;
                    if (instance.IgnoreTargetScale)
                    {
                        instanceTransform.localScale = instance.AttachedBaseLocalScale;
                    }
                    else
                    {
                        instanceTransform.localScale = Vector3.Scale(instance.AttachedBaseLocalScale, target.lossyScale);
                    }

                    return true;

                case AttachMode.FollowPositionOnly:
                    instanceTransform.position = target.position;
                    return true;

                default:
                    return true;
            }
        }

        private static float ResolveAutoReleaseLifetime(in VfxSpawnArgs args)
        {
            if (args.Parameters.HasLifetimeOverride)
            {
                return Mathf.Max(0f, args.Parameters.LifetimeOverride);
            }

            return Mathf.Max(0f, args.FallbackLifetimeSeconds);
        }
    }
}
