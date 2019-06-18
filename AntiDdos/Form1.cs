using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.IO;

namespace AntiDdos
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        public static TextBox ConsoleTB = null;
        public static SynchronizationContext Sync;
        public static Dictionary<string, string> database = new Dictionary<string, string>();
        private void Form1_Load(object sender, EventArgs e)
        {
            int port = 80;
            database = ReadAll();
            if (database.ContainsKey("port"))
            {
                int nport = -1;
                port = int.TryParse(database["port"], out nport) ? nport : port;
            }
            if (database.ContainsKey("ddosminstage"))
            {
                if (int.TryParse(database["ddosminstage"], out int d))
                    numericUpDown1.Value = d;
            }
            if (database.ContainsKey("workantiddos"))
            {
                checkBox1.Checked = database["workantiddos"] == "1" ? true : false;
            }
            Sync = SynchronizationContext.Current;
            ConsoleTB = textBox1;

            AbuseIP.IPClient.ReadAll();
            Task.Factory.StartNew(() =>
            {
                new Server(port);
            });
        }

        public static void SaveAll(Dictionary<string, string> pairs)
        {
            StreamWriter sw = new StreamWriter(Environment.CurrentDirectory + "\\data.cfg");
            foreach (string key in pairs.Keys)
            {
                sw.WriteLine(key + ":" + pairs[key]);
            }
            sw.Close();
        }

        public static Dictionary<string, string> ReadAll()
        {
            Dictionary<string, string> res = new Dictionary<string, string>();
            if (File.Exists(Environment.CurrentDirectory + "\\data.cfg"))
            {
                StreamReader sw = new StreamReader(Environment.CurrentDirectory + "\\data.cfg");
                string[] pairs = sw.ReadToEnd().Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                sw.Close();
                for (int i = 0; i < pairs.Length; i++)
                {
                    string[] par = TrueSplit(pairs[i], ":");
                    if (res.ContainsKey(par[0]))
                        res[par[0]] = par[1];
                    else
                        res.Add(par[0], par[1]);
                }
            }
            if (!res.ContainsKey("port"))
                res.Add("port", "80");
            if (!res.ContainsKey("workantiddos"))
                res.Add("workantiddos", "1");
            if (!res.ContainsKey("ddosminstage"))
                res.Add("ddosminstage", "100");
            return res;
        }

        static string[] TrueSplit(string input, string ch = "=")
        {
            int pos = input.IndexOf(ch);
            if (pos >= 0)
            {
                return new string[2] { input.Substring(0, pos), input.Substring(pos + 1) };
            }
            return new string[2] { input, null };
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            AbuseIP.IPClient.SaveAll();
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            database["workantiddos"] = (checkBox1.Checked ? 1 : 0).ToString();
            Server.BlockWork = checkBox1.Checked;
        }

        private void NumericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            database["ddosminstage"] = ((int)numericUpDown1.Value).ToString();
            Server.ddosMinTarget = (int)numericUpDown1.Value;
        }

        private void NumericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            database["port"] = ((int)numericUpDown2.Value).ToString();
            Task.Factory.StartNew(() =>
            {
                Server.work = false;
                Server.Listener.Stop();
                new Server((int)numericUpDown2.Value);
                Server.work = true;
            });
        }
    }
}