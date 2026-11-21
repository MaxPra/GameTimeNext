using Arcora.Core.Application.Dashboard.Controller;
using Arcora.Core.Application.Dashboard.Views;
using Arcora.Core.Application.DataManagers;
using Arcora.Core.Application.Profiles;
using Arcora.Core.Application.Profiles.Viewmodel;
using Arcora.Core.Application.TableObjects;
using Arcora.Core.Framework.Utils;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;

namespace Arcora.Core.Application.Dashboard.Selectors
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
                CODISEC = TFPROFI.GetDiscSection(prof),
                COISPLA = FnSystem.IsExeFoundInPath(prof.EXGF),
                COCPLTI = FnTime.FormatMinutesAsHoursAndMinutes(TFPROFI.GetGameTimeInMinutes(prof.PFID, timeSpanStart, timeSpanEnd))
            }).ToList();
        }
    }
}
