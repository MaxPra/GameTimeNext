using GameTimeNext.Core.Application.Profiles.Components;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Config;
using System.IO;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.DataManagers
{
    public partial class TFPROFI
    {
        /// <summary>
        /// Deletes the given profile including its linked data
        /// </summary>
        public static void DeleteT1PROFIAndLinkedData(T1PROFI t1profi)
        {
            TXPROFI txprofi = new TXPROFI();

            DeleteAllLinkedT1GRPPOs(t1profi);
            DeleteAllLinkedT1SESSIs(t1profi);
            DeleteAllLinkedT1PLTHRs(t1profi);

            DeleteCoverImage(t1profi);

            // Delete profile
            txprofi.Delete(t1profi.PFID);
        }

        public static bool GetIsUnplayed(T1PROFI t1profi)
        {
            return t1profi.FIPL == DateTime.MinValue;
        }

        public static bool HasExecutables(T1PROFI t1profi)
        {
            CExecutables cExecutables = new CExecutables(t1profi.EXEC).Dezerialize();

            if (cExecutables.KeyValuePairs.Count == 0)
                return false;

            return true;
        }

        /// <summary>
        /// Tags (entries of codetable tG, hidden ones included) linked with the profile. Links to tags that no longer exist are skipped.
        /// </summary>
        public static List<T1CTABD> GetAllLinkedTags(T1PROFI t1profi)
        {
            List<T1CTABD> tags = new List<T1CTABD>();
            TXCTABD txctabd = new TXCTABD();

            foreach (string tagKey in GetLinkedTagKeys(t1profi.PFID))
            {
                T1CTABD? tag = txctabd.Read(TFCTABD.TagCodetable, tagKey);

                if (tag is not null)
                    tags.Add(tag);
            }

            return tags;
        }

        /// <summary>
        /// Keys (T1GRPPO.TGKY) of the tags linked with the profile.
        /// </summary>
        public static List<string> GetLinkedTagKeys(long pfid)
        {
            List<string> tagKeys = new List<string>();

            UIXQuery query = BuildLinkedTagsQuery(pfid);

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    string tagKey = UIXQuery.GetString(reader, K1GRPPO.Name, K1GRPPO.Fields.TGKY, def: "");

                    if (!FnString.IsNullEmptyOrWhitespace(tagKey))
                        tagKeys.Add(tagKey);
                }
            }

            return tagKeys;
        }

        public static List<T1SESSI> GetAllSessions(T1PROFI t1profi)
        {
            List<T1SESSI> t1sessis = new List<T1SESSI>();

            UIXQuery query = new UIXQuery(K1SESSI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1SESSI.Name, K1SESSI.Fields.SEID);

            query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PFID, QueryCompareType.EQUALS, t1profi.PFID);

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    long seid = UIXQuery.GetInt64(reader, K1SESSI.Name, K1SESSI.Fields.SEID);
                    t1sessis.Add(new TXSESSI().Read(seid));
                }
            }

            return t1sessis;
        }

        public static bool BlackoutOverridenAndActive(T1PROFI t1profi)
        {
            CProfileSettings cProfileSettings = new CProfileSettings(t1profi.PRSE).Dezerialize();

            return cProfileSettings.OverrideGlobalBlackout && cProfileSettings.BlackoutSideMonitors;
        }

        public static bool BlackoutOverridenAndInactive(T1PROFI t1profi)
        {
            CProfileSettings cProfileSettings = new CProfileSettings(t1profi.PRSE).Dezerialize();
            return cProfileSettings.OverrideGlobalBlackout && !cProfileSettings.BlackoutSideMonitors;
        }

        /// <summary>
        /// True if the game is still shrink-wrapped: it has to be unwrapped before it can be launched.
        /// Only while the setting SealNewGames is on; turning it off shows all games unwrapped.
        /// </summary>
        public static bool IsSealed(T1PROFI t1profi)
        {
            return AppEnvironment.GetAppConfig().AppSettings.SealNewGames
                && new CProfileSettings(t1profi.PRSE).Dezerialize().Sealed;
        }

        /// <summary>
        /// Unwraps the game and saves the profile
        /// </summary>
        public static void Unseal(T1PROFI t1profi)
        {
            CProfileSettings cProfileSettings = new CProfileSettings(t1profi.PRSE).Dezerialize();

            if (!cProfileSettings.Sealed)
                return;

            cProfileSettings.Sealed = false;

            // Unwrapping is looking at the new game
            cProfileSettings.IsNew = false;
            t1profi.PRSE = cProfileSettings.Serialize();

            new TXPROFI().Save(t1profi);
        }

        /// <summary>
        /// True if the profile was added by the automatic import and not looked at yet (badge "New").
        /// </summary>
        public static bool IsMarkedNew(T1PROFI t1profi)
        {
            return new CProfileSettings(t1profi.PRSE).Dezerialize().IsNew;
        }

        /// <summary>
        /// Removes the badge "New" when the profile is selected and saves it. A sealed game keeps it: it is removed
        /// when the game is unwrapped. Returns true if the badge was removed.
        /// </summary>
        public static bool ClearNewMarkOnSelection(T1PROFI t1profi)
        {
            CProfileSettings cProfileSettings = new CProfileSettings(t1profi.PRSE).Dezerialize();

            if (!cProfileSettings.IsNew || IsSealed(t1profi))
                return false;

            cProfileSettings.IsNew = false;
            t1profi.PRSE = cProfileSettings.Serialize();

            new TXPROFI().Save(t1profi);

            return true;
        }

        /// <summary>
        /// Saves a profile created without the edit view (automatic import) together with its initial playthrough,
        /// as saving a new profile in the edit view does.
        /// </summary>
        public static void SaveNewProfile(T1PROFI t1profi)
        {
            new TXPROFI().Save(t1profi);

            TFPLTHR.CreateNewPlaythrough(t1profi.PFID);
        }

        public static List<T1PLTHR> GetAllPlaythroughs(T1PROFI t1profi)
        {
            List<T1PLTHR> t1plthrs = new List<T1PLTHR>();

            UIXQuery query = new UIXQuery(K1PLTHR.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTID);

            query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PFID, QueryCompareType.EQUALS, t1profi.PFID);

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    long ptid = UIXQuery.GetInt64(reader, K1PLTHR.Name, K1PLTHR.Fields.PTID);
                    t1plthrs.Add(new TXPLTHR().Read(ptid));
                }
            }

            return t1plthrs;
        }

        public static double GetTodaysGameTimeInMinutes(long pfid)
        {
            return GetGameTimeInMinutes(pfid, DateTime.Today.Date, DateTime.Today.Date);
        }

        public static double GetGameTimeInMinutes(long pfid, DateTime? start, DateTime end)
        {
            UIXQuery query = TFSESSI.BuildQuerySessionsInTimeSpanBase(start, end, pfid: pfid);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PLTI);
            query.AddSum(K1SESSI.Name, K1SESSI.Fields.PLTI, "PLTI");

            using (var reader = query.Execute())
                if (reader.Read())
                    return UIXQuery.GetDouble(reader, "PLTI");

            return 0;
        }

        public static double GetTotalGameTimeInMinutes(long pfid)
        {
            UIXQuery query = new UIXQuery(K1SESSI.Name, AppEnvironment.GetDataBaseManager().GetConnection());
            query.AddSum(K1SESSI.Name, K1SESSI.Fields.PLTI, "TotalPlaytime");
            query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PFID, QueryCompareType.EQUALS, pfid);

            using (var reader = query.Execute())
                if (reader.Read())
                    return UIXQuery.GetDouble(reader, "TotalPlaytime");

            return 0;
        }

        public static string GetProfileName(long pfid)
        {
            TXPROFI txprofi = new TXPROFI();
            T1PROFI t1profi = txprofi.Read(pfid);

            if (t1profi is null)
                return string.Empty;

            return t1profi.GANA;
        }

        /// <summary>
        /// Returns whether the profile is an external game (consoles, mobile, etc.)
        /// </summary>
        /// <param name="pfid"></param>
        /// <returns></returns>
        public static bool IsExternalGame(long pfid)
        {
            TXPROFI txprofi = new TXPROFI();
            T1PROFI t1profi = txprofi.Read(pfid);

            return IsExternalGame(t1profi);
        }

        /// <summary>
        /// Same as IsExternalGame(long), for an already loaded profile
        /// </summary>
        public static bool IsExternalGame(T1PROFI t1profi)
        {
            if (t1profi is null)
                return false;

            string plafo = t1profi.PLAFO;
            T1CTABD t1ctabd = new TXCTABD().Read("pF", plafo);

            if (t1ctabd is null)
                return false;

            t1ctabd = new TXCTABD().Read("pT", t1ctabd.PARM1);

            if (t1ctabd is null)
                return false;

            // 02 => External (Console, Mobile, etc.)
            return t1ctabd.TXNUM == "02";
        }

        private static void DeleteAllLinkedT1GRPPOs(T1PROFI t1profi)
        {
            // Delete linked data
            TXGRPPO txgrppo = new TXGRPPO();
            List<T1GRPPO> t1grppos = txgrppo.ReadAll();

            t1grppos = t1grppos.Where(g => g.PFID == t1profi.PFID).ToList();

            foreach (T1GRPPO t1grppo in t1grppos)
            {
                txgrppo.Delete(t1grppo.GPID);
            }
        }

        private static void DeleteAllLinkedT1SESSIs(T1PROFI t1profi)
        {
            // Delete linked data
            List<T1SESSI> t1sessis = GetAllSessions(t1profi);

            foreach (T1SESSI t1sessi in t1sessis)
            {
                new TXSESSI().Delete(t1sessi.SEID);
            }
        }

        private static void DeleteAllLinkedT1PLTHRs(T1PROFI t1profi)
        {
            List<T1PLTHR> t1plthrs = GetAllPlaythroughs(t1profi);

            foreach (T1PLTHR t1plthr in t1plthrs)
            {
                new TXPLTHR().Delete(t1plthr.PTID);
            }
        }

        private static void DeleteCoverImage(T1PROFI t1profi)
        {
            if (!FnString.IsNullEmptyOrWhitespace(t1profi.PPFN))
            {
                try
                {
                    string coverPath = Path.Combine(AppConfig.Storage.ProfileCoversDirectoryPath ?? string.Empty, t1profi.PPFN);
                    File.Delete(coverPath);
                }
                catch (FileNotFoundException fnfe)
                {
                }
            }
        }

        private static UIXQuery BuildLinkedTagsQuery(long pfid)
        {
            UIXQuery query = new UIXQuery(K1GRPPO.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            // Add fields
            query.AddField(K1GRPPO.Name, K1GRPPO.Fields.TGKY);

            // Where restrictions
            query.AddWhere(K1GRPPO.Name, K1GRPPO.Fields.PFID, QueryCompareType.EQUALS, pfid);

            return query;
        }
    }
}
