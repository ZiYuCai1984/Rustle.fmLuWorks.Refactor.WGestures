//This file contains the common Win32 API of the desktop Windows and the Windows CE/Mobile. 

//Created by Warren Tang on 8/8/2008

using System;
using System.Runtime.InteropServices;

namespace WGestures.Common.OsSpecific.Windows.Win32
{
    public static partial class Kernel32
    {
#if PocketPC
        private const string Kernel32Dll = "coredll.dll";
#else
        private const string Kernel32Dll = "kernel32.dll";
#endif

        #region GetSystemPowerStatusEx[2]

        /// <summary>
        ///     Wrapper over GetSystemPowerStatusEx
        /// </summary>
        /// <returns></returns>
        public static int GetBatteryLifePercent()
        {
            var status = new SYSTEM_POWER_STATUS_EX();

            byte percent = 0;
            if (GetSystemPowerStatusEx(status, false) == 1)
            {
                percent = status.BatteryLifePercent;
            }

            return percent;
        }

        /// <summary>
        ///     Wrapper over GetSystemPowerStatusEx2
        /// </summary>
        /// <returns></returns>
        public static int GetBatteryLifePercent2()
        {
            var status2 = new SYSTEM_POWER_STATUS_EX2();

            var percent = 0;
            if (GetSystemPowerStatusEx2(
                    status2,
                    (uint) Marshal.SizeOf(status2),
                    false)
                == (uint) Marshal.SizeOf(status2))
            {
                percent = status2.BackupBatteryLifePercent;
            }

            return percent;
        }

        [DllImport("coredll")]
        public static extern uint GetSystemPowerStatusEx(
            SYSTEM_POWER_STATUS_EX lpSystemPowerStatus,
            bool fUpdate);

        [DllImport("coredll")]
        public static extern uint GetSystemPowerStatusEx2(
            SYSTEM_POWER_STATUS_EX2 lpSystemPowerStatus,
            uint dwLen, bool fUpdate);


        public class SYSTEM_POWER_STATUS_EX2
        {
            public byte ACLineStatus;
            public byte BackupBatteryFlag;
            public uint BackupBatteryFullLifeTime;
            public byte BackupBatteryLifePercent;
            public uint BackupBatteryLifeTime;
            public uint BackupBatteryVoltage;
            public uint BatteryAverageCurrent;
            public uint BatteryAverageInterval;
            public byte BatteryChemistry;
            public uint BatteryCurrent;
            public byte BatteryFlag;
            public uint BatteryFullLifeTime;
            public byte BatteryLifePercent;
            public uint BatteryLifeTime;
            public uint BatterymAHourConsumed;
            public uint BatteryTemperature;
            public uint BatteryVoltage;
            public byte Reserved1;
            public byte Reserved2;
            public byte Reserved3;
        }

        public class SYSTEM_POWER_STATUS_EX
        {
            public byte ACLineStatus;
            public byte BackupBatteryFlag;
            public uint BackupBatteryFullLifeTime;
            public byte BackupBatteryLifePercent;
            public uint BackupBatteryLifeTime;
            public byte BatteryFlag;
            public uint BatteryFullLifeTime;
            public byte BatteryLifePercent;
            public uint BatteryLifeTime;
            public byte Reserved1;
            public byte Reserved2;
            public byte Reserved3;
        }

        #endregion

        #region SetPowerRequirement

        public enum CEDevicePowerState
        {
            Unspecified = -1,
            FullOn = 0,
            LowOn,
            StandBy,
            Sleep,
            Off,
            Maximum
        }


        public enum PowerDeviceFlags
        {
            POWER_FORCE = 0x1000,
            POWER_NAME = 0x0001
        }

        public enum PowerState
        {
            POWER_STATE_SUSPEND = 0x00200000
        }


        [DllImport(Kernel32Dll, SetLastError = true)]
        public static extern int SetSystemPowerState(
            string psState, PowerState StateFlags, PowerDeviceFlags Options); //POWER_FORCE

        [DllImport(Kernel32Dll, SetLastError = true)]
        public static extern IntPtr SetPowerRequirement(
            string pvDevice, CEDevicePowerState DeviceState, PowerDeviceFlags DeviceFlags,
            IntPtr pvSystemState, int StateFlags);

        [DllImport(Kernel32Dll, SetLastError = true)]
        public static extern uint ReleasePowerRequirement(IntPtr hPowerReq);

        [DllImport(Kernel32Dll, SetLastError = true)]
        public static extern int SetDevicePower(
            string pvDevice,
            PowerDeviceFlags dwDeviceFlags,
            CEDevicePowerState DeviceState);

        [DllImport(Kernel32Dll, SetLastError = true)]
        public static extern int GetDevicePower(
            string pvDevice,
            PowerDeviceFlags dwDeviceFlags,
            ref CEDevicePowerState DeviceState);

        [DllImport(Kernel32Dll, SetLastError = true)]
        public static extern int DevicePowerNotify(
            string device,
            CEDevicePowerState state,
            int flags);

        [DllImport(Kernel32Dll, CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion

        public static int GetLastError()
        {
            throw new Exception(
                "Never P/Invoke to GetLastError.  Call Marshal.GetLastWin32Error instead!");
        }
    }
}
