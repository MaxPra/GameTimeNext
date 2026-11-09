using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.LauncherIntegration;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.DataManagers
{
    /// <summary>
    /// Launcher games known to the automatic import (T1LNGAM). Every game is registered once, with status
    /// (codetable lG) PEND = still to import, IMPO = imported or already had a profile, SKIP = left out by the user.
    /// A registered game is never imported again, so a deleted profile doesn't come back.
    /// </summary>
    public class TFLNGAM
    {
        /// <summary>
        /// Returns the status (LGST) of every registered game by its key (LNTY, LNID). Only these two columns are
        /// read, so it is cheap enough to be called regularly (LauncherAutoImportProcess).
        /// </summary>
        public static Dictionary<(string, string), string> GetRegisteredStatuses()
        {
            Dictionary<(string, string), string> statuses = new Dictionary<(string, string), string>();

            UIXQuery query = new UIXQuery(K1LNGAM.Name, AppEnvironment.GetDataBaseManager().GetConnection());
            query.AddField(K1LNGAM.Name, K1LNGAM.Fields.LNTY);
            query.AddField(K1LNGAM.Name, K1LNGAM.Fields.LNID);
            query.AddField(K1LNGAM.Name, K1LNGAM.Fields.LGST);

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    statuses.TryAdd((UIXQuery.GetString(reader, K1LNGAM.Name, K1LNGAM.Fields.LNTY, def: ""),
                                     UIXQuery.GetString(reader, K1LNGAM.Name, K1LNGAM.Fields.LNID, def: "")),
                                    UIXQuery.GetString(reader, K1LNGAM.Name, K1LNGAM.Fields.LGST, def: ""));
                }
            }

            return statuses;
        }

        /// <summary>
        /// Returns the games that are neither registered nor have a profile: the ones an import would add.
        /// </summary>
        public static List<LauncherGame> GetUnregisteredGames(List<LauncherGame> games)
        {
            HashSet<(string, string)> registered = ReadAllByKey().Keys.ToHashSet();
            ProfileLookup profiles = new ProfileLookup(new TXPROFI().ReadAll());

            return DistinctByKey(games)
                .Where(g => !registered.Contains(GetKey(g)) && profiles.Find(g) == 0)
                .ToList();
        }

        /// <summary>
        /// Registers every game that isn't registered yet: a game that already has a profile (linked, or an unlinked
        /// profile with the same name) as imported with that profile, all others with the given status.
        /// </summary>
        public static void RegisterGames(List<LauncherGame> games, string status)
        {
            Dictionary<(string, string), T1LNGAM> registered = ReadAllByKey();
            ProfileLookup profiles = new ProfileLookup(new TXPROFI().ReadAll());
            TXLNGAM txlngam = new TXLNGAM();

            foreach (LauncherGame game in DistinctByKey(games))
            {
                if (registered.ContainsKey(GetKey(game)))
                    continue;

                long pfid = profiles.Find(game);

                T1LNGAM t1lngam = txlngam.CreateNew();
                t1lngam.LNTY = game.LauncherType;
                t1lngam.LNID = game.GameId;
                t1lngam.GANA = game.Name;
                t1lngam.LGST = pfid != 0 ? "IMPO" : status;
                t1lngam.PFID = pfid;

                txlngam.Save(t1lngam);
            }
        }

        /// <summary>
        /// Returns the games still to import (PEND) among the given installed games, oldest registration first.
        /// </summary>
        public static List<(T1LNGAM t1lngam, LauncherGame game)> GetPendingGames(List<LauncherGame> games)
        {
            Dictionary<(string, string), LauncherGame> installed = DistinctByKey(games).ToDictionary(GetKey);

            return new TXLNGAM().ReadAll()
                .Where(t => t.LGST == "PEND" && installed.ContainsKey((t.LNTY, t.LNID)))
                .Select(t => (t, installed[(t.LNTY, t.LNID)]))
                .ToList();
        }

        /// <summary>
        /// Marks the game as imported with the given profile.
        /// </summary>
        public static void SetImported(T1LNGAM t1lngam, long pfid)
        {
            t1lngam.LGST = "IMPO";
            t1lngam.PFID = pfid;

            new TXLNGAM().Save(t1lngam);
        }

        private static Dictionary<(string, string), T1LNGAM> ReadAllByKey()
        {
            Dictionary<(string, string), T1LNGAM> result = new Dictionary<(string, string), T1LNGAM>();

            // LNTY + LNID is unique by this class; TryAdd only guards against hand-edited data
            foreach (T1LNGAM t1lngam in new TXLNGAM().ReadAll())
                result.TryAdd((t1lngam.LNTY, t1lngam.LNID), t1lngam);

            return result;
        }

        private static IEnumerable<LauncherGame> DistinctByKey(IEnumerable<LauncherGame> games)
        {
            return games.DistinctBy(GetKey);
        }

        private static (string, string) GetKey(LauncherGame game)
        {
            return (game.LauncherType, game.GameId);
        }

        /// <summary>
        /// Finds the profile of a launcher game: linked with it, or not linked at all and with the same name
        /// (a profile created by hand before it could be linked; it is left as it is, not linked automatically).
        /// </summary>
        private sealed class ProfileLookup
        {
            private readonly Dictionary<(string, string), long> _linked = new Dictionary<(string, string), long>();
            private readonly Dictionary<string, long> _unlinkedByName = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

            public ProfileLookup(List<T1PROFI> t1profis)
            {
                foreach (T1PROFI t1profi in t1profis)
                {
                    if (FnLauncher.IsLinked(t1profi))
                        _linked.TryAdd((t1profi.LNTY, t1profi.LNID), t1profi.PFID);
                    else
                        _unlinkedByName.TryAdd(t1profi.GANA.Trim(), t1profi.PFID);
                }
            }

            /// <summary>
            /// PFID of the game's profile, 0 if it has none.
            /// </summary>
            public long Find(LauncherGame game)
            {
                if (_linked.TryGetValue(GetKey(game), out long pfid))
                    return pfid;

                return _unlinkedByName.TryGetValue(game.Name.Trim(), out pfid) ? pfid : 0;
            }
        }
    }
}
