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
    public partial class main_form : Form
    {
        public main_form()
        {
            InitializeComponent();
        }

        ProcessEssentials process_essentials = new ProcessEssentials();


        public List<string> blocked_process_list = new List<string>();






        //---------------------------------------------------------------------------
        public
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
                    }
                }
                catch (Exception)
                {
                    continue;
                }

                Thread.Sleep(250);
            }           
        }

       
        public 
            void update_vetted_processList()
        {
            while (true)
            {

                change_via_thread.ControlInvoke(processes_listview, () => toolstrip_processes_count.Text = processes_listview.Items.Count.ToString()); ;
                Thread.Sleep(250);

            }
        }


        //---------------------------------------------------------------------------
        public 
            void thread_setup_form_on_startup(int pid)
        {
            

            ListViewItem item;

            string[] data = new string[3];          
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
                catch (UnauthorizedAccessException)
                {
                    data[2] = "Access Denied";
                }
                catch (Exception)
                {
                    data[2] = "<Unknown>";
                }

                data[1] = process.ProcessName + ".exe";
                

                item = new ListViewItem(data);


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
        }


        //---------------------------------------------------------------------------
        void
            setup_form_on_startup()
        {
            Process[] processes = Process.GetProcesses();

            foreach(Process process in processes)
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

            Thread build_start_process = new Thread(setup_form_on_startup);
            build_start_process.Start();

            Thread remove_old_pids = new Thread(remove_dead_pids);
            remove_old_pids.Start();

            Thread update_counter = new Thread(update_vetted_processList);
            update_counter.Start();

            Thread add_new_spawned_process = new Thread(start_AddNewprocess);
            add_new_spawned_process.Start();

            Thread remove_old_processes = new Thread(start_RemoveOldProcess);
            remove_old_processes.Start();           
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


            ListViewItem item;

            string p_name;                       
            string pid;
            string owner;
            string file_name;

            string[] data = new string[3];
            bool found    = false;



            pid    = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value).ToString();
            p_name = e.NewEvent.Properties["ProcessName"].Value.ToString();


            if (string.IsNullOrEmpty(pid))
            

                return;


            else
            {

                found = true;

            }


            


            if (found)
            {
                

                foreach (var blocked_item in blockeprocesses_lb.Items)
                {
                    if (p_name == blocked_item.ToString())
                    {


                        DateTime starttime = DateTime.Now;
                        DateTime killtime;


                        using (Process process = Process.GetProcessById(Int32.Parse(pid)))
                        {
                            if (terminate_blocked_process_rb.Checked)
                            {

                 
                                //watchdog_logger.Items.Add("["+starttime+"]"+"  "+p_name + " (" + pid + ")" +" Started!");
                                process.Kill(); killtime = DateTime.Now;
                                watchdog_logger.Items.Add("[" + killtime + "]" + "  " + p_name + " (" + pid + ")" + " Terminated!");


                            }

                            if (suspend_blocked_process_rb.Checked)
                            {


                                //watchdog_logger.Items.Add("[" + starttime + "]" + "  " + p_name + " (" + pid + ")" + " started!");
                                process_essentials.freeze_process(process.Id); killtime = DateTime.Now;
                                watchdog_logger.Items.Add("[" + killtime + "]" + "  " + p_name + " (" + pid + ")" + " Suspended!");


                            }
                            
                        }

                        return;     //  If process is part of user "WatchDog" it is either getting suspended or terminated
                                    //  on start, so there is really no need to waste process time and memory filling out the listview of a "dead" / 
                                    //  "soon to be dead" process ...
                    }
                }


                /* IF PROCESS IS NOT IN WATCHDOG ...*/


                data[1] = p_name;
                data[0] = pid;

                try
                {

                    using (Process process = Process.GetProcessById(Int32.Parse(pid)))
                    {
                        try
                        {
                            file_name = process.MainModule.FileName;
                            owner     = File.GetAccessControl(file_name).GetOwner(typeof(System.Security.Principal.NTAccount)).ToString().Split('\\')[1];
                            data[2]   = owner;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            data[2] = "Access Denied";
                        }
                        catch (Exception)
                        {
                            data[2] = "<Unknown>";      //  System processes that require trust certs.
                        }


                    }
                }
                catch (ArgumentException)
                {
                    return;  //  Usually if process has exited before we build to listview
                }
            }
          

            item = new ListViewItem(data);


            try
            {
               
                change_via_thread.ControlInvoke(processes_listview, () => processes_listview.BeginUpdate());        
                change_via_thread.ControlInvoke(processes_listview, () => processes_listview.Items.Add(item));
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

            Thread thread = new Thread(() => process_essentials.freeze_process(Int32.Parse(item.Text)));
            thread.Start();
        }


        //---------------------------------------------------------------------------
        private 
            void rcm_resume_process_Click(object sender, EventArgs e)
        {
            ListViewItem item = processes_listview.SelectedItems[0];

            Thread thread = new Thread(() => process_essentials.continue_process(Int32.Parse(item.Text)));
            thread.Start();
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
            blocked_process_list.Add(program_name);
        }


        //---------------------------------------------------------------------------
        private
            void toolstrip_searchbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string searched_for = toolstrip_searchbox.Text;

                foreach(ListViewItem item in processes_listview.Items)
                {
                    if(item.SubItems[1].Text.StartsWith(searched_for))
                    {

                        item.Selected = true;

                    }
                }
            }
        }

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
                suspend_blocked_process_rb.ForeColor                      = Color.Gold;
                terminate_blocked_process_rb.ForeColor                    = Color.Gold;
                save_blocked_processes_for_next_session_load_cb.ForeColor = Color.Gold;

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
                suspend_blocked_process_rb.ForeColor                      = Color.Black;
                terminate_blocked_process_rb.ForeColor                    = Color.Black;
                save_blocked_processes_for_next_session_load_cb.ForeColor = Color.Black;
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
    }



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
