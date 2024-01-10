using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Dolinay;
using System.IO;
using NLog;
using NLog.Targets;
using RestSharp;

namespace SimpleDetector
{
    public partial class Form1 : Form
    {
        private DriveDetector driveDetector = null;
        Logger logger = LogManager.GetLogger("simple");
        public Form1()
        {
            InitializeComponent();
            driveDetector = new DriveDetector();
            driveDetector.DeviceArrived += new DriveDetectorEventHandler(OnDriveArrived);
            driveDetector.DeviceRemoved += new DriveDetectorEventHandler(OnDriveRemoved);
            driveDetector.QueryRemove += new DriveDetectorEventHandler(OnQueryRemove);

            var target = new FileTarget();
            target.FileName = "${basedir}/output.log";

            NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(target, LogLevel.Info);

            
            
        }

        // Called by DriveDetector when removable device in inserted 
        private void OnDriveArrived(object sender, DriveDetectorEventArgs e)
        {
            // Report the event in the listbox.
            // e.Drive is the drive letter for the device which just arrived, e.g. "E:\\"
            string s = "Drive arrived " + e.Drive;
            listBox1.Items.Add(s);
            logger.Info(s);

            CheckAndSendF(e.Drive);

            // If you want to be notified when drive is being removed (and be able to cancel it), 
            // set HookQueryRemove to true 
            if ( checkBoxAskMe.Checked )   
                e.HookQueryRemove = true;                 
        }

        public string SendString(RestSharp.Method myMethod,String Body, String PostToDeviceAdress, String Destination)
        {
            string result = string.Empty;
            var client = new RestClient(PostToDeviceAdress); // "http://172.20.32.21:8085/datadiod");
            var request = new RestRequest();
            request.Method = myMethod; // Method.Post;
            request.AddHeader("Content-Type", "application/json");

            var body = Body;

            StreamWriter sw = new StreamWriter("body.txt", true);
            sw.WriteLine(body);
            sw.Close();

            request.AddParameter("application/json", body, ParameterType.RequestBody);
            var response = client.Execute(request);
            Console.WriteLine(response.Content);
            return result;
        }
        private void CheckAndSendF(string DriveName)
        {
            //TODO: check file and log
            //TODO: send file if exist
        }

        // Called by DriveDetector after removable device has been unpluged 
        private void OnDriveRemoved(object sender, DriveDetectorEventArgs e)
        {
            // TODO: do clean up here, etc. Letter of the removed drive is in e.Drive;
            
            // Just add report to the listbox
            string s = "Drive removed " + e.Drive;
            listBox1.Items.Add(s);
            logger.Info(s);
        }

        // Called by DriveDetector when removable drive is about to be removed
        private void OnQueryRemove(object sender, DriveDetectorEventArgs e)
        {
            // Should we allow the drive to be unplugged?
            if (checkBoxAskMe.Checked)
            {
                if (MessageBox.Show("Allow remove?", "Query remove",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    e.Cancel = false;       // Allow removal
                else
                    e.Cancel = true;        // Cancel the removal of the device  
            }
        }
        

        // User checked the "Ask me before drive can be disconnected box"        
        private void checkBoxAskMe_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxAskMe.Checked)
            {
                // Is QueryRemove enabled? 
                // If not, we will enable it for the drive which is selected 
                // in the listbox. 
                // If the listbox is empty, no drive has been detected yet so do nothing now.
                if (!driveDetector.IsQueryHooked && listBox1.Items.Count > 0)
                {
                    if (listBox1.SelectedItem == null)
                    {
                        MessageBox.Show("Please choose the drive for which you wish to be asked in the list (select its message).");
                        checkBoxAskMe.Checked = false;
                        return;
                    }

                    bool ok = false;
                    string s = (string)listBox1.SelectedItem;
                    int n = s.IndexOf(':');
                    if (n > 0)
                    {
                        s = s.Substring(n - 1, 3);  // Gets drive letter from the message, (e.g. "E:\\") 

                        // Tell DriveDetector to monitor this drive
                        ok = driveDetector.EnableQueryRemove(s);
                    }

                    if (!ok)
                        MessageBox.Show("Sorry, for some reason notification for QueryRemove did not work out.");

                }

            }
            else
            {
                // "unchecked" the box so disable query remove message
                if (driveDetector.IsQueryHooked)
                    driveDetector.DisableQueryRemove();
            }                        
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Close();            
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txtContent.Text = Properties.Resources.ContentFile.ToString();
            txtURL.Text = Properties.Resources.UrlPathFile.ToString();
        }
    }
}