using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Vet
{
    class ProcessEssentials
    {
        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }



        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll")]
        public static extern bool FreeLibrary(IntPtr hModule);
    



        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);


        
        public void tst(int pid)
        {
            IntPtr load_k32   = LoadLibrary(@"C:\Windows\System32\kernel32.dll");

            IntPtr openthread = GetProcAddress(load_k32, "OpenThread");
            IntPtr susthread  = GetProcAddress(load_k32, "SuspendThread");
            IntPtr resthread  = GetProcAddress(load_k32, "ResumeThread");


        }



        //---------------------------------------------------------------------------
        public
            void freeze_process(int process)
        {
            using (Process pid = Process.GetProcessById(process))
            {
                foreach(ProcessThread thread in pid.Threads)
                {
                    IntPtr open_thread = OpenThread((ThreadAccess)0x0002, false, (uint)thread.Id);

                    if (open_thread == IntPtr.Zero)
                        continue;
                    else
                    {
                        SuspendThread(open_thread);
                        CloseHandle(open_thread);
                    }
                }
            }          
        }


        //---------------------------------------------------------------------------
        public
            void continue_process(int process)
        {
            using (Process pid = Process.GetProcessById(process))
            {
                foreach (ProcessThread thread in pid.Threads)
                {
                    IntPtr open_thread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);

                    if (open_thread == IntPtr.Zero)
                        continue;
                    else
                    {
                        ResumeThread(open_thread);
                        CloseHandle(open_thread);
                    }
                }
            }
        }
    }
}
