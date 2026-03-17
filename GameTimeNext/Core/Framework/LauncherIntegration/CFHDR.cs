using System.Runtime.InteropServices;

namespace GameTimeNext.Core.Framework.LauncherIntegration
{
    static class CFHDR
    {
        // --- Win32 ---
        [DllImport("user32.dll")]
        private static extern int GetDisplayConfigBufferSizes(
            uint flags, out uint numPathArrayElements, out uint numModeInfoArrayElements);

        [DllImport("user32.dll")]
        private static extern int QueryDisplayConfig(
            uint flags,
            ref uint numPathArrayElements, IntPtr pathArray,
            ref uint numModeInfoArrayElements, IntPtr modeInfoArray,
            IntPtr currentTopologyId); // IntPtr.Zero bei QDC_ONLY_ACTIVE_PATHS

        [DllImport("user32.dll")]
        private static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO request);

        [DllImport("user32.dll")]
        private static extern int DisplayConfigSetDeviceInfo(ref DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE request);

        // --- Consts ---
        private const uint QDC_ONLY_ACTIVE_PATHS = 0x00000002;
        private const uint DISPLAYCONFIG_PATH_ACTIVE = 0x00000001;

        // --- Enums / Header ---
        private enum DISPLAYCONFIG_DEVICE_INFO_TYPE
        {
            GET_SOURCE_NAME = 1,
            GET_TARGET_NAME = 2,
            GET_TARGET_PREFERRED_MODE = 3,
            GET_ADAPTER_NAME = 4,
            SET_TARGET_PERSISTENCE = 5,
            GET_TARGET_BASE_TYPE = 6,
            GET_SUPPORT_VIRTUAL_RESOLUTION = 7,
            SET_SUPPORT_VIRTUAL_RESOLUTION = 8,
            GET_ADVANCED_COLOR_INFO = 9,   // korrekt
            SET_ADVANCED_COLOR_STATE = 10, // korrekt
            GET_SDR_WHITE_LEVEL = 11
        }

        private enum DISPLAYCONFIG_COLOR_ENCODING
        {
            RGB = 0,
            YCBCR444 = 1,
            YCBCR422 = 2,
            YCBCR420 = 3,
            INTENSITY = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID { public uint LowPart; public int HighPart; }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_DEVICE_INFO_HEADER
        {
            public DISPLAYCONFIG_DEVICE_INFO_TYPE type;
            public uint size;
            public LUID adapterId;
            public uint id; // targetId
        }

        // --- QueryDisplayConfig structs (minimal) ---
        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_PATH_INFO
        {
            public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
            public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
            public uint flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_PATH_SOURCE_INFO
        {
            public LUID adapterId;
            public uint id;            // sourceId
            public uint modeInfoIdx;
            public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_PATH_TARGET_INFO
        {
            public LUID adapterId;
            public uint id;            // targetId
            public uint modeInfoIdx;
            public uint outputTechnology;
            public uint rotation;
            public uint scaling;
            public DISPLAYCONFIG_RATIONAL refreshRate;
            public uint scanLineOrdering;
            public int targetAvailable; // BOOL (int)
            public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_RATIONAL { public uint Numerator; public uint Denominator; }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_MODE_INFO
        {
            public uint infoType;
            public uint id;
            public LUID adapterId;
            public DISPLAYCONFIG_MODE_INFO_UNION modeInfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct DISPLAYCONFIG_MODE_INFO_UNION
        {
            [FieldOffset(0)] public DISPLAYCONFIG_TARGET_MODE targetMode;
            [FieldOffset(0)] public DISPLAYCONFIG_SOURCE_MODE sourceMode;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_TARGET_MODE
        {
            public DISPLAYCONFIG_VIDEO_SIGNAL_INFO targetVideoSignalInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINTL { public int x; public int y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_SOURCE_MODE
        {
            public uint width;
            public uint height;
            public uint pixelFormat;
            public POINTL position; // (x,y)
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_VIDEO_SIGNAL_INFO
        {
            public ulong pixelRate;
            public DISPLAYCONFIG_RATIONAL hSyncFreq;
            public DISPLAYCONFIG_RATIONAL vSyncFreq;
            public DISPLAYCONFIG_2DREGION activeSize;
            public DISPLAYCONFIG_2DREGION totalSize;
            public uint videoStandard;
            public uint scanLineOrdering;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_2DREGION { public uint cx; public uint cy; }

        // --- Advanced Color GET/SET (korrekte Layouts) ---
        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;

            // DWORD mit Bitflags (siehe Properties unten)
            public uint value;

            public DISPLAYCONFIG_COLOR_ENCODING colorEncoding; // DWORD
            public uint bitsPerColorChannel;                   // DWORD

            // Convenience-Properties auf den Bitflags
            public bool advancedColorSupported => (value & 0x1) != 0;
            public bool advancedColorEnabled => (value & 0x2) != 0;
            public bool wideColorEnforced => (value & 0x4) != 0;
            public bool advancedColorForceDisabled => (value & 0x8) != 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;

            // DWORD: Bit 0 = enableAdvancedColor
            public uint value;

            public static DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE Create(
                DISPLAYCONFIG_DEVICE_INFO_HEADER hdr, bool enable)
                => new DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE
                {
                    header = hdr,
                    value = enable ? 1u : 0u
                };
        }

        // --- Helper ---
        private static string HrStr(int code) =>
            code switch
            {
                0 => "ERROR_SUCCESS",
                122 => "ERROR_INSUFFICIENT_BUFFER",
                87 => "ERROR_INVALID_PARAMETER",
                31 => "ERROR_GEN_FAILURE",
                _ => "Win32 error " + code
            };

        // --- Public API ---
        public static bool SetHdrForAllActiveDisplays(bool enable)
        {
            int st = GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out var pathCount, out var modeCount);
            if (st != 0)
            {
                System.Diagnostics.Debug.WriteLine("GetDisplayConfigBufferSizes: " + HrStr(st));
                return false;
            }

            int pathSize = Marshal.SizeOf<DISPLAYCONFIG_PATH_INFO>();
            int modeSize = Marshal.SizeOf<DISPLAYCONFIG_MODE_INFO>();
            IntPtr pPaths = Marshal.AllocHGlobal((int)pathCount * pathSize);
            IntPtr pModes = Marshal.AllocHGlobal((int)modeCount * modeSize);

            try
            {
                st = QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref pathCount, pPaths,
                                        ref modeCount, pModes, IntPtr.Zero);
                if (st != 0)
                {
                    System.Diagnostics.Debug.WriteLine("QueryDisplayConfig: " + HrStr(st));
                    return false;
                }

                bool changed = false;

                for (int i = 0; i < pathCount; i++)
                {
                    var path = Marshal.PtrToStructure<DISPLAYCONFIG_PATH_INFO>(IntPtr.Add(pPaths, i * pathSize));
                    if ((path.flags & DISPLAYCONFIG_PATH_ACTIVE) == 0) continue;
                    if (path.targetInfo.targetAvailable == 0) continue;

                    var get = new DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO
                    {
                        header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
                        {
                            type = DISPLAYCONFIG_DEVICE_INFO_TYPE.GET_ADVANCED_COLOR_INFO,
                            size = (uint)Marshal.SizeOf<DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO>(),
                            adapterId = path.targetInfo.adapterId,
                            id = path.targetInfo.id
                        }
                    };

                    st = DisplayConfigGetDeviceInfo(ref get);
                    if (st != 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"GetAdvancedColorInfo (target {i}): {HrStr(st)}");
                        continue;
                    }

                    if (!get.advancedColorSupported)
                        continue; // kein HDR/Advanced Color

                    var hdr = new DISPLAYCONFIG_DEVICE_INFO_HEADER
                    {
                        type = DISPLAYCONFIG_DEVICE_INFO_TYPE.SET_ADVANCED_COLOR_STATE,
                        size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE>(),
                        adapterId = path.targetInfo.adapterId,
                        id = path.targetInfo.id
                    };
                    var set = DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE.Create(hdr, enable);

                    st = DisplayConfigSetDeviceInfo(ref set);
                    if (st == 0)
                        changed = true;
                    else
                        System.Diagnostics.Debug.WriteLine($"SetAdvancedColorState (target {i}): {HrStr(st)}");
                }

                return changed;
            }
            finally
            {
                Marshal.FreeHGlobal(pPaths);
                Marshal.FreeHGlobal(pModes);
            }
        }
    }

}
