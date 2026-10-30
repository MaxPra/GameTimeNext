using GameTimeNext.Core.Application.Profiles.Types;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Utils;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Utils;
using static GameTimeNext.Core.Application.Profiles.Controller.ProfilesViewController;

namespace GameTimeNext.Core.Application.DataManagers
{
    /// <summary>
    /// Library filter (profiles view and Big Picture mode).
    /// </summary>
    public partial class TFPROFI
    {
        /// <summary>
        /// Reads all profiles matching the library filter and the name search (empty for none), last played first.
        /// Tags and fixed options are OR-combined within their group and AND-combined between groups.
        /// FilterActive tells whether the filter hides anything (the name search doesn't count).
        /// </summary>
        public static (List<T1PROFI> Profiles, bool FilterActive) GetFilteredProfiles(FilterCache filter, string nameSearch)
        {
            List<T1PROFI> t1profisFiltered = new List<T1PROFI>();

            // Only consider tags that still exist (deleted tags may still be stored in the filter)
            HashSet<long> existingTagIds = new TXGROUP().ReadAll()
                .Where(g => g.GTYP == GroupType.Tag)
                .Select(g => g.GRID)
                .ToHashSet();

            HashSet<long> excludedTagIds = filter.ExcludedTagIds.Where(existingTagIds.Contains).ToHashSet();
            HashSet<string> excludedOptions = filter.ExcludedOptions.ToHashSet();

            Dictionary<long, HashSet<long>> tagIdsByPfid = new TXGRPPO().ReadAll()
                .Where(g => existingTagIds.Contains(g.GRID))
                .GroupBy(g => g.PFID)
                .ToDictionary(g => g.Key, g => g.Select(x => x.GRID).ToHashSet());

            HashSet<long> completedPfids = TFPLTHR.GetPfidsWithCompletedPlaythrough();
            HashSet<long> currentPlaythroughPfids = TFPLTHR.GetPfidsWithCurrentPlaythrough();
            HashSet<long> pausedPlaythroughPfids = TFPLTHR.GetPfidsWithPausedPlaythrough();

            UIXQuery query = BuildFilteredProfilesQuery(filter, nameSearch);

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    TXPROFI txprofi = new TXPROFI();
                    long pfid = UIXQuery.GetInt64(reader, K1PROFI.Name, K1PROFI.Fields.PFID);
                    T1PROFI t1profi = txprofi.Read(pfid);

                    tagIdsByPfid.TryGetValue(pfid, out HashSet<long>? tagIds);

                    string progress = GetProgress(t1profi, completedPfids.Contains(pfid), currentPlaythroughPfids.Contains(pfid), pausedPlaythroughPfids.Contains(pfid));

                    if (IsProfileVisible(t1profi, tagIds, progress, filter, excludedTagIds, excludedOptions))
                        t1profisFiltered.Add(t1profi);
                }
            }

            bool filterActive = excludedTagIds.Count > 0
                || filter.ExcludeUntagged
                || excludedOptions.Count > 0
                || filter.ShowArchived
                || filter.OnlyRecentlyPlayed;

            return (t1profisFiltered, filterActive);
        }

        /// <summary>
        /// Determines the progress option of a profile (exactly one per profile)
        /// </summary>
        private static string GetProgress(T1PROFI t1profi, bool hasCompletedPlaythrough, bool hasCurrentPlaythrough, bool hasPausedPlaythrough)
        {
            // Never played (the initial playthrough is open from the start, so this has to be checked first)
            if (t1profi.FIPL == DateTime.MinValue && !hasCompletedPlaythrough)
                return ProfilesFilterOptions.PROGRESS_UNPLAYED;

            // An open playthrough wins over completed ones (e.g. a second run)
            if (hasCurrentPlaythrough)
                return ProfilesFilterOptions.PROGRESS_IN_PROGRESS;

            // A paused playthrough can be resumed, so it wins over completed ones as well
            if (hasPausedPlaythrough)
                return ProfilesFilterOptions.PROGRESS_PAUSED;

            if (hasCompletedPlaythrough)
                return ProfilesFilterOptions.PROGRESS_COMPLETED;

            // Played, but only cancelled playthroughs left
            return ProfilesFilterOptions.PROGRESS_CANCELLED;
        }

        private static bool IsProfileVisible(T1PROFI t1profi, HashSet<long>? tagIds, string progress, FilterCache filter, HashSet<long> excludedTagIds, HashSet<string> excludedOptions)
        {
            // -- Tags (visible if at least one of its tags is checked)
            if (tagIds == null || tagIds.Count == 0)
            {
                if (filter.ExcludeUntagged)
                    return false;
            }
            else if (tagIds.All(excludedTagIds.Contains))
                return false;

            // -- Progress
            if (excludedOptions.Contains(progress))
                return false;

            // -- Availability (file system check only when needed)
            if (excludedOptions.Contains(ProfilesFilterOptions.AVAILABILITY_PLAYABLE) || excludedOptions.Contains(ProfilesFilterOptions.AVAILABILITY_NOT_PLAYABLE))
            {
                string availability = FnSystem.IsExeFoundInPath(t1profi.EXGF)
                    ? ProfilesFilterOptions.AVAILABILITY_PLAYABLE
                    : ProfilesFilterOptions.AVAILABILITY_NOT_PLAYABLE;

                if (excludedOptions.Contains(availability))
                    return false;
            }

            // -- Source (codetable lookup only when needed)
            if (excludedOptions.Contains(ProfilesFilterOptions.SOURCE_LOCAL) || excludedOptions.Contains(ProfilesFilterOptions.SOURCE_EXTERNAL))
            {
                string source = IsExternalGame(t1profi)
                    ? ProfilesFilterOptions.SOURCE_EXTERNAL
                    : ProfilesFilterOptions.SOURCE_LOCAL;

                if (excludedOptions.Contains(source))
                    return false;
            }

            return true;
        }

        private static UIXQuery BuildFilteredProfilesQuery(FilterCache filter, string nameSearch)
        {
            UIXQuery query = new UIXQuery(K1PROFI.Name, AppEnvironment.GetDataBaseManager().GetConnection());
            query.SetDistinct(true);

            // Fields
            query.AddField(K1PROFI.Name, K1PROFI.Fields.PFID);

            // -- Archived games are hidden unless explicitly requested
            if (!filter.ShowArchived)
                query.AddWhere(K1PROFI.Name, K1PROFI.Fields.ARCH, QueryCompareType.EQUALS, false);

            // -- Recently played
            if (filter.OnlyRecentlyPlayed)
            {
                DateTime limitDate = DateTime.Now.AddDays(-ProfilesFilterOptions.RECENTLY_PLAYED_DAYS);
                query.AddWhere(K1PROFI.Name, K1PROFI.Fields.LAPL, QueryCompareType.GREATER_THAN, limitDate);
            }

            // -- Profile name
            if (!FnString.IsNullEmptyOrWhitespace(nameSearch))
                query.AddWhere(K1PROFI.Name, K1PROFI.Fields.GANA, QueryCompareType.LIKE, nameSearch);

            // Order by LAPL
            query.AddOrderBy(K1PROFI.Name, K1PROFI.Fields.LAPL, OrderDirection.DESC);
            return query;
        }
    }
}
