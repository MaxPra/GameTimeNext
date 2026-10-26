using GameTimeNext.Core.Application.BigPicture.ViewModels;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Screensetups;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.UI.Base;
using GameTimeNext.Core.Framework.UI.Dialogs;
using System.Globalization;
using System.Windows;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.BigPicture.Controller
{
    /// <summary>
    /// Switching the screensetup (monitors and audio device, module Screensetups) from the mode, e.g. from the desk
    /// monitors to the TV.
    /// </summary>
    public partial class BigPictureViewController
    {
        // Screensetup menu: option key -> screensetup
        private readonly Dictionary<string, T1SCRSH> _screensetupOptions = new Dictionary<string, T1SCRSH>();

        private static bool HasScreensetups()
        {
            return new TXSCRSH().ReadAll().Count > 0;
        }

        /// <summary>
        /// Menu "Screen setup": all screensetups, the active one checked and selected.
        /// </summary>
        private void OpenScreensetupMenu()
        {
            long activeSsid = AppEnvironment.GetAppConfig().UserSettings.SelectedScreensetup;

            List<T1SCRSH> t1scrshs = new TXSCRSH().ReadAll()
                .OrderBy(s => s.SSNA, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            _screensetupOptions.Clear();

            List<BigPictureMenuOption> options = new List<BigPictureMenuOption>();

            foreach (T1SCRSH t1scrsh in t1scrshs)
            {
                string key = "screensetup." + t1scrsh.SSID.ToString(CultureInfo.InvariantCulture);

                _screensetupOptions[key] = t1scrsh;
                options.Add(new BigPictureMenuOption(key, t1scrsh.SSNA, t1scrsh.SSID == activeSsid ? "" : ""));
            }

            int activeIndex = Math.Max(0, t1scrshs.FindIndex(s => s.SSID == activeSsid));

            OpenMenu(MenuKind.Screensetup, "", "Screen setup", "Switches the monitors and the audio device.", options, activeIndex);
        }

        private async Task ApplyScreensetupAsync(string key)
        {
            if (!_screensetupOptions.TryGetValue(key, out T1SCRSH? t1scrsh))
                return;

            string? errorMessage;

            GetApp().Loader.Begin("Switching to " + t1scrsh.SSNA + "...");

            try
            {
                errorMessage = await CFScreensetupsApp.ApplyScreensetupAsync(GetApp(), t1scrsh);
            }
            finally
            {
                GetApp().Loader.Stop();
            }

            FnLog.AddInfo(GetApp(), errorMessage == null
                ? $"Screensetup {t1scrsh.SSNA} applied from the Big Picture mode"
                : $"Screensetup {t1scrsh.SSNA} from the Big Picture mode failed: {errorMessage}");

            // The mode goes to the new primary monitor (e.g. the TV), with its size
            if (Window.GetWindow(View) is GTNWindow window)
                window.MoveFullScreenToPrimaryMonitor();

            EnsureFocus();

            // Shown in the dialog of the mode (CFMBOX.Presenter)
            if (errorMessage != null)
                GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window).Show(errorMessage, CFMBOXResult.Ok, CFMBOXIcon.Error);
        }
    }
}
