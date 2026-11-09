using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Application.Profiles.Viewmodel;
using UIX.ViewController.Engine.Utils;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Profiles.Selectors
{
    /// <summary>
    /// Tags the edited profile has, shown as cards. Hidden tags are included, so the profile can lose them.
    /// </summary>
    public class ProfilesEditView_TagsSelectorAdapter : UIXSelectorAdapterBase<ProfilesEditApp, ProfilesEditView, UIXListBox, ProfilesGroupListBoxItem>
    {
        public ProfilesEditView_TagsSelectorAdapter(UIXApplication app) : base(app)
        {
        }

        protected override async Task<IEnumerable<ProfilesGroupListBoxItem>> ExecuteAsync(CancellationToken token)
        {
            long pfid = GetApp().T1Profi.PFID;

            return await Task.Run(() =>
            {
                Dictionary<string, long> linkedGpidsByTagKey = pfid > 0
                    ? new TXGRPPO().ReadAll()
                        .Where(g => g.PFID == pfid && !FnString.IsNullEmptyOrWhitespace(g.TGKY))
                        .GroupBy(g => g.TGKY, StringComparer.Ordinal)
                        .ToDictionary(g => g.Key, g => g.First().GPID, StringComparer.Ordinal)
                    : new Dictionary<string, long>(StringComparer.Ordinal);

                return TFCTABD.GetTags(includeHidden: true)
                    .Where(t => linkedGpidsByTagKey.ContainsKey(t.TXNUM))
                    .Select(t => new ProfilesGroupListBoxItem
                    {
                        ItemObject = t,
                        CONAME = t.DESCR,
                        COGRPID = linkedGpidsByTagKey[t.TXNUM],
                        COISSEL = true
                    })
                    .ToList();
            }, token);
        }
    }
}
