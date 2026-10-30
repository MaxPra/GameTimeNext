using GameTimeNext.Core.Application.Dashboard.Controller;
using GameTimeNext.Core.Application.Dashboard.Views;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.Utils;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Dashboard.Selectors
{
    public class DashboardView_RecentGamesSelectorAdapter : UIXSelectorAdapterBase<DashboardApp, DashboardView, UIXListView, ProfilesListBoxItem>
    {
        public DashboardView_RecentGamesSelectorAdapter(UIXApplication app) : base(app)
        {
        }

        protected override async Task<IEnumerable<ProfilesListBoxItem>> ExecuteAsync(CancellationToken token)
        {
            // The period is chosen in the view
            (DateTime? timeSpanStart, DateTime timeSpanEnd) = GetViewController<DashboardViewController>().GetTimeSpan();

            return await Task.Run(() => BuildItems(TFPROFI.GetPlayedProfiles(timeSpanStart, timeSpanEnd), timeSpanStart, timeSpanEnd), token);
        }

        private static List<ProfilesListBoxItem> BuildItems(List<T1PROFI> t1profiles, DateTime? timeSpanStart, DateTime timeSpanEnd)
        {
            return t1profiles.Select(prof => new ProfilesListBoxItem
            {
                ItemObject = prof,
                COCOVIM = CFProfilesApp.LoadProfileCover(prof.PPFN),
                COISPLA = FnSystem.IsExeFoundInPath(prof.EXGF),
                COCPLTI = FnTime.FormatMinutesAsHoursAndMinutes(TFPROFI.GetGameTimeInMinutes(prof.PFID, timeSpanStart, timeSpanEnd))
            }).ToList();
        }
    }
}
