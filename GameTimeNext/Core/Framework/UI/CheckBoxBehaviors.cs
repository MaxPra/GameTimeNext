using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Plays the check / uncheck animation of a check box box (CheckBoxStyle.xaml) when its state changes.
    /// An EventTrigger in the template can't do this: IsChecked is often set before the template is applied
    /// (bindings of list entries, tabs not shown yet), and the storyboard would then throw, because its targets don't exist.
    /// </summary>
    public static class CheckBoxBehaviors
    {
        private const string CheckAnimationKey = "CheckBox.CheckAnimation";
        private const string UncheckAnimationKey = "CheckBox.UncheckAnimation";

        /// <summary>The template of the check box contains the targets of the animations (Fill, Tick, BoxHost).</summary>
        public static readonly DependencyProperty AnimateCheckProperty = DependencyProperty.RegisterAttached(
            "AnimateCheck",
            typeof(bool),
            typeof(CheckBoxBehaviors),
            new PropertyMetadata(false, OnAnimateCheckChanged));

        public static bool GetAnimateCheck(DependencyObject element) => (bool)element.GetValue(AnimateCheckProperty);

        public static void SetAnimateCheck(DependencyObject element, bool value) => element.SetValue(AnimateCheckProperty, value);

        private static void OnAnimateCheckChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CheckBox checkBox)
                return;

            checkBox.Checked -= CheckBox_Checked;
            checkBox.Unchecked -= CheckBox_Unchecked;

            if ((bool)e.NewValue)
            {
                checkBox.Checked += CheckBox_Checked;
                checkBox.Unchecked += CheckBox_Unchecked;
            }
        }

        private static void CheckBox_Checked(object sender, RoutedEventArgs e) => Play((CheckBox)sender, CheckAnimationKey);

        private static void CheckBox_Unchecked(object sender, RoutedEventArgs e) => Play((CheckBox)sender, UncheckAnimationKey);

        private static void Play(CheckBox checkBox, string resourceKey)
        {
            // Template not applied yet: its IsChecked trigger shows the state as soon as it is
            if (checkBox.Template?.FindName("Fill", checkBox) == null)
                return;

            if (checkBox.TryFindResource(resourceKey) is Storyboard storyboard)
                storyboard.Begin(checkBox, checkBox.Template);
        }
    }
}
