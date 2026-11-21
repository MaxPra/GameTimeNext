using GameTimeNext.Core.Application.Profiles.Controller;
using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Framework.UI;
using System.Collections.ObjectModel;
using System.Windows;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Profiles.Selectors
{
    public class ProfilesEditView_TagsSelectorController : UIXSelectorControllerBase<ProfilesEditApp, ProfilesEditView, UIXListBox>
    {
        public ProfilesEditView_TagsSelectorController(UIXApplication app) : base(app)
        {
        }

        /// <summary>
        /// Cross on a tag card: the card plays its removal, then the profile loses the tag.
        /// </summary>
        protected async Task EV_btnRemoveTag(FrameworkElement item)
        {
            if (item.DataContext is not ProfilesGroupListBoxItem tag)
                return;

            await ItemAnimations.PlayRemoveAsync(item);

            if (GetView().lbTags.ItemsSource is ObservableCollection<ProfilesGroupListBoxItem> tags)
                tags.Remove(tag);

            GetViewController<ProfilesEditViewController>().RefreshTagChoices();
        }
    }
}
