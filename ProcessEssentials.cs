using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vet
{
    class ProcessEssentials
    {
        public Process[] return_ProcessesInList()
        {
            Process[] processes;

            try
            {
                processes = Process.GetProcesses();

                
                if (processes.Length <= 1)
                    return null;
                else
                    return processes;

            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
