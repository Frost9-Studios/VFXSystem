using System;
using UnityEngine;

namespace Frost9.VFX
{
    /// <summary>
    /// Scope filter used by stop-all operations.
    /// </summary>
    [Serializable]
    public struct VfxStopFilter
    {
        [SerializeField]
        private bool stopAllChannels;

        [SerializeField]
        private bool hasChannel;
        [SerializeField]
        private VfxChannel channel;

        [SerializeField]
        private bool hasId;
        [SerializeField]
        private VfxId id;

        [SerializeField]
        private bool hasOwner;
        [SerializeField]
        private GameObject owner;

        /// <summary>
        /// Gets safe default filter that targets gameplay channel.
        /// </summary>
        public static VfxStopFilter GameplayDefault => default(VfxStopFilter).WithChannel(VfxChannel.Gameplay);

        /// <summary>
        /// Gets global filter that includes all channels.
        /// </summary>
        public static VfxStopFilter Global => new VfxStopFilter { stopAllChannels = true };

        /// <summary>
        /// Gets whether channel scoping is disabled.
        /// </summary>
        public bool StopAllChannels => stopAllChannels;

        /// <summary>
        /// Gets whether a specific channel filter is set.
        /// </summary>
        public bool HasChannel => hasChannel;

        /// <summary>
        /// Gets configured channel filter.
        /// </summary>
        public VfxChannel Channel => channel;

        /// <summary>
        /// Gets whether a specific identifier filter is set.
        /// </summary>
        public bool HasId => hasId;

        /// <summary>
        /// Gets configured identifier filter.
        /// </summary>
        public VfxId Id => id;

        /// <summary>
        /// Gets whether a specific owner filter is set.
        /// </summary>
        public bool HasOwner => hasOwner;

        /// <summary>
        /// Gets configured owner filter.
        /// </summary>
        public GameObject Owner => owner;

        /// <summary>
        /// Returns a copy scoped to a specific channel.
        /// </summary>
        /// <param name="value">Target channel.</param>
        /// <returns>Updated filter.</returns>
        public VfxStopFilter WithChannel(VfxChannel value)
        {
            stopAllChannels = false;
            hasChannel = true;
            channel = value;
            return this;
        }

        /// <summary>
        /// Returns a copy scoped to a specific identifier.
        /// </summary>
        /// <param name="value">Target identifier.</param>
        /// <returns>Updated filter.</returns>
        public VfxStopFilter WithId(VfxId value)
        {
            hasId = true;
            id = value;
            return this;
        }

        /// <summary>
        /// Returns a copy scoped to a specific owner.
        /// </summary>
        /// <param name="value">Target owner object.</param>
        /// <returns>Updated filter.</returns>
        public VfxStopFilter WithOwner(GameObject value)
        {
            hasOwner = true;
            owner = value;
            return this;
        }

        /// <summary>
        /// Evaluates whether a runtime instance matches this filter.
        /// </summary>
        /// <param name="candidateId">Instance identifier.</param>
        /// <param name="candidateChannel">Instance channel.</param>
        /// <param name="candidateOwner">Instance owner.</param>
        /// <returns>True when instance is within scope.</returns>
        public bool Matches(VfxId candidateId, VfxChannel candidateChannel, GameObject candidateOwner)
        {
            if (HasId && candidateId != id)
            {
                return false;
            }

            if (HasOwner && candidateOwner != owner)
            {
                return false;
            }

            if (!stopAllChannels)
            {
                var channelToMatch = hasChannel ? channel : VfxChannel.Gameplay;
                if (candidateChannel != channelToMatch)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
