using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace GameTimeNext.Core.Framework.UserInput
{
    /// <summary>
    /// Game controllers connected to the system, beyond what XInput reports.
    /// </summary>
    public static class FnGamepadDevices
    {
        private const int CR_SUCCESS = 0;
        private const uint CM_GET_DEVICE_INTERFACE_LIST_PRESENT = 0;

        private static readonly Guid HidInterfaceClass = new Guid("4D1E55B2-F16F-11CF-88CB-001111000030");

        // Sony vendor ID with the product IDs of DualShock 4 (two versions and the wireless adapter), DualSense and
        // DualSense Edge; USB paths contain "vid_054c&pid_0ce6", Bluetooth paths "vid&0002054c_pid&0ce6"
        private static readonly Regex PlayStationControllerPath = new Regex(
            "vid[_&](?:0002)?054c[_&]pid[_&](?:05c4|09cc|0ba0|0ce6|0df2)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex DualSensePath = new Regex(
            "pid[_&](?:0ce6|0df2)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        // Service class of Bluetooth HID devices in their device path
        private const string BluetoothHidServiceClass = "{00001124-0000-1000-8000-00805f9b34fb}";

        /// <summary>
        /// True if a PlayStation controller (DualShock 4, DualSense) is connected by USB or Bluetooth.
        /// </summary>
        public static bool IsPlayStationControllerPresent()
        {
            return GetPlayStationControllerPaths().Count > 0;
        }

        /// <summary>
        /// HID device paths of the connected PlayStation controllers (DualShock 4, DualSense), USB and Bluetooth.
        /// </summary>
        public static List<string> GetPlayStationControllerPaths()
        {
            Guid interfaceClass = HidInterfaceClass;

            if (CM_Get_Device_Interface_List_Size(out uint length, ref interfaceClass, null, CM_GET_DEVICE_INTERFACE_LIST_PRESENT) != CR_SUCCESS || length <= 1)
                return new List<string>();

            char[] buffer = new char[length];

            if (CM_Get_Device_Interface_List(ref interfaceClass, null, buffer, length, CM_GET_DEVICE_INTERFACE_LIST_PRESENT) != CR_SUCCESS)
                return new List<string>();

            // Device paths separated by '\0'
            return new string(buffer)
                .Split('\0', StringSplitOptions.RemoveEmptyEntries)
                .Where(path => PlayStationControllerPath.IsMatch(path))
                .ToList();
        }

        /// <summary>
        /// True for a DualSense / DualSense Edge path, false for a DualShock 4.
        /// </summary>
        public static bool IsDualSensePath(string devicePath)
        {
            return DualSensePath.IsMatch(devicePath);
        }

        /// <summary>
        /// True if the device is connected by Bluetooth (its input reports differ from USB).
        /// </summary>
        public static bool IsBluetoothPath(string devicePath)
        {
            return devicePath.Contains(BluetoothHidServiceClass, StringComparison.OrdinalIgnoreCase);
        }

        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
        private static extern int CM_Get_Device_Interface_List_Size(out uint pulLen, ref Guid interfaceClassGuid, string? pDeviceID, uint ulFlags);

        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
        private static extern int CM_Get_Device_Interface_List(ref Guid interfaceClassGuid, string? pDeviceID, char[] buffer, uint bufferLen, uint ulFlags);
    }
}
