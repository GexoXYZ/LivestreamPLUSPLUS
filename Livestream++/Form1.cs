using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Windows.Forms;
using MetroFramework;
using MetroFramework.Forms;
using Newtonsoft.Json;

namespace Livestream__
{
    public partial class Form1 : MetroForm
    {
        public Form1()
        {
            InitializeComponent();



            int id = 0;     
            RegisterHotKey(Handle, id, (int)KeyModifier.Alt, Keys.L.GetHashCode());
        }

        #region GlobalHotkey
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
 
        enum KeyModifier
        {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            WinKey = 8
        }
 

 
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
 
            if (m.Msg == 0x0312)
            {
                /* Note that the three lines below are not needed if you only want to register one hotkey.
                 * The below lines are useful in case you want to register multiple keys, which you can use a switch with the id as argument, or if you want to know which key/modifier was pressed for some particular reason. */
 
                Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);                  // The key of the hotkey that was pressed.
                KeyModifier modifier = (KeyModifier)((int)m.LParam & 0xFFFF);       // The modifier of the hotkey that was pressed.
                int id = m.WParam.ToInt32();                                        // The id of the hotkey that was pressed.
    
                Show();
                WindowState = FormWindowState.Normal;
                

            }
        }
        #endregion

        void ExecuteCommand(string channel)
        {
            
            int exitCode;
            ProcessStartInfo processInfo;
            Process process;

            processInfo = new ProcessStartInfo("cmd.exe", "/c " +   "livestreamer twitch.tv/" + channel + " " + quality());
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            // *** Redirect the output ***
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;

            process = Process.Start(processInfo);
            process.WaitForExit();

            // *** Read the streams ***
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            exitCode = process.ExitCode;

            
            process.Close();
        }

        public string quality()
        {
            string selectedItem = "";
            if (metroComboBox1.InvokeRequired)
            {
                metroComboBox1.Invoke(new MethodInvoker(delegate { selectedItem = metroComboBox1.SelectedItem.ToString(); }));
            }
           

            if (isUserPartner(txt_Channel.Text))
            {
                if (selectedItem == "Source (Best)")
                {
                    return "source";
                }
                if (selectedItem == "High")
                {
                    return "high";
                }
                if (selectedItem == "Medium")
                {
                    return "medium";
                }
                if (selectedItem == "Low")
                {
                    return "low";
                }
                if (selectedItem == "Mobile (Worst)")
                {
                    return "mobile";
                }
                return "audio";
            }
            if (selectedItem !="Source (Best)")
            {


                MetroMessageBox.Show(this,
                    "Forced source quality because " + txt_Channel.Text + " doesn't have quality options!",
                    "Quality = Source", MessageBoxButtons.OK, MessageBoxIcon.Information);
                metroComboBox1.Text = "Source (Best)";

                return "source";
            }
            return "penis";



        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterHotKey(Handle, 0);    
            Application.Exit();
        }

        private bool isUserLive(string username)
        {
            string sUsername = username;

            string sUrl = "https://api.twitch.tv/kraken/streams/" + sUsername;
            HttpWebRequest wRequest = (HttpWebRequest) HttpWebRequest.Create(sUrl);
            wRequest.ContentType = "application/json";
            wRequest.Accept = "application/vnd.twitchtv.v3+json";
            wRequest.Method = "GET";

            dynamic wResponse = wRequest.GetResponse().GetResponseStream();
            StreamReader reader = new StreamReader(wResponse);
            dynamic res = reader.ReadToEnd();
            reader.Close();
            wResponse.Close();


            try
            {
                string live = ((dynamic) JsonConvert.DeserializeObject(res)).stream._id;
                return true;
            }
            catch
            {
                
            }
            return false;
        }
        private bool isUserPartner(string username)
        {
            string sUsername = username;

            string sUrl = "https://api.twitch.tv/kraken/streams/" + sUsername;
            HttpWebRequest wRequest = (HttpWebRequest)HttpWebRequest.Create(sUrl);
            wRequest.ContentType = "application/json";
            wRequest.Accept = "application/vnd.twitchtv.v3+json";
            wRequest.Method = "GET";

            dynamic wResponse = wRequest.GetResponse().GetResponseStream();
            StreamReader reader = new StreamReader(wResponse);
            dynamic res = reader.ReadToEnd();
            reader.Close();
            wResponse.Close();

            string live = ((dynamic)JsonConvert.DeserializeObject(res)).stream.channel.partner;
            if (live == "True")
                return true;
            return false;
        }



    

        private void btn_startStream_Click(object sender, EventArgs e)
        {
            if (txt_Channel.Text == "" || metroComboBox1.SelectedItem == null)
            {
                MetroFramework.MetroMessageBox.Show(this,
                    "Please determine a proper username and/or select a Stream quality.","Error",MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                try
                {
                    if (isUserLive(txt_Channel.Text))
                    {
                        webBrowser1.Navigate("http://twitch.tv/" + txt_Channel.Text + "/chat?popout=");
                        

                        //Hide();
                        BackgroundWorker bw = new BackgroundWorker();

                        bw.WorkerReportsProgress = true;
                        bw.DoWork += new DoWorkEventHandler(
                            delegate(object o, DoWorkEventArgs args)
                            {
                                BackgroundWorker b = o as BackgroundWorker;
                                ExecuteCommand(txt_Channel.Text.ToLower());
                            });
                        bw.RunWorkerAsync();
                        
                    }
                    else
                    {
                        MetroFramework.MetroMessageBox.Show(this,
                       txt_Channel.Text + " does not seem to be streaming right now!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception)
                {
                    MetroFramework.MetroMessageBox.Show(this,
                       txt_Channel.Text + " was  not found! Please make sure you didn't misspell the name.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    
                }
                
                
                
                
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void metroTabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (metroTabControl1.SelectedIndex == 1)
            {
                Size = new Size(350, 588);
                metroTabControl1.Size = new Size(304, 502);
                webBrowser1.Size = new Size(300, 454);
            }
            else if (metroTabControl1.SelectedIndex == 0)
            {
                Size = new Size(300, 267);
                metroTabControl1.Size = new Size(254, 502);
            }
        }





       


    }
}
