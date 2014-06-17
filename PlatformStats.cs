//Have Errors write to a log file, continue.
//If connection fails, try again a few times, then continue on the normal routine

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
//using System.DateTime;
//using Newtonsoft.Json;
using System.Data;
using MySql.Data.MySqlClient;
using System.Windows.Forms;

namespace PlatformStats
{
    class PlatformStats
    {
        private MySqlConnection connection;
        private WebClient client;
        private string server;
        private string port;
        private string database;
        private string uid;
        private string password;
        private string table;
        private string url;

        public PlatformStats()
        {
            Initialize();
        }

        static void Main(string[] args)
        {
            PlatformStats web = new PlatformStats();
            int[] stats = web.getCounts();
            web.Insert(stats[0],stats[1],stats[2],stats[3],stats[4]);

            //Console.WriteLine(getStatString(url));
            //Console.WriteLine(getCount("pc", getStatString(url)));
            //Console.Read();
        }

        private void Initialize()
        {
            //pulls info from config file
            string[] lines = System.IO.File.ReadAllLines(@".\Config.txt");

            foreach (string line in lines)
            {
                if (line.ToLower().Contains("server"))
                {
                    server = line.Substring(line.IndexOf("=")+1);
                }
                else if (line.ToLower().Contains("port"))
                {
                    port = line.Substring(line.IndexOf("=")+1);
                }
                else if (line.ToLower().Contains("user"))
                {
                    uid = line.Substring(line.IndexOf("=")+1);
                }
                else if (line.ToLower().Contains("password"))
                {
                    password = line.Substring(line.IndexOf("=")+1);
                }
                else if (line.ToLower().Contains("database"))
                {
                    database = line.Substring(line.IndexOf("=")+1);
                }
                else if (line.ToLower().Contains("table"))
                {
                    table = line.Substring(line.IndexOf("=")+1);
                }
                else if (line.ToLower().Contains("url"))
                {
                    url = line.Substring(line.IndexOf("=") + 1);
                }

            }

            client = new WebClient();

            string connectionString = "SERVER=" + server + ";" + "PORT=" + port + ";" + "DATABASE=" + database + ";" + "UID=" + uid + ";" + "PWD=" + password + ";";
            connection = new MySqlConnection(connectionString);
        }

        private bool OpenConnection()
        {
            try
            {
                connection.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }

        }

        private bool CloseConnection ()
        {
            try
            {
                connection.Close();
                return true;
            }
            catch (MySqlException ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        public void Insert(int pc, int ps3, int xbox, int xone, int ps4)
        {
            if(this.OpenConnection() == true)
            {
                ////Pull last ID from table
                //int lastID = 0;
                //string idQuery = "Select MAX(ID) AS ID FROM " + table;
                //MySqlCommand getID = new MySqlCommand(idQuery, connection);
                //MySqlDataReader dataReader = getID.ExecuteReader();
                //while (dataReader.Read())
                //    lastID = dataReader.GetInt32("ID");
                ////insert new row with timestamp and ID
                //string dt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                //string query = "Insert Into " + table + " (datetime, ID)"+
                //    "VALUES ('"+dt+"','"+lastID+1+"')";

                string dt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string query = "Insert Into " + table + " (datetime, pcPlayerCount, ps3PlayerCount, ps4PlayerCount, xboxPlayerCount, xonePlayerCount)"+
                    "VALUES ('"+dt+"','"+pc+"','"+ps3+"','"+ps4+"','"+xbox+"','"+xone+"')";

                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.ExecuteNonQuery();
                this.CloseConnection();
            }
        }

        public string getStatString()
        {
            try
            {
                return client.DownloadString(url);
            }
            catch (WebException ex)
            {
                MessageBox.Show(ex.Message);
                return "";
            }

        }

        //Console = pc(0), ps3(1), xbox(2), xone(3), ps4(4)
        public int[] getCounts()
        {
            int[] stats = new int[5];
            stats[0] = 0;
            stats[1] = 0;
            stats[2] = 0;
            stats[3] = 0;
            stats[4] = 0;
            string statString = getStatString().ToLower();
            if (statString != "")
            {
                string[] lines = statString.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                foreach (string line in lines)
                {
                    if (line.Contains("pc.count"))
                    {
                        stats[0] = Convert.ToInt32(line.Substring(line.IndexOf("=") + 1));
                    }
                    else if (line.Contains("ps3.count"))
                    {
                        stats[1] = Convert.ToInt32(line.Substring(line.IndexOf("=") + 1));
                    }
                    else if (line.Contains("xbox.count"))
                    {
                        stats[2] = Convert.ToInt32(line.Substring(line.IndexOf("=") + 1));
                    }
                    else if (line.Contains("xone.count"))
                    {
                        stats[3] = Convert.ToInt32(line.Substring(line.IndexOf("=") + 1));
                    }
                    else if (line.Contains("ps4.count"))
                    {
                        stats[4] = Convert.ToInt32(line.Substring(line.IndexOf("=") + 1));
                    }
                }
                return stats;
            }
            return stats;

        }
    }
}
