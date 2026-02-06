using System;

namespace Frost9.VFX
{
    /// <summary>
    /// Immutable snapshot of runtime VFX pool statistics.
    /// </summary>
    [Serializable]
    public readonly struct VfxStatsSnapshot
    {
        /// <summary>
        /// Initializes a new stats snapshot.
        /// </summary>
        /// <param name="totalActiveInstances">Total active instances.</param>
        /// <param name="totalPooledInstances">Total inactive pooled instances.</param>
        /// <param name="totalCreatedInstances">Total created instances.</param>
        /// <param name="totalRecycleCount">Total recycle count.</param>
        /// <param name="byId">Per-id statistics array.</param>
        public VfxStatsSnapshot(
            int totalActiveInstances,
            int totalPooledInstances,
            int totalCreatedInstances,
            int totalRecycleCount,
            VfxIdStats[] byId)
        {
            TotalActiveInstances = totalActiveInstances;
            TotalPooledInstances = totalPooledInstances;
            TotalCreatedInstances = totalCreatedInstances;
            TotalRecycleCount = totalRecycleCount;
            ById = byId ?? Array.Empty<VfxIdStats>();
        }

        /// <summary>
        /// Gets total active instances.
        /// </summary>
        public int TotalActiveInstances { get; }

        /// <summary>
        /// Gets total pooled instances.
        /// </summary>
        public int TotalPooledInstances { get; }

        /// <summary>
        /// Gets total created instances.
        /// </summary>
        public int TotalCreatedInstances { get; }

        /// <summary>
        /// Gets total instance recycle count.
        /// </summary>
        public int TotalRecycleCount { get; }

        /// <summary>
        /// Gets per-id statistics.
        /// </summary>
        public VfxIdStats[] ById { get; }
    }

    /// <summary>
    /// Per-identifier statistics data.
    /// </summary>
    [Serializable]
    public readonly struct VfxIdStats
    {
        /// <summary>
        /// Initializes per-id stats.
        /// </summary>
        /// <param name="id">Catalog identifier.</param>
        /// <param name="activeInstances">Active count.</param>
        /// <param name="pooledInstances">Inactive pool count.</param>
        /// <param name="createdInstances">Created count.</param>
        /// <param name="recycleCount">Recycle count.</param>
        public VfxIdStats(
            VfxId id,
            int activeInstances,
            int pooledInstances,
            int createdInstances,
            int recycleCount)
        {
            Id = id;
            ActiveInstances = activeInstances;
            PooledInstances = pooledInstances;
            CreatedInstances = createdInstances;
            RecycleCount = recycleCount;
        }

        /// <summary>
        /// Gets catalog identifier.
        /// </summary>
        public VfxId Id { get; }

        /// <summary>
        /// Gets active instance count.
        /// </summary>
        public int ActiveInstances { get; }

        /// <summary>
        /// Gets pooled instance count.
        /// </summary>
        public int PooledInstances { get; }

        /// <summary>
        /// Gets created instance count.
        /// </summary>
        public int CreatedInstances { get; }

        /// <summary>
        /// Gets recycle count.
        /// </summary>
        public int RecycleCount { get; }
    }
}
