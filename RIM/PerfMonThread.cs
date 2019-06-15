using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

using OpenLibSys;

namespace RIM
{
    class PerfMonThread
    {
        private static Object perfDataLock = new Object();

        protected static long[][] _perfData;

        public static long[][] PerfData
        {
            get
            {
                lock (perfDataLock)
                {
                    return _perfData;
                }
            }

            set
            {
                lock (perfDataLock)
                {
                    _perfData = value;
                }
            }
        }

        private static Object stopLock = new Object();

        protected static bool _stopWork = false;

        public static bool StopWork {
            get {
                lock (stopLock)
                {
                    return _stopWork;
                }
            }

            set
            {
                lock (stopLock)
                {
                    _stopWork = value;
                }
            }
        }

        protected Ols ols = new Ols();

        const long MAX_COUNT = 0x007F_FFFF_FFFF_FFFFL;

        const uint MSR_PERF_CTL_1 = 0xC0010200;
        const uint MSR_PERF_CTL_2 = 0xC0010202;
        const uint MSR_PERF_CTL_3 = 0xC0010204;
        const uint MSR_PERF_CTL_4 = 0xC0010206;
        const uint MSR_PERF_CTL_5 = 0xC0010208;
        const uint MSR_PERF_CTL_6 = 0xC001020A;

        const uint MSR_PERF_CTR_1 = 0xC0010201;
        const uint MSR_PERF_CTR_2 = 0xC0010203;
        const uint MSR_PERF_CTR_3 = 0xC0010205;
        const uint MSR_PERF_CTR_4 = 0xC0010207;
        const uint MSR_PERF_CTR_5 = 0xC0010209;
        const uint MSR_PERF_CTR_6 = 0xC001020B;

        const uint PERF_CTR_COUNT_NONE = 0x00000000;
        const uint PERF_CTR_COUNT_USER = 0x00010000;
        const uint PERF_CTR_COUNT_OS   = 0x00020000;
        const uint PERF_CTR_COUNT_ALL  = 0x00030000;

        const uint PERF_CTR_ENABLE_EVENT = 0x00400000;

        const uint PERF_CTR_LS_NONE = 0x00000000;
        const uint PERF_CTR_LS_LDST = 0x00000400;
        const uint PERF_CTR_LS_ST = 0x00000200;
        const uint PERF_CTR_LS_LD = 0x00000100;
        const uint PERF_CTR_LS_ALL = 0x00000700;

        const uint PERF_CTR_FP_NONE = 0x00000000;
        const uint PERF_CTR_FP_ALL  = 0x00000500;

        const uint PERF_CTR_FUSED_NONE = 0x00000000;
        const uint PERF_CTR_FUSED_ALL  = 0x0000FF00;

        const uint PERF_CTR_EVENT_LS_DISPATCH = 0x00000029;
        const uint PERF_CTR_EVENT_RETIRED_INSTRUCTIONS = 0x000000C0;
        const uint PERF_CTR_EVENT_RETIRED_UOPS = 0x000000C1;
        const uint PERF_CTR_EVENT_RETIRED_BRANCH = 0x000000C2;

        const uint PERF_CTR_EVENT_RETIRED_FP     = 0x000000C0;
        const uint PERF_CTR_EVENT_RETIRED_FUSED  = 0x00000003;

        const uint PERF_CTR_RESERVED = 0x00000000;

        const uint ZERO = 0x00000000;

        const uint COUNTER_COUNT = 5;

        public static void Init(int core_count)
        {
            _perfData = new long[core_count][];

            for (uint i = 0; i < core_count ; i++)
            {
                _perfData[i] = new long[COUNTER_COUNT];
            }

            for (int i = 0; i < core_count; i++)
            {
                PerfMonThread worker = new PerfMonThread();

                Thread thread = new Thread(worker.DoWork);

                thread.Priority = ThreadPriority.Highest;
                
                thread.Start(i);
            }
        }

        protected void InitCounter(uint id, uint eax)
        {
            uint index = MSR_PERF_CTL_1 + (id * 2);

            ols.Wrmsr(index, eax, ZERO);
            ols.Wrmsr(MSR_PERF_CTR_1 + (id * 2), eax, ZERO);

            ols.Wrmsr(index, eax, ZERO);
        }

        protected void ClearCounter(uint id)
        {
            ols.Wrmsr(MSR_PERF_CTL_1 + (id * 2), ZERO, ZERO);
            ols.Wrmsr(MSR_PERF_CTR_1 + (id * 2), ZERO, ZERO);
        }

        protected void RestCounter(uint id)
        {
            uint index = MSR_PERF_CTL_1 + (id * 2);
            uint eax = 0, edx = 0;

            ols.Rdmsr(index, ref eax, ref edx);
            ols.Wrmsr(index, ZERO, ZERO);
            ols.Wrmsr(MSR_PERF_CTR_1 + (id * 2), ZERO, ZERO);
            ols.Wrmsr(index, eax, edx);
        }

        protected long ReadCounter(uint id)
        {
            uint eax = 0, edx = 0;

            ols.Rdmsr(MSR_PERF_CTR_1 + (id * 2), ref eax, ref edx);

            return ((long)edx << 32) | eax;
        }

        public void DoWork(object id)
        {
            Thread.BeginThreadAffinity();

            int threadID = (int)id;

            Processor.CurrentThread.ProcessorAffinity = new IntPtr((int)1<<threadID);
            
            long counter;

            long[] buffer = new long[COUNTER_COUNT];
            long[] data = new long[COUNTER_COUNT];

            try
            {
                InitCounter(0, PERF_CTR_EVENT_RETIRED_UOPS   | PERF_CTR_RESERVED  | PERF_CTR_COUNT_ALL | PERF_CTR_ENABLE_EVENT);
                InitCounter(1, PERF_CTR_EVENT_RETIRED_BRANCH | PERF_CTR_RESERVED  | PERF_CTR_COUNT_ALL | PERF_CTR_ENABLE_EVENT);
                InitCounter(2, PERF_CTR_EVENT_LS_DISPATCH    | PERF_CTR_LS_ALL    | PERF_CTR_COUNT_ALL | PERF_CTR_ENABLE_EVENT);
                InitCounter(3, PERF_CTR_EVENT_RETIRED_FP     | PERF_CTR_FP_ALL    | PERF_CTR_COUNT_ALL | PERF_CTR_ENABLE_EVENT);
                InitCounter(4, PERF_CTR_EVENT_RETIRED_FUSED  | PERF_CTR_FUSED_ALL | PERF_CTR_COUNT_ALL | PERF_CTR_ENABLE_EVENT);

                do
                {
                    Thread.Sleep(1000);

                    for (uint i = 0; i < COUNTER_COUNT; i++)
                    {
                        counter = ReadCounter(i);

                        data[i] = counter - buffer[i];
                        
                        if (counter > MAX_COUNT)
                        {
                            RestCounter(i);
                            buffer[i] = 0;
                        }
                        else
                        {
                            buffer[i] = counter;
                        }
                    }

                    PerfData[threadID] = data;
                }
                while (!StopWork);
            }
            finally
            {
                for (uint i = 0; i < COUNTER_COUNT; i++)
                {
                    ClearCounter(i);
                }

                Thread.EndThreadAffinity();
            }
        }
    }
}
