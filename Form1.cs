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



        /*
        private
            void fill_process_data(Process process)
        {
            

            string[] process_item_indexes = new string[2];
            ListViewItem item;



            
            string p_name;
            string pid;



            using (process)
            {
                p_name = process.ProcessName + ".exe";
                pid    = process.Id.ToString();
            }


            

            process_item_indexes[0] = p_name;
            process_item_indexes[1] = pid;

            item = new ListViewItem(process_item_indexes);
            
            change_via_thread.ControlInvoke(processes_listview, () => processes_listview.BeginUpdate());        //  ~20% less memory usage
            change_via_thread.ControlInvoke(processes_listview, () => processes_listview.Items.Add(item));
            change_via_thread.ControlInvoke(processes_listview, () => processes_listview.EndUpdate());          //


            
        }
        



        private
            void start__fill_process_data()
        {

            change_via_thread.ControlInvoke(processes_listview, () =>
                    processes_listview.Items.Clear()
                    );

            

            Process[] processes = Process.GetProcesses();




            change_via_thread.ControlInvoke(processes_listview, () =>
                    processes_listview.VirtualListSize = processes.Length
                    ); 

            foreach (Process process in processes)
            {
                Thread p_thread = new Thread(() => fill_process_data(process));
                p_thread.Start();
            }
        }
        */

        //---------------------------------------------------------------------------
        private
            void update_process_count_lbl()
        {
            while (true)
            {
                Process[] count;

                try
                {

                    count = Process.GetProcesses();

                    change_via_thread.ControlInvoke(label1, () =>
                    label1.Text = count.Length.ToString()
                    );   
                    
                }
                catch (Exception)
                {
                    continue;
                }
                Thread.Sleep(444);
            }
        }


        //---------------------------------------------------------------------------
        private void main_form_Load(object sender, EventArgs e)
        {



            processes_listview.View = View.Details;
            processes_listview.ListViewItemSorter = null;
            processes_listview.Columns.Add("Process", 70);
            processes_listview.Columns.Add("PID", 50);





            Thread add_new_spawned_process = new Thread(start_AddNewprocess);
            add_new_spawned_process.Start();
            

            Thread thread_updateProcessCounter_lbl = new Thread(update_process_count_lbl);
            thread_updateProcessCounter_lbl.Start();
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

                data[0] = p_name;
                data[1] = pid;

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

        void processStopEvent_EventArrived(object sender, EventArrivedEventArgs e)
        {
            //  string p_name = e.NewEvent.Properties["ProcessName"].Value.ToString();
            //  string pid = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value).ToString();
        }


        //---------------------------------------------------------------------------
        private
            void label1_TextChanged(object sender, EventArgs e)
        {
            //Thread thread = new Thread (start__fill_process_data);
            //thread.Start();
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
            void button1_Click(object sender, EventArgs e)
        {
            label2.Text = processes_listview.Items.Count.ToString();            
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
