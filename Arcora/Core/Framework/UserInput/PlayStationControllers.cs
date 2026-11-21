using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Runtime.InteropServices;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.UserInput
{
    /// <summary>
    /// Reads DualShock 4 and DualSense controllers directly over HID (USB and Bluetooth). XInput only knows them through
    /// Steam Input or DS4Windows, and Steam Input feeds its virtual Xbox controller only while a Steam game is in front.
    /// Used by the poll thread of GamepadInput: each controller is read on its own (asynchronous reads), the poll
    /// takes the buttons of the last report. A controller opened exclusively by another program (e.g. DS4Windows with
    /// HidHide) can't be opened and is left to XInput.
    /// </summary>
    internal sealed class PlayStationControllers : IDisposable
    {
        // Plugged in / paired controllers are looked for this often
        private static readonly TimeSpan DeviceCheckInterval = TimeSpan.FromSeconds(2);

        // Source of the log entries
        private readonly UIXApplication _app;

        public PlayStationControllers(UIXApplication app)
        {
            _app = app;
        }

        private readonly Dictionary<string, Reader> _readers = new Dictionary<string, Reader>(StringComparer.OrdinalIgnoreCase);

        // Paths that couldn't be opened are not tried again on every check
        private readonly HashSet<string> _failedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private DateTime _nextDeviceCheck;

        /// <summary>
        /// True if at least one controller is open and has sent a report.
        /// </summary>
        public bool IsConnected => _readers.Values.Any(reader => reader.HasReport);

        /// <summary>
        /// Adds the pressed buttons of all controllers (Xbox layout: cross = A, circle = B, square = X, triangle = Y,
        /// Options = Start, Share / Create = Back, PS = Guide).
        /// </summary>
        public void AddPressedButtons(HashSet<GamepadButton> buttons)
        {
            RefreshDevices();

            foreach (Reader reader in _readers.Values)
                buttons.UnionWith(reader.Buttons);
        }

        public void Dispose()
        {
            foreach (Reader reader in _readers.Values)
                reader.Dispose();

            _readers.Clear();
            _failedPaths.Clear();
        }

        private void RefreshDevices()
        {
            foreach (string endedPath in _readers.Where(entry => entry.Value.IsEnded).Select(entry => entry.Key).ToList())
            {
                _readers[endedPath].Dispose();
                _readers.Remove(endedPath);
            }

            DateTime now = DateTime.UtcNow;

            if (now < _nextDeviceCheck)
                return;

            _nextDeviceCheck = now + DeviceCheckInterval;

            List<string> paths = FnGamepadDevices.GetPlayStationControllerPaths();

            // A disconnected device may come back under the same path
            _failedPaths.IntersectWith(paths);

            foreach (string path in paths)
            {
                if (_readers.ContainsKey(path) || _failedPaths.Contains(path))
                    continue;

                Reader? reader = Reader.Open(path);

                if (reader == null)
                {
                    _failedPaths.Add(path);
                    FnLog.AddInfo(_app, $"PlayStation controller can't be opened (in use exclusively by another program?), left to XInput: {path}");
                    continue;
                }

                _readers.Add(path, reader);
                FnLog.AddInfo(_app, $"PlayStation controller opened ({(reader.IsDualSense ? "DualSense" : "DualShock 4")}, {(reader.IsBluetooth ? "Bluetooth" : "USB")})");
            }
        }

        /// <summary>
        /// One opened controller: reads its input reports until it is disconnected or disposed.
        /// </summary>
        private sealed class Reader : IDisposable
        {
            // A stick counts as a direction beyond about half of its range (center 128)
            private const int StickThreshold = 64;
            private const byte TriggerThreshold = 128;

            private readonly FileStream _stream;
            private readonly int _reportLength;
            private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();

            // Replaced (never changed) on every report, read by the poll thread
            private volatile IReadOnlySet<GamepadButton> _buttons = new HashSet<GamepadButton>();
            private volatile bool _hasReport;
            private volatile bool _isEnded;

            public bool IsDualSense { get; }

            public bool IsBluetooth { get; }

            public IReadOnlySet<GamepadButton> Buttons => _buttons;

            public bool HasReport => _hasReport;

            public bool IsEnded => _isEnded;

            private Reader(FileStream stream, int reportLength, bool isDualSense, bool isBluetooth)
            {
                _stream = stream;
                _reportLength = reportLength;
                IsDualSense = isDualSense;
                IsBluetooth = isBluetooth;

                _ = ReadLoopAsync(_cancellation.Token);
            }

            public static Reader? Open(string path)
            {
                SafeFileHandle handle = CreateFile(path, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, IntPtr.Zero);

                if (handle.IsInvalid)
                {
                    handle.Dispose();
                    return null;
                }

                int reportLength = GetInputReportLength(handle);

                if (reportLength <= 0)
                {
                    handle.Dispose();
                    return null;
                }

                FileStream stream = new FileStream(handle, FileAccess.Read, 0, isAsync: true);

                return new Reader(stream, reportLength, FnGamepadDevices.IsDualSensePath(path), FnGamepadDevices.IsBluetoothPath(path));
            }

            public void Dispose()
            {
                _cancellation.Cancel();
                _stream.Dispose();
                _cancellation.Dispose();
            }

            private async Task ReadLoopAsync(CancellationToken token)
            {
                // Exactly one report per read: a larger buffer may get several queued reports at once
                byte[] report = new byte[_reportLength];

                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        int length = await _stream.ReadAsync(report, token).ConfigureAwait(false);

                        if (length == 0)
                            break;

                        if (ParseReport(report.AsSpan(0, length)) is HashSet<GamepadButton> buttons)
                        {
                            _buttons = buttons;
                            _hasReport = true;
                        }
                    }
                }
                catch (Exception ex) when (ex is OperationCanceledException or IOException or ObjectDisposedException)
                {
                    // Disconnected or disposed
                }
                finally
                {
                    _buttons = new HashSet<GamepadButton>();
                    _isEnded = true;
                }
            }

            /// <summary>
            /// Buttons of an input report, null for reports without buttons (e.g. of the Bluetooth setup).
            /// DualShock 4: 0x01 (USB, and Bluetooth until switched to full reports) and 0x11 (Bluetooth).
            /// DualSense: 0x01 (USB: full layout; Bluetooth: the short DualShock 4 layout) and 0x31 (Bluetooth).
            /// </summary>
            private HashSet<GamepadButton>? ParseReport(ReadOnlySpan<byte> report)
            {
                if (report.Length < 1)
                    return null;

                // Position of the left stick X; the DualSense layout puts the triggers and a counter before the buttons
                (int stick, bool isDualSenseLayout) = report[0] switch
                {
                    0x01 => (1, IsDualSense && !IsBluetooth),
                    0x11 when !IsDualSense => (3, false),
                    0x31 when IsDualSense => (2, true),
                    _ => (-1, false)
                };

                if (stick < 0)
                    return null;

                int buttonsIndex = stick + (isDualSenseLayout ? 7 : 4);
                int leftTriggerIndex = stick + (isDualSenseLayout ? 4 : 7);
                int rightTriggerIndex = leftTriggerIndex + 1;

                if (report.Length <= Math.Max(buttonsIndex + 2, rightTriggerIndex))
                    return null;

                HashSet<GamepadButton> buttons = new HashSet<GamepadButton>();

                byte faceAndDpad = report[buttonsIndex];
                byte shoulders = report[buttonsIndex + 1];
                byte system = report[buttonsIndex + 2];

                // D-pad as hat: 0 = up, clockwise in eighths, 8 = released
                int hat = faceAndDpad & 0x0F;

                if (hat is 7 or 0 or 1 || report[stick + 1] < 128 - StickThreshold)
                    buttons.Add(GamepadButton.Up);

                if (hat is 3 or 4 or 5 || report[stick + 1] > 128 + StickThreshold)
                    buttons.Add(GamepadButton.Down);

                if (hat is 5 or 6 or 7 || report[stick] < 128 - StickThreshold)
                    buttons.Add(GamepadButton.Left);

                if (hat is 1 or 2 or 3 || report[stick] > 128 + StickThreshold)
                    buttons.Add(GamepadButton.Right);

                AddIf(buttons, faceAndDpad, 0x10, GamepadButton.X);
                AddIf(buttons, faceAndDpad, 0x20, GamepadButton.A);
                AddIf(buttons, faceAndDpad, 0x40, GamepadButton.B);
                AddIf(buttons, faceAndDpad, 0x80, GamepadButton.Y);

                AddIf(buttons, shoulders, 0x01, GamepadButton.LeftShoulder);
                AddIf(buttons, shoulders, 0x02, GamepadButton.RightShoulder);
                AddIf(buttons, shoulders, 0x10, GamepadButton.Back);
                AddIf(buttons, shoulders, 0x20, GamepadButton.Start);

                AddIf(buttons, system, 0x01, GamepadButton.Guide);

                if (report[leftTriggerIndex] >= TriggerThreshold)
                    buttons.Add(GamepadButton.LeftTrigger);

                if (report[rightTriggerIndex] >= TriggerThreshold)
                    buttons.Add(GamepadButton.RightTrigger);

                return buttons;
            }

            private static void AddIf(HashSet<GamepadButton> buttons, byte flags, byte mask, GamepadButton button)
            {
                if ((flags & mask) != 0)
                    buttons.Add(button);
            }

            private static int GetInputReportLength(SafeFileHandle handle)
            {
                if (!HidD_GetPreparsedData(handle, out IntPtr preparsedData))
                    return 0;

                try
                {
                    return HidP_GetCaps(preparsedData, out HIDP_CAPS caps) == HIDP_STATUS_SUCCESS ? caps.InputReportByteLength : 0;
                }
                finally
                {
                    HidD_FreePreparsedData(preparsedData);
                }
            }

            // --- Win32 / HID ---

            private const uint GENERIC_READ = 0x80000000;
            private const uint FILE_SHARE_READ = 0x00000001;
            private const uint FILE_SHARE_WRITE = 0x00000002;
            private const uint OPEN_EXISTING = 3;
            private const uint FILE_FLAG_OVERLAPPED = 0x40000000;
            private const int HIDP_STATUS_SUCCESS = 0x00110000;

            [StructLayout(LayoutKind.Sequential)]
            private struct HIDP_CAPS
            {
                public ushort Usage;
                public ushort UsagePage;
                public ushort InputReportByteLength;
                public ushort OutputReportByteLength;
                public ushort FeatureReportByteLength;

                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
                public ushort[] Reserved;

                public ushort NumberLinkCollectionNodes;
                public ushort NumberInputButtonCaps;
                public ushort NumberInputValueCaps;
                public ushort NumberInputDataIndices;
                public ushort NumberOutputButtonCaps;
                public ushort NumberOutputValueCaps;
                public ushort NumberOutputDataIndices;
                public ushort NumberFeatureButtonCaps;
                public ushort NumberFeatureValueCaps;
                public ushort NumberFeatureDataIndices;
            }

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            private static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

            [DllImport("hid.dll")]
            private static extern bool HidD_GetPreparsedData(SafeFileHandle hidDeviceObject, out IntPtr preparsedData);

            [DllImport("hid.dll")]
            private static extern bool HidD_FreePreparsedData(IntPtr preparsedData);

            [DllImport("hid.dll")]
            private static extern int HidP_GetCaps(IntPtr preparsedData, out HIDP_CAPS capabilities);
        }
    }
}
