#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Legato.Beatmaps {
    internal class BeatmapMaxScoreCache {
        private readonly Dictionary<BeatmapKey, int> _cache = new Dictionary<BeatmapKey, int>();
        private readonly BeatmapLevelLoader _beatmapLevelLoader;
        private readonly BeatmapDataLoader _beatmapDataLoader;
        private readonly BeatmapLevelsEntitlementModel _beatmapLevelsEntitlementModel;

        public BeatmapMaxScoreCache(BeatmapLevelLoader beatmapLevelLoader, BeatmapDataLoader beatmapDataLoader, BeatmapLevelsEntitlementModel beatmapLevelsEntitlementModel) {
            _beatmapLevelLoader = beatmapLevelLoader;
            _beatmapDataLoader = beatmapDataLoader;
            _beatmapLevelsEntitlementModel = beatmapLevelsEntitlementModel;
        }

        public async Task<int> GetMaxScore(BeatmapLevel beatmapLevel, BeatmapKey beatmapKey) {
            if (_cache.TryGetValue(beatmapKey, out int cachedScore)) {
                return cachedScore;
            }

            var beatmapLevelDataVersion = await _beatmapLevelsEntitlementModel.GetLevelDataVersionAsync(beatmapKey.levelId, CancellationToken.None);
            var beatmapLevelData = (await _beatmapLevelLoader.LoadBeatmapLevelDataAsync(beatmapLevel, beatmapLevelDataVersion, CancellationToken.None)).beatmapLevelData;
            if (beatmapLevelData == null) {
                throw new InvalidOperationException($"Beatmap data is unavailable for {beatmapKey.levelId}");
            }

            var beatmapData = await _beatmapDataLoader.LoadBeatmapDataAsync(
                beatmapLevelData: beatmapLevelData,
                beatmapKey: beatmapKey,
                startBpm: beatmapLevel.beatsPerMinute,
                loadingForDesignatedEnvironment: false,
                originalEnvironmentInfo: null,
                targetEnvironmentInfo: null,
                beatmapLevelDataVersion: beatmapLevelDataVersion,
                gameplayModifiers: null,
                playerSpecificSettings: null);
            if (beatmapData == null) {
                throw new InvalidOperationException($"Beatmap could not be loaded for {beatmapKey.levelId}");
            }

            int maxScore = ScoreModel.ComputeMaxMultipliedScoreForBeatmap(beatmapData);
            _cache[beatmapKey] = maxScore;
            return maxScore;
        }
    }
}
