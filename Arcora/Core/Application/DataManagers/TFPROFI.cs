using GameTimeNext.Core.Application.Profiles.Components;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.UI;
using GameTimeNext.Core.Framework.Utils;
using System.IO;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.DataManagers
{
    public partial class TFPROFI
    {
        /// <summary>
        /// Deletes the given profile including its linked data
        /// </summary>
        public static void DeleteT1PROFIAndLinkedData(UIXApplication app, T1PROFI t1profi)
        {
            TXPROFI txprofi = app.GetDataManager<TXPROFI>();

            DeleteAllLinkedT1GRPPOs(app, t1profi);
            DeleteAllLinkedT1SESSIs(app, t1profi);
            DeleteAllLinkedT1PLTHRs(app, t1profi);

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

        public static List<T1SESSI> GetAllSessions(UIXApplication app, T1PROFI t1profi)
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
                    t1sessis.Add(app.GetDataManager<TXSESSI>().Read(seid));
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
        /// Only while the setting SealNewGames is on; turning it off unwraps all games (UnsealAll).
        /// </summary>
        public static bool IsSealed(T1PROFI t1profi)
        {
            return AppEnvironment.GetAppConfig().AppSettings.SealNewGames
                && new CProfileSettings(t1profi.PRSE).Dezerialize().Sealed;
        }

        /// <summary>
        /// Unwraps the game and saves the profile (read again under the record lock, the given object gets the new
        /// settings too). False if it is being edited elsewhere or its game is monitored (the user is told).
        /// </summary>
        public static bool Unseal(UIXApplication app, T1PROFI t1profi)
        {
            return FnRecordLock.Run(app, () =>
            {
                TXPROFI txprofi = app.GetDataManager<TXPROFI>();
                T1PROFI current = txprofi.Read(t1profi.PFID) ?? t1profi;

                // Unwrapping is looking at the new game
                if (RemoveSealedFlag(current, clearNewMark: true))
                    txprofi.Save(current);

                t1profi.PRSE = current.PRSE;
            }, t1profi);
        }

        /// <summary>
        /// The profile and the records that belong to it (playthroughs, group positions), e.g. to lock them all
        /// (monitoring, deleting). Empty if the profile doesn't exist.
        /// </summary>
        public static List<UIXTableObjectBase> GetProfileWithLinkedRecords(UIXApplication app, long pfid)
        {
            List<UIXTableObjectBase> records = new List<UIXTableObjectBase>();
            T1PROFI? t1profi = app.GetDataManager<TXPROFI>().Read(pfid);

            if (t1profi == null)
                return records;

            records.Add(t1profi);
            records.AddRange(GetAllPlaythroughs(app, t1profi));
            records.AddRange(app.GetDataManager<TXGRPPO>().ReadAll().Where(g => g.PFID == pfid));

            return records;
        }

        /// <summary>
        /// Removes the shrink wrap from the profile object without saving it (also when the setting SealNewGames is
        /// off). clearNewMark also removes the badge "New". Returns true if the profile was sealed.
        /// </summary>
        public static bool RemoveSealedFlag(T1PROFI t1profi, bool clearNewMark)
        {
            CProfileSettings cProfileSettings = new CProfileSettings(t1profi.PRSE).Dezerialize();

            if (!cProfileSettings.Sealed)
                return false;

            cProfileSettings.Sealed = false;

            if (clearNewMark)
                cProfileSettings.IsNew = false;

            t1profi.PRSE = cProfileSettings.Serialize();

            return true;
        }

        /// <summary>
        /// All profiles that are still shrink-wrapped, regardless of the setting SealNewGames.
        /// </summary>
        public static List<T1PROFI> GetSealedProfiles(UIXApplication app)
        {
            return app.GetDataManager<TXPROFI>().ReadAll()
                .Where(p => new CProfileSettings(p.PRSE).Dezerialize().Sealed)
                .ToList();
        }

        /// <summary>
        /// Unwraps all sealed profiles and saves them (setting SealNewGames switched off, otherwise they would be
        /// sealed again when it is switched on). The badge "New" stays: the games were not looked at yet.
        /// </summary>
        public static void UnsealAll(UIXApplication app)
        {
            TXPROFI txprofi = app.GetDataManager<TXPROFI>();

            foreach (T1PROFI t1profi in GetSealedProfiles(app))
            {
                // Being edited or monitored (record lock): stays sealed, which doesn't matter while the setting is off
                if (!t1profi.Lock())
                {
                    FnLog.AddInfo(app, $"{t1profi.GANA} wasn't unwrapped: it is locked by {t1profi.GetLockHolder()?.Holder.AppProperties.ApplicationName}.");
                    continue;
                }

                try
                {
                    if (RemoveSealedFlag(t1profi, clearNewMark: false))
                        txprofi.Save(t1profi);
                }
                finally
                {
                    t1profi.Unlock();
                }
            }
        }

        /// <summary>
        /// True if the profile was added by the automatic import and not looked at yet (badge "New").
        /// </summary>
        public static bool IsMarkedNew(T1PROFI t1profi)
        {
            return new CProfileSettings(t1profi.PRSE).Dezerialize().IsNew;
        }

        /// <summary>
        /// The section of the cover the user chose for the disc; null: determined automatically (DiscPrint).
        /// </summary>
        public static DiscSection? GetDiscSection(T1PROFI t1profi)
        {
            return ToDiscSection(t1profi.DPRX, t1profi.DPRY, t1profi.DPRS);
        }

        private static DiscSection? ToDiscSection(double x, double y, double size)
        {
            return size > 0 ? new DiscSection(x, y, size) : null;
        }

        /// <summary>
        /// Removes the badge "New" when the profile is selected and saves it (read again under the record lock, the given
        /// object gets the new settings too). A sealed game keeps it: it is removed when the game is unwrapped; a profile
        /// that is being edited or monitored keeps it until it is selected again. Returns true if the badge was removed.
        /// </summary>
        public static bool ClearNewMarkOnSelection(UIXApplication app, T1PROFI t1profi)
        {
            if (!IsMarkedNew(t1profi) || IsSealed(t1profi))
                return false;

            // Only a selection: no message when it is locked
            if (!t1profi.Lock())
                return false;

            try
            {
                TXPROFI txprofi = app.GetDataManager<TXPROFI>();
                T1PROFI current = txprofi.Read(t1profi.PFID) ?? t1profi;

                CProfileSettings cProfileSettings = new CProfileSettings(current.PRSE).Dezerialize();
                cProfileSettings.IsNew = false;
                current.PRSE = cProfileSettings.Serialize();

                txprofi.Save(current);

                t1profi.PRSE = current.PRSE;
            }
            finally
            {
                t1profi.Unlock();
            }

            return true;
        }

        /// <summary>
        /// Saves a profile created without the edit view (automatic import) together with its initial playthrough,
        /// as saving a new profile in the edit view does.
        /// </summary>
        public static void SaveNewProfile(UIXApplication app, T1PROFI t1profi)
        {
            app.GetDataManager<TXPROFI>().Save(t1profi);

            TFPLTHR.CreateNewPlaythrough(app, t1profi.PFID);
        }

        public static List<T1PLTHR> GetAllPlaythroughs(UIXApplication app, T1PROFI t1profi)
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
                    t1plthrs.Add(app.GetDataManager<TXPLTHR>().Read(ptid));
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

        private static void DeleteAllLinkedT1GRPPOs(UIXApplication app, T1PROFI t1profi)
        {
            // Delete linked data
            TXGRPPO txgrppo = app.GetDataManager<TXGRPPO>();
            List<T1GRPPO> t1grppos = txgrppo.ReadAll();

            t1grppos = t1grppos.Where(g => g.PFID == t1profi.PFID).ToList();

            foreach (T1GRPPO t1grppo in t1grppos)
            {
                txgrppo.Delete(t1grppo.GPID);
            }
        }

        private static void DeleteAllLinkedT1SESSIs(UIXApplication app, T1PROFI t1profi)
        {
            // Delete linked data
            List<T1SESSI> t1sessis = GetAllSessions(app, t1profi);

            foreach (T1SESSI t1sessi in t1sessis)
            {
                app.GetDataManager<TXSESSI>().Delete(t1sessi.SEID);
            }
        }

        private static void DeleteAllLinkedT1PLTHRs(UIXApplication app, T1PROFI t1profi)
        {
            List<T1PLTHR> t1plthrs = GetAllPlaythroughs(app, t1profi);

            foreach (T1PLTHR t1plthr in t1plthrs)
            {
                app.GetDataManager<TXPLTHR>().Delete(t1plthr.PTID);
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
