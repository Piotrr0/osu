// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Online.API;
using osu.Game.Users;

namespace osu.Game.Online.Metadata
{
    public abstract partial class MetadataClient : Component, IMetadataClient, IMetadataServer
    {
        public abstract IBindable<bool> IsConnected { get; }

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        #region Beatmap metadata updates

        public abstract Task<BeatmapUpdates> GetChangesSince(int queueId);

        public abstract Task BeatmapSetsUpdated(BeatmapUpdates updates);

        public event Action<int[]>? ChangedBeatmapSetsArrived;

        protected Task ProcessChanges(int[] beatmapSetIDs)
        {
            ChangedBeatmapSetsArrived?.Invoke(beatmapSetIDs.Distinct().ToArray());
            return Task.CompletedTask;
        }

        #endregion

        #region User presence updates

        /// <summary>
        /// Whether the client is currently receiving user presence updates from the server.
        /// </summary>
        public abstract IBindable<bool> IsWatchingUserPresence { get; }

        /// <summary>
        /// The <see cref="UserPresence"/> information about the current user.
        /// </summary>
        public abstract UserPresence LocalUserState { get; }

        /// <summary>
        /// Dictionary keyed by user ID containing all of the <see cref="UserPresence"/> information about currently online users received from the server.
        /// </summary>
        public abstract IBindableDictionary<int, UserPresence> UserStates { get; }

        /// <summary>
        /// Dictionary keyed by user ID containing all of the <see cref="UserPresence"/> information about currently online friends received from the server.
        /// </summary>
        public abstract IBindableDictionary<int, UserPresence> FriendStates { get; }

        /// <summary>
        /// Attempts to retrieve the presence of a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>The user presence, or null if not available or the user's offline.</returns>
        public UserPresence? GetPresence(int userId)
        {
            if (userId == api.LocalUser.Value.OnlineID)
                return LocalUserState;

            if (FriendStates.TryGetValue(userId, out UserPresence presence))
                return presence;

            if (UserStates.TryGetValue(userId, out presence))
                return presence;

            return null;
        }

        /// <inheritdoc/>
        public abstract Task UpdateActivity(UserActivity? activity);

        /// <inheritdoc/>
        public abstract Task UpdateStatus(UserStatus? status);

        /// <inheritdoc/>
        public abstract Task BeginWatchingUserPresence();

        /// <inheritdoc/>
        public abstract Task EndWatchingUserPresence();

        /// <inheritdoc/>
        public abstract Task UserPresenceUpdated(int userId, UserPresence? presence);

        /// <inheritdoc/>
        public abstract Task FriendPresenceUpdated(int userId, UserPresence? presence);

        #endregion

        #region Daily Challenge

        public abstract IBindable<DailyChallengeInfo?> DailyChallengeInfo { get; }

        /// <inheritdoc/>
        public abstract Task DailyChallengeUpdated(DailyChallengeInfo? info);

        #endregion

        #region Multiplayer room watching

        public abstract Task<MultiplayerPlaylistItemStats[]> BeginWatchingMultiplayerRoom(long id);

        public abstract Task EndWatchingMultiplayerRoom(long id);

        public event Action<MultiplayerRoomScoreSetEvent>? MultiplayerRoomScoreSet;

        Task IMetadataClient.MultiplayerRoomScoreSet(MultiplayerRoomScoreSetEvent roomScoreSetEvent)
        {
            if (MultiplayerRoomScoreSet != null)
                Schedule(MultiplayerRoomScoreSet, roomScoreSetEvent);

            return Task.CompletedTask;
        }

        #endregion

        #region Disconnection handling

        public event Action? Disconnecting;

        public virtual Task DisconnectRequested()
        {
            Schedule(() => Disconnecting?.Invoke());
            return Task.CompletedTask;
        }

        #endregion
    }
}
