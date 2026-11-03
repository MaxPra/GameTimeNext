using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.Settings.Viewmodels
{
    /// <summary>
    /// Launcher in the list of the automatic game import (settings "Game import").
    /// </summary>
    public class SettingsAutoImportLauncherListItem : UIXListItem
    {
        // Key of the codetable lT
        public string LauncherType { get; set; } = string.Empty;

        public string CONAME { get; set; } = string.Empty;

        // Takes part in the automatic import
        public bool COISSEL { get; set; }
    }
}
