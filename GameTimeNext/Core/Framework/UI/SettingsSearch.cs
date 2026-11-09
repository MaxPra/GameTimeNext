using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Search in a settings view built from pages (<see cref="TabItem"/>), cards (<see cref="GroupBox"/> with a
    /// panel) and settings (the children of that panel). Pages, cards and settings that do not match the search words
    /// are collapsed; a card whose header matches stays complete.
    /// </summary>
    public static class SettingsSearch
    {
        // Marks elements collapsed by the search, so only those are shown again (others are collapsed by a controller)
        private static readonly DependencyProperty HiddenBySearchProperty =
            DependencyProperty.RegisterAttached("HiddenBySearch", typeof(bool), typeof(SettingsSearch), new PropertyMetadata(false));

        /// <summary>
        /// Applies the search and returns whether anything is shown. An empty search shows everything again.
        /// If the selected page is hidden, the first page with a match is selected.
        /// </summary>
        public static bool Apply(TabControl tabs, string? query)
        {
            string[] words = (query ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            bool anyShown = false;

            foreach (TabItem page in tabs.Items.OfType<TabItem>())
            {
                bool pageShown = ApplyToPage(page, words);
                SetHidden(page, !pageShown);
                anyShown |= pageShown;
            }

            if (tabs.SelectedItem is not TabItem selected || selected.Visibility != Visibility.Visible)
                tabs.SelectedItem = tabs.Items.OfType<TabItem>().FirstOrDefault(p => p.Visibility == Visibility.Visible);

            return anyShown;
        }

        private static bool ApplyToPage(TabItem page, string[] words)
        {
            // The name of the page counts too: "backup" shows the whole page
            if (words.Length == 0 || Matches(page.Header?.ToString(), words))
            {
                foreach (GroupBox card in FindCards(page))
                    ApplyToCard(card, Array.Empty<string>());

                return true;
            }

            bool anyCard = false;

            foreach (GroupBox card in FindCards(page))
                anyCard |= ApplyToCard(card, words);

            return anyCard;
        }

        private static bool ApplyToCard(GroupBox card, string[] words)
        {
            // A card collapsed by a controller (depending on another setting) is left alone
            if (card.Visibility == Visibility.Collapsed && !GetHidden(card))
                return false;

            bool cardMatches = words.Length == 0 || Matches(card.Header?.ToString(), words);
            bool anyRow = false;

            if (card.Content is Panel panel)
            {
                foreach (UIElement row in panel.Children)
                {
                    bool rowShown = cardMatches || Matches(CollectText(row), words);
                    SetHidden(row, !rowShown);
                    anyRow |= rowShown;
                }
            }
            else
            {
                anyRow = cardMatches;
            }

            SetHidden(card, !anyRow);

            return anyRow;
        }

        private static IEnumerable<GroupBox> FindCards(DependencyObject root)
        {
            foreach (object child in LogicalTreeHelper.GetChildren(root))
            {
                if (child is GroupBox card)
                {
                    yield return card;
                }
                else if (child is DependencyObject element)
                {
                    foreach (GroupBox nested in FindCards(element))
                        yield return nested;
                }
            }
        }

        private static bool GetHidden(UIElement element)
        {
            return (bool)element.GetValue(HiddenBySearchProperty);
        }

        private static void SetHidden(UIElement element, bool hidden)
        {
            if (hidden)
            {
                if (element.Visibility == Visibility.Visible)
                {
                    element.SetValue(HiddenBySearchProperty, true);
                    element.Visibility = Visibility.Collapsed;
                }
            }
            else if (GetHidden(element))
            {
                element.SetValue(HiddenBySearchProperty, false);
                element.Visibility = Visibility.Visible;
            }
        }

        private static bool Matches(string? text, string[] words)
        {
            if (words.Length == 0)
                return true;

            if (text == null)
                return false;

            return words.All(w => CultureInfo.CurrentCulture.CompareInfo.IndexOf(text, w, CompareOptions.IgnoreCase) >= 0);
        }

        /// <summary>
        /// All texts a setting shows: labels, hints, content of buttons and check boxes, tool tips.
        /// </summary>
        private static string CollectText(DependencyObject element)
        {
            List<string> texts = new List<string>();
            Collect(element, texts);

            return string.Join(' ', texts);
        }

        private static void Collect(DependencyObject element, List<string> texts)
        {
            if (element is TextBlock textBlock && !string.IsNullOrEmpty(textBlock.Text))
                texts.Add(textBlock.Text);

            if (element is ContentControl { Content: string content })
                texts.Add(content);

            if (element is FrameworkElement { ToolTip: string tip })
                texts.Add(tip);

            foreach (object child in LogicalTreeHelper.GetChildren(element))
            {
                if (child is DependencyObject childElement)
                    Collect(childElement, texts);
            }
        }
    }
}
