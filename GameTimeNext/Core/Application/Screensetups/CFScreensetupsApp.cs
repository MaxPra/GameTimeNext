using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using UIX.ViewController.Engine.Utils;
using UIXMonitorTweaks;

namespace GameTimeNext.Core.Application.Screensetups
{
    public class CFScreensetupsApp
    {
        // COM based: created lazily and only used on the UI thread
        private static UIXAudioManager? _audioManager;

        public static UIXAudioManager GetAudioManager()
        {
            _audioManager ??= new UIXAudioManager();
            return _audioManager;
        }

        /// <summary>
        /// Applies the monitors and the audio device of a screensetup. Returns an error message or null on success.
        /// </summary>
        public static async Task<string?> ApplyScreensetupAsync(T1SCRSH t1scrsh)
        {
            List<T1SCRSP> t1scrsps = TFSCRSP.GetMonitorsOfScreensetup(t1scrsh.SSID);

            // Monitors that are not connected right now cannot be switched
            HashSet<string> connectedMonitorIds = UIXMonitorManager.GetMonitors().Select(m => m.Id).ToHashSet();

            List<UIXMonitorTarget> targets = t1scrsps
                .Where(p => connectedMonitorIds.Contains(p.MOID))
                .Select(p => new UIXMonitorTarget(p.MOID, p.MOEN, p.MOEN && p.MOPR))
                .ToList();

            if (!targets.Any(t => t.Enabled))
                return "None of the monitors enabled in this screensetup is connected.";

            try
            {
                await Task.Run(() => UIXMonitorManager.Apply(targets));
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                // The library message names the real cause (e.g. open display settings)
                return ex.Message;
            }

            // Audio after the monitors: an HDMI output only exists once its monitor is on
            if (!FnString.IsNullEmptyOrWhitespace(t1scrsh.ADID) && !GetAudioManager().SetDefaultPlaybackDevice(t1scrsh.ADID))
                return $"The monitors have been switched, but the audio device \"{t1scrsh.ADNA}\" could not be set as default.";

            AppEnvironment.GetAppConfig().UserSettings.SelectedScreensetup = t1scrsh.SSID;
            AppEnvironment.SaveAppConfig();

            return null;
        }

        public static string FormatMonitors(List<T1SCRSP> t1scrsps)
        {
            List<string> monitorNames = t1scrsps
                .Where(p => p.MOEN)
                .Select(p => p.MOPR ? $"{p.MONA} (Primary)" : p.MONA)
                .ToList();

            return monitorNames.Count == 0 ? "-" : string.Join(", ", monitorNames);
        }

        public static string FormatAudioDevice(T1SCRSH t1scrsh)
        {
            return FnString.IsNullEmptyOrWhitespace(t1scrsh.ADID) ? "(Unchanged)" : t1scrsh.ADNA;
        }
    }
}
