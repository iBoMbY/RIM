using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using OpenLibSys;

namespace RIM
{
    class Processor
    {
        [DllImport("kernel32.dll")]
        public static extern int GetCurrentThreadId();

        //[DllImport("kernel32.dll")]
        //public static extern int GetCurrentProcessorNumber();

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct GROUP_AFFINITY
        {
            public UIntPtr Mask;

            [MarshalAs(UnmanagedType.U2)]
            public ushort Group;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.U2)]
            public ushort[] Reserved;
        }

        [DllImport("kernel32", SetLastError = true)]
        private static extern Boolean SetThreadGroupAffinity(IntPtr hThread, ref GROUP_AFFINITY GroupAffinity, ref GROUP_AFFINITY PreviousGroupAffinity);

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESSORCORE
        {
            public byte Flags;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct NUMANODE
        {
            public uint NodeNumber;
        }

        public enum PROCESSOR_CACHE_TYPE
        {
            CacheUnified,
            CacheInstruction,
            CacheData,
            CacheTrace
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CACHE_DESCRIPTOR
        {
            public byte Level;
            public byte Associativity;
            public ushort LineSize;
            public uint Size;
            public PROCESSOR_CACHE_TYPE Type;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_UNION
        {
            [FieldOffset(0)]
            public PROCESSORCORE ProcessorCore;
            [FieldOffset(0)]
            public NUMANODE NumaNode;
            [FieldOffset(0)]
            public CACHE_DESCRIPTOR Cache;
            [FieldOffset(0)]
            private UInt64 Reserved1;
            [FieldOffset(8)]
            private UInt64 Reserved2;
        }

        public enum LOGICAL_PROCESSOR_RELATIONSHIP
        {
            RelationProcessorCore,
            RelationNumaNode,
            RelationCache,
            RelationProcessorPackage,
            RelationGroup,
            RelationAll = 0xffff
        }

        public struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION
        {
#pragma warning disable 0649
            public UIntPtr ProcessorMask;
            public LOGICAL_PROCESSOR_RELATIONSHIP Relationship;
            public SYSTEM_LOGICAL_PROCESSOR_INFORMATION_UNION ProcessorInformation;
#pragma warning restore 0649
        }

        [DllImport(@"kernel32.dll", SetLastError = true)]
        public static extern bool GetLogicalProcessorInformation(IntPtr Buffer, ref uint ReturnLength);

        private const int ERROR_INSUFFICIENT_BUFFER = 122;

        private static SYSTEM_LOGICAL_PROCESSOR_INFORMATION[] _logicalProcessorInformation = null;

        public static SYSTEM_LOGICAL_PROCESSOR_INFORMATION[] LogicalProcessorInformation
        {
            get
            {
                if (_logicalProcessorInformation != null)
                    return _logicalProcessorInformation;

                uint ReturnLength = 0;

                GetLogicalProcessorInformation(IntPtr.Zero, ref ReturnLength);

                if (Marshal.GetLastWin32Error() == ERROR_INSUFFICIENT_BUFFER)
                {
                    IntPtr Ptr = Marshal.AllocHGlobal((int)ReturnLength);
                    try
                    {
                        if (GetLogicalProcessorInformation(Ptr, ref ReturnLength))
                        {
                            int size = Marshal.SizeOf(typeof(SYSTEM_LOGICAL_PROCESSOR_INFORMATION));
                            int len = (int)ReturnLength / size;
                            _logicalProcessorInformation = new SYSTEM_LOGICAL_PROCESSOR_INFORMATION[len];
                            IntPtr Item = Ptr;

                            for (int i = 0; i < len; i++)
                            {
                                _logicalProcessorInformation[i] = (SYSTEM_LOGICAL_PROCESSOR_INFORMATION)Marshal.PtrToStructure(Item, typeof(SYSTEM_LOGICAL_PROCESSOR_INFORMATION));
                                Item += size;
                            }

                            return _logicalProcessorInformation;
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(Ptr);
                    }
                }
                return null;
            }
        }

        private static string Uint_to_string(uint value)
        {
            return Encoding.ASCII.GetString(BitConverter.GetBytes(value));
        }

        public static ProcessThread CurrentThread
        {
            get
            {
                return (from ProcessThread th 
                          in Process.GetCurrentProcess().Threads
                       where th.Id == GetCurrentThreadId()
                        select th).Single();
            }
        }

        private static byte _stepping, _model, _family, _type;
        private static string _vendor;
        private static int _logical_core_count;

        public static string Vendor
        {
            get { return _vendor; }
        }

        public static byte Family
        {
            get { return _family; }
        }

        public static byte Model
        {
            get { return _model; }
        }

        public static byte Type
        {
            get { return _type; }
        }

        public static int LogicalCoreCount
        {
            get { return _logical_core_count; }
        }

        static Processor()
        {
            uint eax = 0, ebx = 0, ecx = 0, edx = 0;

            Ols ols = new Ols();

            ols.Cpuid(0x0000000, ref eax, ref ebx, ref ecx, ref edx);

            _vendor = Uint_to_string(ebx) + Uint_to_string(edx) + Uint_to_string(ecx);

            ols.Cpuid(0x0000001, ref eax, ref ebx, ref ecx, ref edx);

            _stepping = (byte)((eax >> 0) & 0x0Fb);
            _model = (byte)((eax >> 4) & 0xF);
            _family = (byte)((eax >> 8) & 0xF);
            _type = (byte)((eax >> 12) & 0x3);
            byte _ext_model = (byte)((eax >> 16) & 0xF);
            byte _ext_family = (byte)((eax >> 20) & 0xFF);

            if (_family == 0xF)
            {
               _family += _ext_family;
               _model += (byte)(_ext_model << 4);
            }
            else if (_family == 0x6)
            {
                _model += (byte)(_ext_model << 4);
            }

            _logical_core_count = (byte)(ebx >> 16);
        }
    }
}
