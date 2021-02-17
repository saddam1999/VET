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
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);


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


        //---------------------------------------------------------------------------
        [DllImport("user32.dll")]       
        public static extern bool SetForegroundWindow(IntPtr handle);
        public 
            int bringWindowToFront(int id)
        {
            try
            {
                using (Process proc = Process.GetProcessById(id))
                {
                    IntPtr windowHandle = proc.MainWindowHandle;
                    SetForegroundWindow(windowHandle);

                    return 1;
                }
            }
            catch (Exception)
            {

                return 0;
            }

        }


        //---------------------------------------------------------------------------
        [DllImport("user32.dll")]        
        static extern bool ShowWindow(IntPtr handle, int cmd);
        public 
            int maximizeWindow(int id)
        {
            try
            {
                using (Process process = Process.GetProcessById(id))
                {
                    IntPtr windowHandle = process.MainWindowHandle;
                    ShowWindow(windowHandle, 3);

                    return 1;
                }

            }
            catch (Exception)
            {

                return 0;
            }

        }


        //---------------------------------------------------------------------------
        public
            int minimizeWindow(int id)
        {
            try
            {
                using (Process process = Process.GetProcessById(id))
                {
                    IntPtr windowHandle = process.MainWindowHandle;
                    ShowWindow(windowHandle, 2);

                    return 1;
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
}
