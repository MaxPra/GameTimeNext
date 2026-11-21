using System.Runtime.InteropServices;
using static GameTimeNext.Core.Framework.UserInput.KeyInput;

namespace GameTimeNext.Core.Framework.UserInput
{
    public class KeyPressTracker
    {
        private readonly Dictionary<VirtualKey, bool> keyStates = new();
        private readonly Dictionary<string, bool> combinationStates = new();

        public bool IsPressedOnce(VirtualKey key)
        {
            int keystate = SysWin32.GetAsyncKeyState((int)key);
            bool isCurrentlyPressed = (keystate & 0x8000) != 0;

            if (!keyStates.ContainsKey(key))
                keyStates[key] = false;

            if (isCurrentlyPressed && !keyStates[key])
            {
                keyStates[key] = true;
                return true;
            }

            if (!isCurrentlyPressed)
            {
                keyStates[key] = false;
            }

            return false;
        }

        public bool IsCombinationPressedOnce(params VirtualKey[] keys)
        {
            string combinationKey = string.Join("_", keys.OrderBy(k => (int)k));

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

            if (!combinationStates.ContainsKey(combinationKey))
                combinationStates[combinationKey] = false;

            if (allPressed && !combinationStates[combinationKey])
            {
                combinationStates[combinationKey] = true;
                return true;
            }

            if (!allPressed)
            {
                combinationStates[combinationKey] = false;
            }

            return false;
        }

        public VirtualKey GetPressedKeyOnce()
        {
            foreach (int keyCode in Enum.GetValues(typeof(VirtualKey)))
            {
                VirtualKey key = (VirtualKey)keyCode;
                int keystate = SysWin32.GetAsyncKeyState(keyCode);
                bool isCurrentlyPressed = (keystate & 0x8000) != 0;

                if (!keyStates.ContainsKey(key))
                    keyStates[key] = false;

                if (isCurrentlyPressed && !keyStates[key])
                {
                    keyStates[key] = true;
                    return key;
                }

                if (!isCurrentlyPressed)
                {
                    keyStates[key] = false;
                }
            }

            return VirtualKey.VK_NORESULT;
        }

        internal class SysWin32
        {
            [DllImport("user32.dll")]
            public static extern int GetAsyncKeyState(Int32 i);
        }
    }
}
