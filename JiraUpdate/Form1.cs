using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace JiraUpdate
{
    public partial class mainFrm : Form
    {
        private int updateCount = 0;
        private int bugCount = 0;
        private string[] textBoxLines;
        private string jUser;
        private string jKey;
        private string jURL;
        private List<KeyValuePair<string, string>> tranIDs;

        public mainFrm()
        {
            InitializeComponent();
        }

        private async void BtnUpdate_Click(object sender, EventArgs e)
        {
            btnUpdate.Enabled = false;
            btnClear.Enabled = false;
            txtBugIDs.Enabled = false;

            textBoxLines = txtBugIDs.Lines;

            for (int i=0; i<textBoxLines.Length; i++)
            {
                string bugID = ReplaceWhitespace(textBoxLines[i].Trim(), ""); 

                if (bugID.Length > 0)
                {
                    bugCount++;

                    string tranID = getTranID(bugID);

                    if (tranID!="ERROR") { 
                        DoUpdate(bugID, tranID, i);
                    }
                    else
                    {
                        
                        textBoxLines[i] = bugID + "  - ERROR";
                        txtBugIDs.Lines = textBoxLines;
                        bugCount++;
                    }
                }
            }

            btnClear.Enabled = true;
            txtBugIDs.Enabled = true;
        }

        private string getTranID(string bgID)
        {
            string prjCode = "";
            try { 
            string[] prjCodeArr = bgID.Split("-");
                prjCode = prjCodeArr[0];

            string value = (from kvp in tranIDs where kvp.Key == prjCodeArr[0] select kvp.Value).Last();

            return value.ToString();
            }
            catch (Exception e)
            {
                MessageBox.Show("Module '"+ prjCode + "' not found");
                return "ERROR";
            }
        }



        private async void DoUpdate(string bugID, string transitionID, int rowID)
        {
            

            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), jURL + "/rest/api/2/issue/"+ bugID + "/transitions?expand=transitions.fields"))
                {
                    var base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes(jUser + ":" + jKey));
                    request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64authorization}");

                    request.Content = new StringContent("{\"transition\":{\"id\":\""+ transitionID + "\"}}");
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    var response = await httpClient.SendAsync(request);


                    if (response.IsSuccessStatusCode)
                    {
                        updateCount++;
                        textBoxLines[rowID] = bugID + "  - Updated";
                        txtBugIDs.Lines = textBoxLines;
                    }
                    else
                    {
                        textBoxLines[rowID] = bugID + "  - ERROR";
                        txtBugIDs.Lines = textBoxLines;
                    }


                }
            }
        }

        private void mainFrm_Load(object sender, EventArgs e)
        {

            txtURL.Enabled = false;
            jURL = ConfigurationManager.AppSettings["jiraURL"];
            txtURL.Text = jURL;

            txtUsername.Enabled = false;
            jUser = ConfigurationManager.AppSettings["jiraUser"]; 
            txtUsername.Text = jUser;

            txtPassword.Enabled = false;
            txtPassword.PasswordChar = '*';
            jKey = ConfigurationManager.AppSettings["jApiKey"]; 
            txtPassword.Text = jKey;


            var tranList = new List<KeyValuePair<string, string>>();

            foreach (string key in ConfigurationManager.AppSettings)
            {
                if (!key.StartsWith("j"))
                {
                    string value = ConfigurationManager.AppSettings[key];

                    tranList.Add(new KeyValuePair<string, string>(key, value));

                }

            }

            tranIDs = tranList;


            txtPassword.Text = jKey;

        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            btnUpdate.Enabled = true;
            txtBugIDs.Enabled = true;
            txtBugIDs.Clear();
        }

        private void txtBugIDs_TextChanged(object sender, EventArgs e)
        {

        }

        private static readonly Regex sWhitespace = new Regex(@"\s+");
        public static string ReplaceWhitespace(string input, string replacement)
        {
            return sWhitespace.Replace(input, replacement);
        }
    }
}
