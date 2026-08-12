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
        public static List<T1PROFI> GetPlayedProfiles(DateTime? timeSpanStart, DateTime timeSpanEnd)
        {
            UIXQuery query = Queries.BuildQueryPlayedProfiles(timeSpanStart, timeSpanEnd);

            List<T1PROFI> t1profis = new List<T1PROFI>();

            TXPROFI txprofi = new TXPROFI();
            using (var reader = query.Execute())
                while (reader.Read())
                    t1profis.Add(txprofi.Read(UIXQuery.GetInt64(reader, K1SESSI.Name, K1SESSI.Fields.PFID))!);

            return t1profis;
        }

        public static int GetPlayedProfilesCount(DateTime? timeSpanStart, DateTime timeSpanEnd)
        {
            UIXQuery query = Queries.BuildQueryPlayedProfilesCount(timeSpanStart, timeSpanEnd);

            using (var reader = query.Execute())
                if (reader.Read())
                    return UIXQuery.GetInt32(reader, "TotalGames");

            return 0;
        }

        public static double GetPlaytime(DateTime? timeSpanStart, DateTime timeSpanEnd, long? pfid = null)
        {
            UIXQuery query = Queries.BuildQueryPlaytime(timeSpanStart, timeSpanEnd, pfid: pfid);

            return CalculatePlaytime(timeSpanStart, timeSpanEnd, query);
        }

        public static int GetDaysPlayed(DateTime? timeSpanStart, DateTime timeSpanEnd, long? pfid = null)
        {
            string query = Queries.BuildQueryDaysPlayed(timeSpanStart, timeSpanEnd, pfid);

            using (var reader = UIXQuery.ExecuteCustom(query, AppEnvironment.GetDataBaseManager().GetConnection()))
                if (reader.Read())
                    return UIXQuery.GetInt32(reader, "DAYS");

            return 0;
        }

        public static double GetPlaytimeToday(DateTime? timeSpanStart, DateTime timeSpanEnd)
        {
            UIXQuery query = Queries.BuildQueryPlaytime(timeSpanStart, timeSpanEnd, today: true);

            return CalculatePlaytime(timeSpanStart, timeSpanEnd, query);
        }

        public static (string gana, double plti, DateTime plto) GetLongestSession(DateTime? timeSpanStart, DateTime timeSpanEnd)
        {
            UIXQuery query = Queries.BuildQueryLongestSession(timeSpanStart, timeSpanEnd);

            using (var reader = query.Execute())
                if (reader.Read())
                    return (
                        UIXQuery.GetString(reader, "GANA"),
                        UIXQuery.GetDouble(reader, "PLTI"),
                        UIXQuery.GetDateTime(reader, "PLTO")
                    );

            return ("n.A.", 0, DateTime.MinValue);
        }

        public static (string gana, DateTime lapl, double plti, string ppfn) GetLastPlayed(DateTime? timeSpanStart, DateTime timeSpanEnd)
        {
            UIXQuery query = Queries.BuildQueryLastPlayed(timeSpanStart, timeSpanEnd);

            using (var reader = query.Execute())
                if (reader.Read())
                    return (
                        UIXQuery.GetString(reader, "GANA"),
                        UIXQuery.GetDateTime(reader, "PLTO"),
                        TFPROFI.GetGameTimeInMinutes(UIXQuery.GetInt64(reader, "PFID"), timeSpanStart, timeSpanEnd),
                        Path.Combine(AppConfig.Storage.ProfileCoversDirectoryPath ?? string.Empty, UIXQuery.GetString(reader, "PPFN"))
                    );

            return ("n.A.", DateTime.MinValue, 0, string.Empty);
        }

        public static (string gana, double plti, int days, string ppfn) GetMostPlayed(DateTime? timeSpanStart, DateTime timeSpanEnd)
        {
            UIXQuery query = Queries.BuildQueryMostPlayed(timeSpanStart, timeSpanEnd);

            using (var reader = query.Execute())
                if (reader.Read())
                    return (
                        UIXQuery.GetString(reader, "GANA"),
                        GetPlaytime(timeSpanStart, timeSpanEnd, UIXQuery.GetInt64(reader, "PFID")),
                        GetDaysPlayed(timeSpanStart, timeSpanEnd, UIXQuery.GetInt64(reader, "PFID")),
                        Path.Combine(AppConfig.Storage.ProfileCoversDirectoryPath ?? string.Empty, UIXQuery.GetString(reader, "PPFN"))
                    );

            return ("n.A.", 0, 0, string.Empty);
        }

        private static double CalculatePlaytime(DateTime? timeSpanStart, DateTime timeSpanEnd, UIXQuery query)
        {
            double playtime = 0;

            using (var reader = query.Execute())
                while (reader.Read())
                {
                    DateTime plfr = UIXQuery.GetDateTime(reader, "PLFR");
                    DateTime plto = UIXQuery.GetDateTime(reader, "PLTO");
                    double plti = UIXQuery.GetDouble(reader, "PLTI");

                    if ((timeSpanStart is null || plfr >= timeSpanStart) && plto <= timeSpanEnd)
                    {
                        // Same day
                        playtime += plti;
                        continue;
                    }
                    else if (timeSpanStart is not null && plfr < timeSpanStart)
                    {
                        playtime += (plto - (DateTime)timeSpanStart).TotalMinutes;
                    }
                    else if (plto > timeSpanEnd)
                    {
                        playtime += (timeSpanEnd - plfr).TotalMinutes;
                    }
                }

            return playtime;
        }

        private class Queries
        {
            public static UIXQuery BuildQueryPlayedProfiles(DateTime? timeSpanStart, DateTime timeSpanEnd)
            {
                UIXQuery query = BuildQuerySessionsInTimeSpanBase(timeSpanStart, timeSpanEnd);
                query.SetDistinct();

                query.AddField(K1SESSI.Name, K1SESSI.Fields.PFID);

                query.AddOrderBy(K1SESSI.Name, K1SESSI.Fields.PLFR, OrderDirection.DESC);

                return query;
            }

            public static UIXQuery BuildQueryPlayedProfilesCount(DateTime? timeSpanStart, DateTime timeSpanEnd)
            {
                UIXQuery query = BuildQuerySessionsInTimeSpanBase(timeSpanStart, timeSpanEnd);

                query.AddCount(K1SESSI.Name, K1SESSI.Fields.PFID, true, "TotalGames");

                return query;
            }

            public static UIXQuery BuildQueryPlaytime(DateTime? timeSpanStart, DateTime timeSpanEnd, bool today = false, long? pfid = null)
            {
                UIXQuery query = BuildQuerySessionsInTimeSpanBase(timeSpanStart, timeSpanEnd, today: today, pfid: pfid);

                query.AddField(K1SESSI.Name, K1SESSI.Fields.PLFR, "PLFR");
                query.AddField(K1SESSI.Name, K1SESSI.Fields.PLTO, "PLTO");
                query.AddField(K1SESSI.Name, K1SESSI.Fields.PLTI, "PLTI");

                return query;
            }

            public static string BuildQueryDaysPlayed(DateTime? timeSpanStart, DateTime timeSpanEnd,long? pfid = null)
            {
                string sqlPlfr = "";
                string sqlPlto = "";

                {
                    // PLFR
                    UIXQuery query = BuildQuerySessionsInTimeSpanBase(timeSpanStart, timeSpanEnd, pfid: pfid);
                    query.AddFieldRaw($"DATE({K1SESSI.Name}.{K1SESSI.Fields.PLFR})", "PLAYDAY");

                    sqlPlfr = query.PreviewQuery();
                }

                {
                    // PLTO
                    UIXQuery query = BuildQuerySessionsInTimeSpanBase(timeSpanStart, timeSpanEnd, pfid: pfid);
                    query.AddFieldRaw($"DATE({K1SESSI.Name}.{K1SESSI.Fields.PLTO})", "PLAYDAY");

                    sqlPlto = query.PreviewQuery();
                }

                return $"SELECT COUNT(DISTINCT PLAYDAY) AS DAYS FROM ({sqlPlfr} UNION {sqlPlto})";
            }

            public static UIXQuery BuildQueryLongestSession(DateTime? timeSpanStart, DateTime timeSpanEnd)
            {
                UIXQuery query = BuildQuerySessionsInTimeSpanBase(timeSpanStart, timeSpanEnd);
                query.SetTopX(1);

                UIXQueryTable t1profi = query.AddJoinTable(K1PROFI.Name, JoinType.LEFT);
                t1profi.AddJoinCondition(K1SESSI.Name, K1SESSI.Fields.PFID, QueryCompareType.EQUALS, K1PROFI.Name, K1PROFI.Fields.PFID);

                query.AddField(K1PROFI.Name, K1PROFI.Fields.GANA, "GANA");
                query.AddField(K1SESSI.Name, K1SESSI.Fields.PLTI, "PLTI");
                query.AddField(K1SESSI.Name, K1SESSI.Fields.PLTO, "PLTO");

                query.AddOrderBy(K1SESSI.Name, K1SESSI.Fields.PLTI, OrderDirection.DESC);

                return query;
            }

            private static UIXQuery BuildQuerySessionsInTimeSpanBase(DateTime? timeSpanStart, DateTime timeSpanEnd, bool today = false, long? pfid = null)
            {
                DateTime? start = timeSpanStart;
                DateTime end = timeSpanEnd;

                if (today)
                {
                    (start, end) = FnTimeSpan.GetBeginningAndEnd(FnTimeSpan.TimeSpanType.Day);
                }

                return TFSESSI.BuildQuerySessionsInTimeSpanBase(start, end, pfid: pfid);
            }

            public static UIXQuery BuildQueryLastPlayed(DateTime? timeSpanStart, DateTime timeSpanEnd)
            {
                UIXQuery query = TFSESSI.BuildQuerySessionsInTimeSpanBase(timeSpanStart, timeSpanEnd);
                query.SetTopX(1);

                UIXQueryTable t1profi = query.AddJoinTable(K1PROFI.Name, JoinType.LEFT);
                t1profi.AddJoinCondition(K1SESSI.Name, K1SESSI.Fields.PFID, QueryCompareType.EQUALS, K1PROFI.Name, K1PROFI.Fields.PFID);

                query.AddField(K1PROFI.Name, K1PROFI.Fields.PFID, "PFID");
                query.AddField(K1PROFI.Name, K1PROFI.Fields.GANA, "GANA");
                query.AddField(K1PROFI.Name, K1PROFI.Fields.PPFN, "PPFN");
                query.AddField(K1SESSI.Name, K1SESSI.Fields.PLTO, "PLTO");

                query.AddOrderBy(K1SESSI.Name, K1SESSI.Fields.PLTO, OrderDirection.DESC);

                return query;
            }

            public static UIXQuery BuildQueryMostPlayed(DateTime? timeSpanStart, DateTime timeSpanEnd)
            {
                UIXQuery query = TFSESSI.BuildQuerySessionsInTimeSpanBase(timeSpanStart, timeSpanEnd);
                query.SetTopX(1);

                UIXQueryTable t1profi = query.AddJoinTable(K1PROFI.Name, JoinType.LEFT);
                t1profi.AddJoinCondition(K1SESSI.Name, K1SESSI.Fields.PFID, QueryCompareType.EQUALS, K1PROFI.Name, K1PROFI.Fields.PFID);

                query.AddField(K1PROFI.Name, K1PROFI.Fields.PFID, "PFID");
                query.AddField(K1PROFI.Name, K1PROFI.Fields.GANA, "GANA");
                query.AddField(K1PROFI.Name, K1PROFI.Fields.PPFN, "PPFN");
                query.AddSum(K1SESSI.Name, K1SESSI.Fields.PLTI, "PLTI");

                query.AddGroupBy(K1SESSI.Name, K1SESSI.Fields.PFID);
                query.AddOrderByAlias("PLTI", OrderDirection.DESC);

                return query;
            }
        }
    }
}
