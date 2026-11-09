using System.Runtime.InteropServices;
using System.Windows.Threading;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.UserInput
{
    /// <summary>
    /// Buttons of a game controller in Xbox layout. The directions combine the D-pad and the left stick.
    /// </summary>
    public enum GamepadButton
    {
        Up,
        Down,
        Left,
        Right,
        A,
        B,
        X,
        Y,
        Start,
        Back,
        LeftShoulder,
        RightShoulder,
        LeftTrigger,
        RightTrigger,

        /// <summary>Xbox button in the middle. Only readable through XInputGetStateEx (not on every system).</summary>
        Guide
    }

    /// <summary>
    /// Reads XInput controllers (Xbox and compatible) and PlayStation controllers (PlayStationControllers, over HID)
    /// and reports button presses on the dispatcher of the creating (UI) thread. A button reported by both (e.g. a
    /// PlayStation controller that Steam Input also emulates as an Xbox controller) counts once. Polls on its own thread: XInputGetState can block for a while (above all for empty slots), which
    /// made animations of the UI stutter. Held directions repeat like a held arrow key. Only polls between Start and Stop.
    /// </summary>
    public sealed class GamepadInput
    {
        private const int MaxControllers = 4;
        private const int ErrorSuccess = 0;

        // About half of the stick range, so a resting stick never counts as a direction
        private const short StickThreshold = 16000;

        // Triggers count as pressed from half way down
        private const byte TriggerThreshold = 128;

        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(16);
        private static readonly TimeSpan RepeatDelay = TimeSpan.FromMilliseconds(320);
        private static readonly TimeSpan RepeatInterval = TimeSpan.FromMilliseconds(90);

        // XInputGetState is slow for empty slots, so they are only checked from time to time
        private static readonly TimeSpan EmptySlotRetryInterval = TimeSpan.FromSeconds(2);

        private readonly Dispatcher _dispatcher;
        private readonly bool[] _connected = new bool[MaxControllers];
        private readonly DateTime[] _nextSlotCheck = new DateTime[MaxControllers];

        // Only used by the poll thread; opened while polling, closed when it ends
        private readonly UIXApplication _app;
        private readonly PlayStationControllers _playStationControllers;
        private bool _playStationAvailable = true;

        // Replaced (never changed) on every poll, so IsHeld can read it from the UI thread
        private volatile HashSet<GamepadButton> _previous = new HashSet<GamepadButton>();

        // Fields below are only used by the poll thread
        private GamepadButton? _heldDirection;
        private DateTime _nextRepeat;
        private bool _ignoreHeldButtons;

        private volatile bool _xinputAvailable = true;
        private volatile bool _isConnected;

        // Stops the running poll thread (Stop); a new one is started with a new source
        private CancellationTokenSource? _pollCancellation;

        // XInputGetStateEx (ordinal 100) also reports the Guide button; it isn't exported on every system
        private bool _stateExAvailable = true;

        /// <summary>A button was pressed (or a held direction repeats).</summary>
        public event Action<GamepadButton>? ButtonPressed;

        /// <summary>The first controller was connected or the last one disconnected.</summary>
        public event Action<bool>? ConnectionChanged;

        public bool IsConnected => _isConnected;

        /// <summary>
        /// True while the button is held, e.g. to recognize a combination in ButtonPressed.
        /// </summary>
        public bool IsHeld(GamepadButton button)
        {
            return _previous.Contains(button);
        }

        /// <param name="app">Application that reads the controller; source of the log entries.</param>
        public GamepadInput(UIXApplication app)
        {
            _app = app;
            _playStationControllers = new PlayStationControllers(app);
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        public void Start()
        {
            if (_pollCancellation != null)
                return;

            CancellationTokenSource cancellation = new CancellationTokenSource();
            _pollCancellation = cancellation;

            Thread thread = new Thread(() => PollLoop(cancellation.Token))
            {
                IsBackground = true,
                Name = nameof(GamepadInput)
            };

            thread.Start();
        }

        public void Stop()
        {
            _pollCancellation?.Cancel();
            _pollCancellation = null;
        }

        private void PollLoop(CancellationToken token)
        {
            Array.Clear(_nextSlotCheck);

            // Buttons that are already held (e.g. the one that opened the caller) don't count as a press
            _ignoreHeldButtons = true;
            _heldDirection = null;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    Poll(token);

                    if (token.WaitHandle.WaitOne(PollInterval))
                        break;
                }
            }
            finally
            {
                _playStationControllers.Dispose();
            }
        }

        private void Poll(CancellationToken token)
        {
            bool wasConnected = _isConnected;
            HashSet<GamepadButton> current = ReadButtons();

            if (token.IsCancellationRequested)
                return;

            _isConnected = _connected.Any(c => c) || (_playStationAvailable && _playStationControllers.IsConnected);

            if (wasConnected != _isConnected)
            {
                bool connected = _isConnected;
                _dispatcher.BeginInvoke(() => ConnectionChanged?.Invoke(connected), DispatcherPriority.Input);
            }

            if (_ignoreHeldButtons)
            {
                _ignoreHeldButtons = false;
                _previous = current;
                return;
            }

            DateTime now = DateTime.UtcNow;
            HashSet<GamepadButton> previous = _previous;
            _previous = current;

            foreach (GamepadButton button in current)
            {
                if (previous.Contains(button))
                    continue;

                if (IsDirection(button))
                {
                    _heldDirection = button;
                    _nextRepeat = now + RepeatDelay;
                }

                RaiseButtonPressed(button);
            }

            if (_heldDirection is not GamepadButton held)
                return;

            if (!current.Contains(held))
            {
                _heldDirection = null;
                return;
            }

            if (now < _nextRepeat)
                return;

            _nextRepeat = now + RepeatInterval;
            RaiseButtonPressed(held);
        }

        /// <summary>
        /// Raised on the UI thread without waiting for it: a handler may show something modal (nested dispatcher frame).
        /// </summary>
        private void RaiseButtonPressed(GamepadButton button)
        {
            _dispatcher.BeginInvoke(() => ButtonPressed?.Invoke(button), DispatcherPriority.Input);
        }

        /// <summary>
        /// Returns the pressed buttons of all connected controllers together.
        /// </summary>
        private HashSet<GamepadButton> ReadButtons()
        {
            HashSet<GamepadButton> buttons = new HashSet<GamepadButton>();

            if (_xinputAvailable)
                AddXInputButtons(buttons);

            if (_playStationAvailable)
            {
                try
                {
                    _playStationControllers.AddPressedButtons(buttons);
                }
                catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
                {
                    FnLog.AddWarning(_app, "HID is not available, PlayStation controllers are only read through XInput", ex);
                    _playStationAvailable = false;
                }
            }

            return buttons;
        }

        private void AddXInputButtons(HashSet<GamepadButton> buttons)
        {
            DateTime now = DateTime.UtcNow;

            for (int slot = 0; slot < MaxControllers; slot++)
            {
                if (!_connected[slot] && now < _nextSlotCheck[slot])
                    continue;

                XINPUT_STATE state;

                try
                {
                    if (GetState(slot, out state) != ErrorSuccess)
                    {
                        _connected[slot] = false;
                        _nextSlotCheck[slot] = now + EmptySlotRetryInterval;
                        continue;
                    }
                }
                catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
                {
                    FnLog.AddWarning(_app, "XInput is not available, only PlayStation controllers can be used", ex);
                    _xinputAvailable = false;
                    return;
                }

                _connected[slot] = true;
                AddPressedButtons(state.Gamepad, buttons);
            }
        }

        private static void AddPressedButtons(XINPUT_GAMEPAD gamepad, HashSet<GamepadButton> buttons)
        {
            ushort flags = gamepad.wButtons;

            if ((flags & XINPUT_GAMEPAD_DPAD_UP) != 0 || gamepad.sThumbLY > StickThreshold)
                buttons.Add(GamepadButton.Up);

            if ((flags & XINPUT_GAMEPAD_DPAD_DOWN) != 0 || gamepad.sThumbLY < -StickThreshold)
                buttons.Add(GamepadButton.Down);

            if ((flags & XINPUT_GAMEPAD_DPAD_LEFT) != 0 || gamepad.sThumbLX < -StickThreshold)
                buttons.Add(GamepadButton.Left);

            if ((flags & XINPUT_GAMEPAD_DPAD_RIGHT) != 0 || gamepad.sThumbLX > StickThreshold)
                buttons.Add(GamepadButton.Right);

            if ((flags & XINPUT_GAMEPAD_A) != 0)
                buttons.Add(GamepadButton.A);

            if ((flags & XINPUT_GAMEPAD_B) != 0)
                buttons.Add(GamepadButton.B);

            if ((flags & XINPUT_GAMEPAD_X) != 0)
                buttons.Add(GamepadButton.X);

            if ((flags & XINPUT_GAMEPAD_Y) != 0)
                buttons.Add(GamepadButton.Y);

            if ((flags & XINPUT_GAMEPAD_START) != 0)
                buttons.Add(GamepadButton.Start);

            if ((flags & XINPUT_GAMEPAD_BACK) != 0)
                buttons.Add(GamepadButton.Back);

            if ((flags & XINPUT_GAMEPAD_LEFT_SHOULDER) != 0)
                buttons.Add(GamepadButton.LeftShoulder);

            if ((flags & XINPUT_GAMEPAD_RIGHT_SHOULDER) != 0)
                buttons.Add(GamepadButton.RightShoulder);

            if ((flags & XINPUT_GAMEPAD_GUIDE) != 0)
                buttons.Add(GamepadButton.Guide);

            if (gamepad.bLeftTrigger >= TriggerThreshold)
                buttons.Add(GamepadButton.LeftTrigger);

            if (gamepad.bRightTrigger >= TriggerThreshold)
                buttons.Add(GamepadButton.RightTrigger);
        }

        private int GetState(int slot, out XINPUT_STATE state)
        {
            if (_stateExAvailable)
            {
                try
                {
                    return XInputGetStateEx(slot, out state);
                }
                catch (EntryPointNotFoundException)
                {
                    FnLog.AddInfo(_app, "XInputGetStateEx is not available, the Guide button can't be read");
                    _stateExAvailable = false;
                }
            }

            return XInputGetState(slot, out state);
        }

        private static bool IsDirection(GamepadButton button)
        {
            return button is GamepadButton.Up or GamepadButton.Down or GamepadButton.Left or GamepadButton.Right;
        }

        // --- XInput ---

        private const ushort XINPUT_GAMEPAD_DPAD_UP = 0x0001;
        private const ushort XINPUT_GAMEPAD_DPAD_DOWN = 0x0002;
        private const ushort XINPUT_GAMEPAD_DPAD_LEFT = 0x0004;
        private const ushort XINPUT_GAMEPAD_DPAD_RIGHT = 0x0008;
        private const ushort XINPUT_GAMEPAD_START = 0x0010;
        private const ushort XINPUT_GAMEPAD_BACK = 0x0020;
        private const ushort XINPUT_GAMEPAD_LEFT_SHOULDER = 0x0100;
        private const ushort XINPUT_GAMEPAD_RIGHT_SHOULDER = 0x0200;
        private const ushort XINPUT_GAMEPAD_GUIDE = 0x0400;
        private const ushort XINPUT_GAMEPAD_A = 0x1000;
        private const ushort XINPUT_GAMEPAD_B = 0x2000;
        private const ushort XINPUT_GAMEPAD_X = 0x4000;
        private const ushort XINPUT_GAMEPAD_Y = 0x8000;

        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_GAMEPAD
        {
            public ushort wButtons;
            public byte bLeftTrigger;
            public byte bRightTrigger;
            public short sThumbLX;
            public short sThumbLY;
            public short sThumbRX;
            public short sThumbRY;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_STATE
        {
            public uint dwPacketNumber;
            public XINPUT_GAMEPAD Gamepad;
        }

        [DllImport("xinput1_4.dll")]
        private static extern int XInputGetState(int dwUserIndex, out XINPUT_STATE pState);

        // Undocumented variant of XInputGetState that also sets the Guide button (same structure)
        [DllImport("xinput1_4.dll", EntryPoint = "#100")]
        private static extern int XInputGetStateEx(int dwUserIndex, out XINPUT_STATE pState);
    }
}
