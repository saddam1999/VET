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


    internal struct PROCESS_STARTED_EVENT_VAR
    { 
        internal string p_name;
        internal string pid;
        internal string owner;
        internal string file_name;
        internal string[] data;
        internal ListViewItem item;
    }


    internal struct SETUP_FORM_VAR
    {
        internal string owner;
        internal string file_name;
        internal ListViewItem item;
        internal string[] data;
    }

    
    public partial class main_form : Form
    {
        public main_form()
        {
            InitializeComponent();
        }

        


        private struct main_essentials
        {
            public Task[] form_load_tasks;
        }




        main_essentials essentials           = new main_essentials();
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
                            
                                change_via_thread.ControlInvoke(processes_listview, () => item.Remove());
                          
                        Thread.Sleep(30);  
                    }
                }
                catch (Exception)
                {
                    continue;
                }

                Thread.Sleep(10000);        
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
            void _display_processes_startup(int pid)
        {

            SETUP_FORM_VAR variable_essentials = new SETUP_FORM_VAR();          
            variable_essentials.data           = new string[5];                     


            using (Process process = Process.GetProcessById(pid))
            {

                variable_essentials.data[0]   = process.Id.ToString();
                variable_essentials.data[1]   = process.ProcessName + ".exe";
                variable_essentials.file_name = null;

                try
                {
                    variable_essentials.file_name = process.MainModule.FileName;    //  If we can't get access to the mainmodule then we will
                                                                                    //  have permission issues with other things, so i'm just going
                                                                                    //  too hard-code them a "access denied" if this ever happens.
                }
                catch (Win32Exception)
                {
                    variable_essentials.data[2] = "Access Denied";
                    variable_essentials.data[3] = "Access Denied";
                    variable_essentials.data[4] = "Access Denied";
                }

                if (!string.IsNullOrWhiteSpace(variable_essentials.file_name))   
                {

                    variable_essentials.data[2] = File.GetAccessControl(variable_essentials.file_name).GetOwner(typeof(System.Security.Principal.NTAccount)).ToString().Split('\\')[1];
                    variable_essentials.data[3] = process.MainModule.FileVersionInfo.CompanyName;
                    variable_essentials.data[4] = process.MainModule.FileVersionInfo.FileVersion;

                }                                              
            }

            variable_essentials.item = new ListViewItem(variable_essentials.data);

            try
            {
                change_via_thread.ControlInvoke(processes_listview, () => processes_listview.BeginUpdate());
                change_via_thread.ControlInvoke(processes_listview, () => processes_listview.Items.Add(variable_essentials.item));
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
                    
            foreach(Process process in processes)
            {
                Thread thread = new Thread(() => _display_processes_startup(process.Id));
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


            essentials.form_load_tasks = new Task[5];



            essentials.form_load_tasks[0] = Task.Run(() => { displaySystemUsage(); });
            essentials.form_load_tasks[1] = Task.Run(() => { update_vetted_processList(); });
            essentials.form_load_tasks[2] = Task.Run(() => { remove_dead_pids(); });
            essentials.form_load_tasks[3] = Task.Run(() => { start_AddNewprocess(); });
            essentials.form_load_tasks[4] = Task.Run(() => { start_RemoveOldProcess(); });

            

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


            bool     found                    = false;
            bool     is_suspending            = toolstrip_watchdog_suspendonstart.Checked;


            variable_essentials.data   = new string[5];
            variable_essentials.pid    = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value).ToString();
            variable_essentials.p_name = e.NewEvent.Properties["ProcessName"].Value.ToString();



            if (string.IsNullOrWhiteSpace(variable_essentials.pid))
            
                return;         //  Don't even attempt listing the process.

            else
            
                found = true;


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
                                watchdog_logger.Items.Add($"[{killtime}]  {variable_essentials.p_name} ({variable_essentials.pid}) Suspended!");
                            }
                            else
                            {                               
                                process.Kill(); killtime = DateTime.Now;
                                watchdog_logger.Items.Add($"[{killtime}]  {variable_essentials.p_name} ({variable_essentials.pid}) Terminated!");
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



                variable_essentials.data[1] = variable_essentials.p_name;
                variable_essentials.data[0] = variable_essentials.pid;


                try
                {

                    using (Process process = Process.GetProcessById(Int32.Parse(variable_essentials.pid)))
                    {

                        variable_essentials.data[0]   = process.Id.ToString();
                        variable_essentials.data[1]   = process.ProcessName + ".exe";
                        variable_essentials.file_name = null;

                        try
                        {
                            variable_essentials.file_name = process.MainModule.FileName;    //  If we can't get access to the mainmodule then we will
                                                                                            //  have permission issues with other things, so i'm just going
                                                                                            //  too hard-code them a "access denied" if this ever happens.
                        }
                        catch (Win32Exception)
                        {
                            variable_essentials.data[2] = "Access Denied";
                            variable_essentials.data[3] = "Access Denied";
                            variable_essentials.data[4] = "Access Denied";
                        }

                        if (!string.IsNullOrWhiteSpace(variable_essentials.file_name))
                        {

                            variable_essentials.data[2] = File.GetAccessControl(variable_essentials.file_name).GetOwner(typeof(System.Security.Principal.NTAccount)).ToString().Split('\\')[1];
                            variable_essentials.data[3] = process.MainModule.FileVersionInfo.CompanyName;
                            variable_essentials.data[4] = process.MainModule.FileVersionInfo.FileVersion;

                        }
                    }
                }
                catch (ArgumentException)
                {
                    return;     //  Usually if process has exited before we write to listview
                }
            }


            variable_essentials.item = new ListViewItem(variable_essentials.data);


            try
            {
               
                change_via_thread.ControlInvoke(processes_listview, () => processes_listview.BeginUpdate());        
                change_via_thread.ControlInvoke(processes_listview, () => processes_listview.Items.Add(variable_essentials.item));
                change_via_thread.ControlInvoke(processes_listview, () => processes_listview.EndUpdate());          

            }
            catch (Exception)
            {
                return;
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
            using (Process process = Process.GetCurrentProcess())
            {
                process.Kill();
            }
                

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
            Process open_and_select_file = null;

            using (Process pid = Process.GetProcessById(Int32.Parse(item.Text)))
            {
                try
                {                  
                    open_and_select_file = Process.Start("explorer.exe", string.Format("/select,\"{0}", pid.MainModule.FileName));

                    if(open_and_select_file == null)
                    
                        throw new InvalidOperationException("Failed starting process!");                  
                }
                catch (Win32Exception)
                {
                    //
                }
                catch (FileNotFoundException)
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
                catch (Win32Exception)
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
            string path       = null;

            using (Process pid = Process.GetProcessById(process_id))
            {
                try
                {
                    pid.Kill();
                }
                catch (Win32Exception)
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
                }
                catch (UnauthorizedAccessException)
                {
                    //
                }

                if (string.IsNullOrEmpty(path))
                    
                        return;
                    
               else
                    
                  Process.Start(path);                               
            }
        }


        //---------------------------------------------------------------------------
        private
            void rcm_bring_window_to_front_Click(object sender, EventArgs e)
        {
            ListViewItem item = processes_listview.SelectedItems[0];
            int process_id    = Int32.Parse(item.Text);

            process_essentials.bringWindowToFront(process_id);
        }


        //---------------------------------------------------------------------------
        private
            void rcm_minimize_window_Click(object sender, EventArgs e)
        {
            ListViewItem item = processes_listview.SelectedItems[0];
            int process_id    = Int32.Parse(item.Text);

            process_essentials.minimizeWindow(process_id);
        }


        //---------------------------------------------------------------------------
        private
            void rcm_maximize_window_Click(object sender, EventArgs e)
        {
            ListViewItem item = processes_listview.SelectedItems[0];
            int process_id    = Int32.Parse(item.Text);

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
            ListViewItem item   = processes_listview.SelectedItems[0];

            int process_id      = Int32.Parse(item.Text);
            string program_name = processes_listview.SelectedItems[0].SubItems[1].Text;

            

            using (Process pid = Process.GetProcessById(process_id))
            {
                try
                {
                    pid.Kill();
                }               
                catch (Win32Exception)
                {
                    //
                }
            }

            blockeprocesses_lb.Items.Add(program_name);           
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
                watchdog_logger_groupbox.ForeColor = Color.Gold;
                toolstrip_count_searched_processes_lbl.ForeColor = Color.LightGreen;
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
                watchdog_logger_groupbox.ForeColor = Color.Black;
                toolstrip_count_searched_processes_lbl.ForeColor = Color.Black;
                this.BackColor = Color.White;
                
            }
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

        private 
            void blockeprocesses_lb_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {

                var focusedItem = blockeprocesses_lb.IndexFromPoint(e.Location);
                rightclick_option_blocked_processes.Show(Cursor.Position);
                rightclick_option_blocked_processes.Visible = true;

            }
        }

        private 
            void toolstrip_watchdog_clearlist_Click(object sender, EventArgs e)
        {
            blockeprocesses_lb.Items.Clear();
        }

        private 
            void toolstrip_search_for_process_Click(object sender, EventArgs e)
        {
            search_process_form form = new search_process_form();
            form.Show();           
        }

        private 
            void toolstrip_process_lookup_textbox_KeyDown(object sender, KeyEventArgs e)
        {

            //  select all processes that were searched

            if (e.KeyCode == Keys.Enter)
            {
                int counter = 0;

                foreach (ListViewItem item in processes_listview.Items)
                {
                    if (item.SubItems[1].Text.StartsWith(toolstrip_process_lookup_textbox.Text, StringComparison.InvariantCultureIgnoreCase) || item.SubItems[1].Text.Equals(toolstrip_process_lookup_textbox.Text, StringComparison.InvariantCultureIgnoreCase))
                    {

                        counter += 1;
                        item.Selected = true;
                        item.Focused  = true;

                    }
                }

                toolstrip_count_searched_processes_lbl.Text = counter.ToString() + "x";
            }



            //  terminate all processes that were selected

            if (e.KeyCode == Keys.Delete)
            {

                foreach (ListViewItem item in processes_listview.Items)
                {
                    if (item.Selected)
                    {
                        using (Process process = Process.GetProcessById(Int32.Parse(item.Text)))
                        {
                            try
                            {
                                process.Kill();
                            }
                            catch (Win32Exception)
                            {
                                continue;
                            }
                        }
                    }
                }
            }
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
