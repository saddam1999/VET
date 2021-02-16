using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
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

        

        
       
        public 
            void remove_dead_pids()
        {
            while (true)
            {

                foreach (ListViewItem item in processes_listview.Items)
                {
                    bool found = Process.GetProcesses().Any(x => x.Id == Int32.Parse(item.Text));

                    if (!found)
                    {

                        change_via_thread.ControlInvoke(processes_listview, () => item.Remove());

                    }                   
                }

                Thread.Sleep(1250);
            }           
        }

       
        public 
            void update_vetted_processList()
        {
            while (true)
            {

                change_via_thread.ControlInvoke(null, () => toolstrip_processes_count.Text = processes_listview.Items.Count.ToString());
                Thread.Sleep(250);

            }
        }


        //---------------------------------------------------------------------------
        private void main_form_Load(object sender, EventArgs e)
        {



            processes_listview.View = View.Details;
            processes_listview.ListViewItemSorter = null;
            processes_listview.Columns.Add("ID", 90);
            processes_listview.Columns.Add("NAME", 170);


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
            string[] data = new string[2];

            try
            {

                string p_name = e.NewEvent.Properties["ProcessName"].Value.ToString();
                string pid = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value).ToString();

                data[1] = p_name;
                data[0] = pid;

                item = new ListViewItem(data);

                change_via_thread.ControlInvoke(processes_listview, () => processes_listview.BeginUpdate());        //  ~20% less memory usage
                change_via_thread.ControlInvoke(processes_listview, () => processes_listview.Items.Add(item));
                change_via_thread.ControlInvoke(processes_listview, () => processes_listview.EndUpdate());          //

            }
            catch (Exception)
            {
                //
            }
        }
        


        //---------------------------------------------------------------------------
        void processStopEvent_EventArrived(object sender, EventArrivedEventArgs e)
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
                
                    path = "/select, \"" + pid.MainModule.FileName.Substring(0, pid.MainModule.FileName.LastIndexOf(program_name)) + "\"";
                    Process.Start("explorer.exe", "-p" + path);
                
            }
  
        }


        //---------------------------------------------------------------------------
        private 
            void rcm_kill_process_Click(object sender, EventArgs e)
        {
            ListViewItem item = processes_listview.SelectedItems[0];
            using (Process pid = Process.GetProcessById(Int32.Parse(item.Text)))
            {
                pid.Kill();
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
            int process_id = Int32.Parse(item.Text);

            using (Process pid = Process.GetProcessById(process_id))
            {
                pid.Kill();
            }

            using (Process pid = Process.GetProcessById(process_id))
            {
                string path = pid.MainModule.FileName;
                Process.Start(path);
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
