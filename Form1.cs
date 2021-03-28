using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Vet
{


    struct PROCESS_STARTED_EVENT_VAR    //  These variable are being called evertime a process starts
                                        //  so they we be placed in a struct for better memory managment.
    {
        public string p_name;
        public string pid;
        public string owner;
        public string file_name;

        public ListViewItem item;
    }



    
    public partial class main_form : Form
    {
        public main_form()
        {
            InitializeComponent();
        }

        ProcessEssentials process_essentials = new ProcessEssentials();



        


        void displaySystemUsage()
        {
            while (true)
            {
                try
                {

                    using (PerformanceCounter system_cpu_usage = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
                    {
                        var first = system_cpu_usage.NextValue();
                        Thread.Sleep(250);

                        change_via_thread.ControlInvoke(processes_listview, () => toolstrip_cpu_label.Text = "CPU: " + system_cpu_usage.NextValue().ToString().Split('.')[0] + "%");

                    }

                }              
                catch (Exception)
                {
                    change_via_thread.ControlInvoke(processes_listview, () => toolstrip_cpu_label.Text = "n/a");
                }
                try
                {
                    using (PerformanceCounter system_mem_usage = new PerformanceCounter("Memory", "% Committed Bytes In Use", null))
                    {
                        var first = system_mem_usage.NextValue();
                        Thread.Sleep(250);

                        change_via_thread.ControlInvoke(processes_listview, () => toolstrip_memory_label.Text = "Memory: " + system_mem_usage.NextValue().ToString().Split('.')[0] + "%");

                    }
                }
                catch (Exception)
                {
                    change_via_thread.ControlInvoke(processes_listview, () => toolstrip_memory_label.Text = "n/a");
                }
                try
                {
                    using (PerformanceCounter system_disk_usage = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total"))
                    {
                        var first = system_disk_usage.NextValue();
                        Thread.Sleep(250);

                        change_via_thread.ControlInvoke(processes_listview, () => toolstrip_diskusage_label.Text = "Disk: " + system_disk_usage.NextValue().ToString().Split('.')[0] + "%");

                    }
                }
                catch (Exception)
                {
                    change_via_thread.ControlInvoke(processes_listview, () => toolstrip_diskusage_label.Text = "n/a");
                }
            }          
        }


        //---------------------------------------------------------------------------     
            void remove_dead_pids()
        {
            while (true)
            {
                try
                {
                    foreach (ListViewItem item in processes_listview.Items)
                    {
                        bool found = Process.GetProcesses().Any(x => x.Id == Int32.Parse(item.Text));

                        if (!found)
                        {

                            change_via_thread.ControlInvoke(processes_listview, () => item.Remove());

                        }
                        Thread.Sleep(30);   //  Reducing cpu usage
                    }
                }
                catch (Exception)
                {
                    continue;
                }

                Thread.Sleep(5000);        //          ^
            }           
        }


        //---------------------------------------------------------------------------
        void 
            update_vetted_processList()
        {
            while (true)
            {

                change_via_thread.ControlInvoke(processes_listview, () => toolstrip_processes_count.Text = "Processes: "+processes_listview.Items.Count.ToString()); ;
                Thread.Sleep(600);

            }
        }


        //---------------------------------------------------------------------------
        private 
            void thread_setup_form_on_startup(int pid)
        {
            

            ListViewItem item;

            string[] data = new string[5];          
            string   owner;
            string   file_name;
            



            using (Process process = Process.GetProcessById(pid))
            {
                

                data[0] = process.Id.ToString();
                

                try
                {
                    file_name = process.MainModule.FileName;
                    owner     = File.GetAccessControl(file_name).GetOwner(typeof(System.Security.Principal.NTAccount)).ToString().Split('\\')[1];

                    data[2] = owner;

                   
                }
                catch (Win32Exception)
                {
                    data[2] = "Access Denied";
                }
                catch (Exception)
                {
                    data[2] = "<Unknown, Error>";
                }



                



                data[1] = process.ProcessName + ".exe";



                try
                {
                    data[3] = process.MainModule.FileVersionInfo.CompanyName;
                }
                catch (Win32Exception)
                {
                    data[3] = "Access Denied";
                }
                catch (Exception)
                {
                    data[3] = "<Unknown, Error>";
                }


                try
                {
                    data[4] = process.MainModule.FileVersionInfo.FileVersion;
                }
                catch (Win32Exception)
                {
                    data[4] = "Access Denied";
                }
                catch (Exception)
                {
                    data[4] = "<Unknown, Error>";
                }


                item = new ListViewItem(data);

                

            }

            try
            {

                change_via_thread.ControlInvoke(processes_listview, () => processes_listview.BeginUpdate());
                change_via_thread.ControlInvoke(processes_listview, () => processes_listview.Items.Add(item));
                change_via_thread.ControlInvoke(processes_listview, () => processes_listview.EndUpdate());

            }
            catch (Exception)
            {
                return;
            }
        }


        //---------------------------------------------------------------------------
        void
            setup_form_on_startup()
        {
            Process[] processes = Process.GetProcesses();


            foreach (Process process in processes)
            {


                Thread thread = new Thread(() => thread_setup_form_on_startup(process.Id));
                thread.Start();


            }
        }


        //---------------------------------------------------------------------------
        private void main_form_Load(object sender, EventArgs e)
        {



            processes_listview.View = View.Details;
            processes_listview.ListViewItemSorter = null;
            processes_listview.Columns.Add("ID", 90);
            processes_listview.Columns.Add("NAME", 170);
            processes_listview.Columns.Add("OWNER", 120);
            processes_listview.Columns.Add("COMPANY", 220);
            processes_listview.Columns.Add("FILE VERSION", 120);



            using (Task build_start_process = new Task(setup_form_on_startup))
            {
                build_start_process.Start();
                build_start_process.Wait();
            }

           
                

            Task task = new Task(update_vetted_processList);
            task.Start();

            Task update_system_cpu_usage = new Task(new Action(displaySystemUsage));
            update_system_cpu_usage.Start();

            Task remove_old_pids = new Task( () => remove_dead_pids());
            remove_old_pids.Start();

            Task add_new_spawned_process = new Task(start_AddNewprocess);
            add_new_spawned_process.Start();

            Task remove_old_processes = new Task(start_RemoveOldProcess);
            remove_old_processes.Start();




            Task clear_pages_task = new Task(process_essentials.clear_footprints);
            clear_pages_task.Start();
        }


        //---------------------------------------------------------------------------
        void start_AddNewprocess()
        {
            ManagementEventWatcher processStartEvent = new ManagementEventWatcher("SELECT * FROM Win32_ProcessStartTrace");
            
            processStartEvent.EventArrived += new EventArrivedEventHandler(processStartEvent_EventArrived);
            processStartEvent.Start();
        }


        //---------------------------------------------------------------------------
        void start_RemoveOldProcess()
        {            
            ManagementEventWatcher processStopEvent = new ManagementEventWatcher("SELECT * FROM Win32_ProcessStopTrace");

            processStopEvent.EventArrived += new EventArrivedEventHandler(processStopEvent_EventArrived);
            processStopEvent.Start();
        }


        //---------------------------------------------------------------------------


        void processStartEvent_EventArrived(object sender, EventArrivedEventArgs e)
        {


            
            PROCESS_STARTED_EVENT_VAR variable_essentials = new PROCESS_STARTED_EVENT_VAR();



            bool     found         = false;
            bool     is_suspending = toolstrip_watchdog_suspendonstart.Checked;
            string[] data          = new string[5];
            
            

            variable_essentials.pid = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value).ToString();
            variable_essentials.p_name = e.NewEvent.Properties["ProcessName"].Value.ToString();


            if (string.IsNullOrEmpty(variable_essentials.pid))
            

                return;         //  Don't even attempt listing the process.


            else
            {


                found = true;


            }


            


            if (found)
            {
                

                foreach (var blocked_item in blockeprocesses_lb.Items)
                {

                    

                    if (variable_essentials.p_name == blocked_item.ToString())
                    {


                        DateTime starttime = DateTime.Now ;
                        DateTime killtime;


                        using (Process process = Process.GetProcessById(Int32.Parse(variable_essentials.pid)))
                        {
                            

                            if (is_suspending)
                            {

                                
                                process_essentials.freeze_process(process.Id); killtime = DateTime.Now;
                                watchdog_logger.Items.Add("[" + killtime + "]" + "  " + variable_essentials.p_name + " (" + variable_essentials.pid + ")" + " Suspended!");


                            }
                            else
                            {
                                

                                process.Kill(); killtime = DateTime.Now;
                                watchdog_logger.Items.Add("[" + killtime + "]" + "  " + variable_essentials.p_name + " (" + variable_essentials.pid + ")" + " Terminated!");


                            }

                            
                        }

                        
                        if(!is_suspending)
                              return;        //  If process is part of watchdog it will either be getting suspend or terminated on start.
                                             //
                                             //  If the suspend option is FALSE then we just return the thread because there will be no 
                                             //  point in using processor juice filling out the listview with process info when it will just
                                             //  be remove in seconds.
                                             //
                                             //  If the suspend option is TRUE we continue on with displaying the process in listview as it will
                                             //  still be consuming memory on the machine, and not become a "ghost process" just sitting in the 
                                             //  background not doing anything ... This way the user can still at the least kill the process.

                        
                    }
                }


                /* IF PROCESS IS NOT IN WATCHDOG or is being SUSPENDED ...*/


                data[1] = variable_essentials.p_name;
                data[0] = variable_essentials.pid;


                try
                {


                    using (Process process = Process.GetProcessById(Int32.Parse(variable_essentials.pid)))
                    {

                        

                        try
                        {
                            variable_essentials.file_name = process.MainModule.FileName;
                            variable_essentials.owner = File.GetAccessControl(variable_essentials.file_name).GetOwner(typeof(System.Security.Principal.NTAccount)).ToString().Split('\\')[1];
                            data[2]   = variable_essentials.owner;
                        }
                        catch (Win32Exception)
                        {
                            data[2] = "Access Denied";
                        }
                        catch (Exception)
                        {
                            data[2] = "<Unknown, Error>";     //  System processes that require trust certs.
                        }

                        try
                        {
                            data[3] = process.MainModule.FileVersionInfo.CompanyName;
                        }
                        catch (Win32Exception)
                        {
                            data[3] = "Access Denied";
                        }
                        catch (Exception)
                        {
                            data[3] = "<Unknown, Error>";
                        }

                        try
                        {
                            data[4] = process.MainModule.FileVersionInfo.FileVersion;
                        }
                        catch (Win32Exception)
                        {
                            data[4] = "Access Denied";
                        }
                        catch (Exception)
                        {
                            data[4] = "<Unknown, Error>";
                        }
                    }


                }
                catch (ArgumentException)
                {
                    return;                            //  Usually if process has exited before we build to listview
                }


            }


            variable_essentials.item = new ListViewItem(data);


            try
            {
               
                change_via_thread.ControlInvoke(processes_listview, () => processes_listview.BeginUpdate());        
                change_via_thread.ControlInvoke(processes_listview, () => processes_listview.Items.Add(variable_essentials.item));
                change_via_thread.ControlInvoke(processes_listview, () => processes_listview.EndUpdate());          

            }
            catch (Exception)
            {
                //
            }
        }
        


        //---------------------------------------------------------------------------
        void 
            processStopEvent_EventArrived(object sender, EventArrivedEventArgs e)
        {
            string pid = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value).ToString();

            foreach(ListViewItem item in processes_listview.Items)
            {
                if(item.Text == pid)
                {
                    
                    item.Remove();
                    break;

                }
            }
        }

              
        //---------------------------------------------------------------------------
        private
            void main_form_FormClosing(object sender, FormClosingEventArgs e)
        {

            Process process = Process.GetCurrentProcess();
            process.Kill();

        }


        //---------------------------------------------------------------------------
        private
            void processes_listview_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {

                var focusedItem = processes_listview.FocusedItem;

                if (focusedItem != null && focusedItem.Bounds.Contains(e.Location))
                {
                    rightclick_options_menu.Show(Cursor.Position);
                }

            }
        }


        //---------------------------------------------------------------------------
        private
            void rcm_open_file_location_Click(object sender, EventArgs e)
        {
            ListViewItem item = processes_listview.SelectedItems[0];

            string program_name = processes_listview.SelectedItems[0].SubItems[1].Text;
            string path;

            using (Process pid = Process.GetProcessById(Int32.Parse(item.Text)))
            {
                try
                {
                    path = "/select, \"" + pid.MainModule.FileName.Substring(0, pid.MainModule.FileName.LastIndexOf(program_name)) + "\"";
                    Process.Start("explorer.exe", "-p" + path);
                }
                catch (UnauthorizedAccessException)
                {
                    //
                }               
            } 
        }


        //---------------------------------------------------------------------------
        private 
            void rcm_kill_process_Click(object sender, EventArgs e)
        {
            ListViewItem item  = processes_listview.SelectedItems[0];

            using (Process pid = Process.GetProcessById(Int32.Parse(item.Text)))
            {
                try
                {
                    pid.Kill();
                }
                catch (UnauthorizedAccessException)
                {
                    //
                }
            }
        }


        //---------------------------------------------------------------------------
        private
            void rcm_suspend_process_Click(object sender, EventArgs e)
        {
            ListViewItem item = processes_listview.SelectedItems[0];

            Task suspend_process_task = new Task(() => process_essentials.freeze_process(Int32.Parse(item.Text)));

            suspend_process_task.Start();
            suspend_process_task.Wait();
            suspend_process_task.Dispose();

        }


        //---------------------------------------------------------------------------
        private 
            void rcm_resume_process_Click(object sender, EventArgs e)
        {
            ListViewItem item = processes_listview.SelectedItems[0];

            Task resume_process_task = new Task(() => process_essentials.continue_process(Int32.Parse(item.Text)));

            resume_process_task.Start();
            resume_process_task.Wait();
            resume_process_task.Dispose();
            
        }


        //---------------------------------------------------------------------------
        private 
            void rcm_process_restart_Click(object sender, EventArgs e)
        {
            ListViewItem item = processes_listview.SelectedItems[0];
            int process_id    = Int32.Parse(item.Text);
            string path;

            using (Process pid = Process.GetProcessById(process_id))
            {
                try
                {
                    pid.Kill();
                }
                catch (UnauthorizedAccessException)
                {

                    return;     //  If we don't have permission to kill, then most likely won't have permission
                                //  for catching mainmodule.
                }
            }

            
            using (Process pid = Process.GetProcessById(process_id))
            {
                try
                {
                    path = pid.MainModule.FileName;

                    if (string.IsNullOrEmpty(path))
                    {
                        return;
                    }
                    else
                    {
                        Process.Start(path);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    //
                }     
            }
        }


        //---------------------------------------------------------------------------
        private
            void rcm_bring_window_to_front_Click(object sender, EventArgs e)
        {
            ListViewItem item = processes_listview.SelectedItems[0];
            int process_id = Int32.Parse(item.Text);

            process_essentials.bringWindowToFront(process_id);
        }


        //---------------------------------------------------------------------------
        private
            void rcm_minimize_window_Click(object sender, EventArgs e)
        {
            ListViewItem item = processes_listview.SelectedItems[0];
            int process_id = Int32.Parse(item.Text);

            process_essentials.minimizeWindow(process_id);
        }


        //---------------------------------------------------------------------------
        private
            void rcm_maximize_window_Click(object sender, EventArgs e)
        {
            ListViewItem item = processes_listview.SelectedItems[0];
            int process_id = Int32.Parse(item.Text);

            process_essentials.maximizeWindow(process_id);
        }


        //---------------------------------------------------------------------------
        private
            void processes_listview_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                int process_id;

                foreach (ListViewItem item in processes_listview.SelectedItems)
                {
                    process_id = Int32.Parse(item.Text);

                    using (Process process = Process.GetProcessById(process_id))
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch (UnauthorizedAccessException)
                        {
                            
                            continue;  //
                        }
                        catch (ArgumentException)
                        {
                            continue; //
                        }
                    }
                }
            }
        }


        //---------------------------------------------------------------------------
        private
            void rcm_add_process_blocklist_Click(object sender, EventArgs e)
        {
            ListViewItem item = processes_listview.SelectedItems[0];

            int process_id      = Int32.Parse(item.Text);
            string program_name = processes_listview.SelectedItems[0].SubItems[1].Text;

            

            using (Process pid = Process.GetProcessById(process_id))
            {
                try
                {
                    pid.Kill();
                }               
                catch (UnauthorizedAccessException)
                {
                    //
                }
            }

            blockeprocesses_lb.Items.Add(program_name);
            //  blocked_process_list.Add(program_name);
        }


        //---------------------------------------------------------------------------
        

        private void tabview_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                processes_listview.BackColor        = Color.Black;
                processes_listview.ForeColor        = Color.LightGreen;
                toolstrip.BackColor                 = Color.Black;
                toolstrip_processes_count.ForeColor = Color.LightGreen;
                watchdog_tab.BackColor              = Color.Black;
                processes_tab.BackColor             = Color.Black;


                blockeprocesses_lb.BackColor                              = Color.Black;
                blockeprocesses_lb.ForeColor                              = Color.LightGreen;
                watchdog_logger.BackColor                                 = Color.Black;
                watchdog_logger.ForeColor                                 = Color.LightBlue;


                toolstrip_cpu_label.ForeColor = Color.LightGreen;
                toolstrip_diskusage_label.ForeColor = Color.LightGreen;
                toolstrip_memory_label.ForeColor = Color.LightGreen;
                watchdog_process_groupbox.ForeColor = Color.Gold;
                this.BackColor = Color.Black;

            }

            if (e.KeyCode == Keys.Up)
            {
                

                processes_listview.BackColor        = Color.White;
                processes_listview.ForeColor        = Color.Black;
                toolstrip.BackColor                 = Color.White;
                toolstrip_processes_count.ForeColor = Color.Black;
                watchdog_tab.BackColor              = Color.White;
                processes_tab.BackColor             = Color.White;


                blockeprocesses_lb.BackColor                              = Color.White;
                blockeprocesses_lb.ForeColor                              = Color.Black;
                watchdog_logger.BackColor                                 = Color.White;
                watchdog_logger.ForeColor                                 = Color.Black;


                toolstrip_cpu_label.ForeColor = Color.Black;
                toolstrip_diskusage_label.ForeColor = Color.Black;
                toolstrip_memory_label.ForeColor = Color.Black;
                watchdog_process_groupbox.ForeColor = Color.Black;
                this.BackColor = Color.White;
                
            }
        }

        private void watchdog_setting_gb_Enter(object sender, EventArgs e)
        {

        }



        private 
            void blockeprocesses_lb_MouseClick(object sender, MouseEventArgs e)
        {
            
        }

        private void rcm_blockedproclb_Remove_Click(object sender, EventArgs e)
        {
            int item = blockeprocesses_lb.SelectedIndex;

            try
            {
                blockeprocesses_lb.Items.RemoveAt(item);
            }
            catch (IndexOutOfRangeException)
            {
                //
            }
            catch (Exception)
            {
                //
            }
        }

        private void blockeprocesses_lb_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {

                var focusedItem = blockeprocesses_lb.IndexFromPoint(e.Location);
                rightclick_option_blocked_processes.Show(Cursor.Position);
                rightclick_option_blocked_processes.Visible = true;

            }
        }

        private void toolstrip_watchdog_clearlist_Click(object sender, EventArgs e)
        {
            blockeprocesses_lb.Items.Clear();
        }

        private void toolstrip_search_for_process_Click(object sender, EventArgs e)
        {
            search_process_form form = new search_process_form();
            form.Show();    
        }
    }


    /* Class  for  updating UI elements from calling threads */


    class change_via_thread
    {
        delegate void UniversalVoidDelegate();

        public static void ControlInvoke(Control control, Action function)
        {
            if (control.IsDisposed || control.Disposing)
                return;

            if (control.InvokeRequired)
            {

                control.Invoke(new UniversalVoidDelegate(() => ControlInvoke(control, function)));
                return;

            }
            function();
        }
    }
}
