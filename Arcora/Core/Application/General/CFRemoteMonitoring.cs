using Arcora.Core.Application.General.BackgroundProcesses;
using Arcora.Core.Framework;
using Arcora.Core.Framework.UI;
using Arcora.Core.Framework.Utils;
using QRCoder;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Windows.Media.Imaging;
using UIX.ViewController.Engine.Utils;

namespace Arcora.Core.Application.General
{
    /// <summary>
    /// Pairing and address of the remote monitoring: the QR code in the settings opens the web app on the phone and
    /// pairs it in one step (/pair?code=...).
    /// </summary>
    public static class CFRemoteMonitoring
    {
        // Port of the web app when arcora didn't start it itself (debug: started from Visual Studio, launchSettings.json)
        private const int DefaultWebAppPort = 5131;

        private static readonly object SyncRoot = new object();

        // The blackout running now was started by a monitoring from the phone (not by Ctrl+B or the monitoring key)
        private static bool _isRemoteBlackout = false;

        /// <summary>
        /// The pairing code, created and stored on first use. Read by the web app over the socket.
        /// </summary>
        public static string GetPairingCode()
        {
            lock (SyncRoot)
            {
                EnsurePairingCode();
                return AppEnvironment.GetAppConfig().RemoteMonitoringPairingCode;
            }
        }

        /// <summary>
        /// Every request of the web app is written to the log: always in debug, in the release when the setting
        /// "Extended remote monitoring logging" (tab Advanced) is on.
        /// </summary>
        public static bool IsExtendedLoggingEnabled()
        {
            return FnSystem.IsDebug() || AppEnvironment.GetAppConfig().AppSettings.ExtendedRemoteMonitoringLogging;
        }

        /// <summary>
        /// Replaces the pairing code: every paired phone has to scan the QR code again (e.g. after a lost phone). The
        /// web app is restarted, so pages already open on a phone end too instead of running on.
        /// </summary>
        public static void ResetPairingCode()
        {
            lock (SyncRoot)
            {
                AppEnvironment.GetAppConfig().RemoteMonitoringPairingCode = CreateCode();
                AppEnvironment.SaveAppConfig();
            }

            AppEnvironment.StartedBackgroundProcesses.Values.OfType<RemoteMonitoringProcess>().FirstOrDefault()?.RequestWebAppRestart();
        }

        /// <summary>
        /// Port the web app listens on: the one arcora started it with, the default port when the developer started it
        /// (debug), null while it isn't running (not started yet or being restarted).
        /// </summary>
        public static int? GetWebAppPort()
        {
            // Debug always uses the default port (RemoteMonitoringProcess doesn't search a free one there), also before
            // its first cycle has set the state
            if (FnSystem.IsDebug())
                return DefaultWebAppPort;

            RemoteMonitoringProcess? process = AppEnvironment.StartedBackgroundProcesses.Values.OfType<RemoteMonitoringProcess>().FirstOrDefault();
            return process?.BlazorStatus.State == BlazorProcessState.Running ? process.BlazorStatus.Port : null;
        }

        /// <summary>
        /// QR code and addresses for pairing a phone (settings, popup of the title bar): the QR code links to /pair with
        /// the pairing code on the first home network address, the text lists all addresses without the code. Without a
        /// running web app or a home network there is no QR code, the text says why. Port: the one just reported by
        /// RemoteMonitoringProcess, otherwise the current one.
        /// </summary>
        public static RemoteMonitoringPairing GetPairing(int? port = null)
        {
            string pairingCode = GetPairingCode();

            int? webAppPort = port ?? GetWebAppPort();
            if (webAppPort is null)
                return new RemoteMonitoringPairing(null, "The remote monitoring is starting. Open this again in a moment.", pairingCode);

            List<string> addresses = GetLanAddresses();
            if (addresses.Count == 0)
                return new RemoteMonitoringPairing(null, "No home network connection found. Connect the PC to your router (LAN or Wi-Fi) and try again.", pairingCode);

            return new RemoteMonitoringPairing(
                CreateQrCode(BuildPairingUrl(addresses[0], webAppPort.Value)),
                string.Join(Environment.NewLine, addresses.Select(address => string.Create(CultureInfo.InvariantCulture, $"http://{address}:{webAppPort}"))),
                pairingCode);
        }

        /// <summary>
        /// IPv4 addresses of the PC in the home network: active adapters with a default gateway and an address of a
        /// private range. Virtual adapters are not excluded on purpose (a Hyper-V external switch is the real LAN);
        /// VPNs (100.x), host-only switches (no gateway) and link-local addresses (169.254.x) drop out.
        /// </summary>
        public static List<string> GetLanAddresses()
        {
            List<string> addresses = new List<string>();

            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.OperationalStatus != OperationalStatus.Up || adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                IPInterfaceProperties properties = adapter.GetIPProperties();

                bool hasGateway = properties.GatewayAddresses.Any(gateway =>
                    gateway.Address.AddressFamily == AddressFamily.InterNetwork && !gateway.Address.Equals(IPAddress.Any));

                if (!hasGateway)
                    continue;

                foreach (UnicastIPAddressInformation unicast in properties.UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily == AddressFamily.InterNetwork && IsPrivate(unicast.Address))
                        addresses.Add(unicast.Address.ToString());
                }
            }

            return addresses.Distinct().ToList();
        }

        /// <summary>
        /// Blacks out all screens for a monitoring started from the phone (setting RemoteMonitoringBlackout), like the
        /// monitoring key does for a console game: plain black, cursor hidden. A blackout already running (e.g. Ctrl+B)
        /// is left as it is and not ended by the stop. UI thread.
        /// </summary>
        public static void BeginRemoteBlackout()
        {
            if (!AppEnvironment.GetAppConfig().AppSettings.RemoteMonitoringBlackout || CFBlackout.IsActive())
                return;

            CFBlackout.Enable(System.Windows.Application.Current.MainWindow, consoleMode: true);
            _isRemoteBlackout = true;
        }

        /// <summary>
        /// Ends the blackout of BeginRemoteBlackout. Called on every stop of a monitoring (phone, monitoring key, Big
        /// Picture, ...), so it never stays on; does nothing if the blackout came from somewhere else. UI thread.
        /// </summary>
        public static void EndRemoteBlackout()
        {
            if (!_isRemoteBlackout)
                return;

            _isRemoteBlackout = false;

            // The monitoring key may have ended it already (it toggles the blackout before it stops)
            if (CFBlackout.IsActive())
                CFBlackout.Disable(System.Windows.Application.Current.Dispatcher);
        }

        /// <summary>
        /// Link the QR code opens: web app on the address and pairing in one step.
        /// </summary>
        public static string BuildPairingUrl(string address, int port)
        {
            return string.Create(CultureInfo.InvariantCulture, $"http://{address}:{port}/pair?code={Uri.EscapeDataString(GetPairingCode())}");
        }

        /// <summary>
        /// QR code of the text as a frozen image (usable in any thread).
        /// </summary>
        public static BitmapImage CreateQrCode(string text)
        {
            using QRCodeGenerator generator = new QRCodeGenerator();
            using QRCodeData data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
            byte[] png = new PngByteQRCode(data).GetGraphic(8, darkColorRgba: [0, 0, 0, 255], lightColorRgba: [255, 255, 255, 255]);

            BitmapImage image = new BitmapImage();
            using (MemoryStream stream = new MemoryStream(png))
            {
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
            }

            image.Freeze();
            return image;
        }

        private static void EnsurePairingCode()
        {
            if (!FnString.IsNullEmptyOrWhitespace(AppEnvironment.GetAppConfig().RemoteMonitoringPairingCode))
                return;

            AppEnvironment.GetAppConfig().RemoteMonitoringPairingCode = CreateCode();
            AppEnvironment.SaveAppConfig();
        }

        /// <summary>
        /// 128 random bits, URL safe (base64url without padding).
        /// </summary>
        private static string CreateCode()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        /// <summary>
        /// Pairing for display: QR code (null if there is none), the addresses or the reason why there is no QR code, and
        /// the pairing code (also contained in the QR code).
        /// </summary>
        public sealed record RemoteMonitoringPairing(BitmapImage? QrCode, string Text, string PairingCode);

        private static bool IsPrivate(IPAddress address)
        {
            byte[] bytes = address.GetAddressBytes();

            return bytes[0] == 10
                || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                || (bytes[0] == 192 && bytes[1] == 168);
        }
    }
}
