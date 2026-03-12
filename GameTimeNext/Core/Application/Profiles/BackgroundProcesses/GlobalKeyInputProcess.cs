using GameTimeNext.Core.Framework.UserInput;
using System.Runtime.InteropServices;
using UIX.ViewController.Engine.Runnables;
using static GameTimeNext.Core.Framework.UserInput.KeyInput;

namespace GameTimeNext.Core.Application.Profiles.BackgroundProcesses
{
    public class GlobalKeyInputProcess : UIXBackgroundProcess
    {

        #region private attributes
        private static int KEY_DOWN_EVENT = 0x1;
        private static int KEY_UP_EVENT = 0x2;

        private bool key_was_pressed = false;
        private bool key_was_released = false;
        #endregion

        #region public attributes
        public string StartingType { get; set; } = StartingTypes.Normal;
        public Action<VirtualKey>? AfterKeyPressed { get; set; }
        #endregion

        public override void Logic()
        {
            switch (StartingType)
            {
                case StartingTypes.Normal:
                    processNormal();
                    break;

                case StartingTypes.MonitorKey:
                    processMonitorKey();
                    break;
            }
        }
        private void processNormal()
        {
            throw new NotImplementedException();
        }

        private void processMonitorKey()
        {
            VirtualKey pressedKey = CheckAllKeysOnKeyboard();

            if (pressedKey == VirtualKey.VK_NONE || pressedKey == VirtualKey.VK_NORESULT)
                return;

            if (AfterKeyPressed == null)
                return;

            AfterKeyPressed?.Invoke(pressedKey);
        }

        protected override void InitializeInfos()
        {
        }

        #region Check Key Methods
        /// <summary>
        /// Prüft alle Tasten auf der Tastatur und gibt die gedrückte zurück
        /// </summary>
        /// <returns></returns>
        private VirtualKey CheckAllKeysOnKeyboard()
        {
            foreach (int key in Enum.GetValues(typeof(KeyInput.VirtualKey)))
            {
                int keystate = SysWin32.GetAsyncKeyState(key);

                // Prüfen, ob Taste gedrückt
                if ((keystate & 0x8000) != 0)
                {
                    return ParseKeyEnum(key);
                }

            }

            return VirtualKey.VK_NORESULT;
        }

        /// <summary>
        /// Prüft nur eine spezifische Taste auf der Tastatur und gibt zurück, ob sie gedrückt wurde
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private bool CheckForSpecificKeyOnKeyboard(VirtualKey key)
        {
            int keystate = SysWin32.GetAsyncKeyState((int)key);

            // Prüfen, ob Taste gedrückt
            if ((keystate & 0x8000) != 0)
            {

                if (!key_was_pressed)
                {
                    key_was_pressed = true;
                    return true;
                }
            }
            else
            {
                if (key_was_pressed && !key_was_released)
                {
                    key_was_released = true;
                }
            }

            if ((keystate & 0x8000) == 0 && key_was_pressed)
            {
                key_was_pressed = false;
                key_was_released = false;
            }

            return false;
        }

        /// <summary>
        /// Prüft, ob eine Tastenkombination gedrückt wurde (z. B. STRG + B).
        /// </summary>
        private bool CheckForKeyCombination(params VirtualKey[] keys)
        {
            bool allPressed = true;

            foreach (var key in keys)
            {
                int keystate = SysWin32.GetAsyncKeyState((int)key);

                if ((keystate & 0x8000) == 0)
                {
                    allPressed = false;
                    break;
                }
            }

            if (allPressed && !key_was_pressed)
            {
                key_was_pressed = true;
                return true;
            }

            if (!allPressed && key_was_pressed)
            {
                key_was_pressed = false;
            }

            return false;
        }


        private VirtualKey ParseKeyEnum(int keyCode)
        {
            return (VirtualKey)Enum.ToObject(typeof(VirtualKey), keyCode);
        }

        public static VirtualKey GetVirtualKeyByValue(Dictionary<VirtualKey, string> list, string value)
        {
            return list.FirstOrDefault(x => x.Value == value).Key;
        }

        public enum StartType
        {
            MONITORE_KEY,
            GAME_MONITORING,
            BLACKOUT_SCREEN
        }
        #endregion

    }

    public class StartingTypes
    {
        public const string Normal = "GKIP.Normal";
        public const string MonitorKey = "GKIP.MonitorKey";
    }

    internal class SysWin32
    {
        [DllImport("user32.dll")]
        public static extern int GetAsyncKeyState(Int32 i);
    }
}
