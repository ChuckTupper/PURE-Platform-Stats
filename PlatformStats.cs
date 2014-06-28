//Have Errors write to a log file, continue.
//If connection fails, try again a few times, then continue on the normal routine

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
//using Newtonsoft.Json;
using System.Data;
using MySql.Data.MySqlClient;
//using System.Windows.Forms;
using System.Timers;

namespace PlatformStats
{
    class PlatformStats
    {
        private MySqlConnection connection;
        private WebClient client;
        private Timer aTimer;
        private string server;
        private string port;
        private string database;
        private string uid;
        private string password;
        private string table;
        private string url;
        private bool debug;
        private int timerInterval;

        private string message;

        public PlatformStats()
        {
            Initialize();
        }

        static void Main()
        {
            PlatformStats pStats = new PlatformStats();
            pStats.startTimer();
            Console.WriteLine(pStats.message);
            Console.WriteLine("Press any key to stop the program");
            Console.ReadLine();
        }

        private void Initialize() 
        {
            //pulls info from config file
            string[] lines = System.IO.File.ReadAllLines(@".\Config.txt");

            foreach (string line in lines)
            {
                if (line.ToLower().Contains("server"))
                {
                    this.server = line.Substring(line.IndexOf("=")+1);
                }
                else if (line.ToLower().Contains("port"))
                {
                    this.port = line.Substring(line.IndexOf("=") + 1);
                }
                else if (line.ToLower().Contains("user"))
                {
                    this.uid = line.Substring(line.IndexOf("=") + 1);
                }
                else if (line.ToLower().Contains("password"))
                {
                    this.password = line.Substring(line.IndexOf("=") + 1);
                }
                else if (line.ToLower().Contains("database"))
                {
                    this.database = line.Substring(line.IndexOf("=") + 1);
                }
                else if (line.ToLower().Contains("table"))
                {
                    this.table = line.Substring(line.IndexOf("=") + 1);
                }
                else if (line.ToLower().Contains("url"))
                {
                    this.url = line.Substring(line.IndexOf("=") + 1);
                }
                else if (line.ToLower().Contains("interval"))
                {
                    this.timerInterval = Convert.ToInt32(line.Substring(line.IndexOf("=") + 1));
                }
                else if (line.ToLower().Contains("debug"))
                {
                    try
                    {
                        this.debug = Convert.ToBoolean(line.Substring(line.IndexOf("=") + 1));
                    }
                    catch(FormatException e)
                    {
                        this.debug = false;
                    } 
                }
            }

            message = "PureLog Platform Stats v1.0";

            this.client = new WebClient();
            aTimer = new Timer();

            string connectionString = "SERVER=" + server + ";" + "PORT=" + port + ";" + "DATABASE=" + database + ";" + "UID=" + uid + ";" + "PWD=" + password + ";";
            this.connection = new MySqlConnection(connectionString);
        }

        private void startTimer()
        {
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Interval = timerInterval;
            aTimer.Enabled = true;
            aTimer.Start();
        }

        private void stopTimer()
        {
            aTimer.Stop();
        }

        private void setInterval(int interval)
        {
            timerInterval = interval;
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            int[] stats = this.getCounts();
            this.Insert(stats[0], stats[1], stats[2], stats[3], stats[4]);
        }

        private bool OpenConnection()
        {
            try
            {
                this.connection.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                if (debug)
                    Console.WriteLine(ex.Message);
                return false;
            }
        }

        private bool CloseConnection ()
        {
            try
            {
                this.connection.Close();
                return true;
            }
            catch (MySqlException ex)
            {
                if (debug)
                    Console.WriteLine(ex.Message);
                return false;
            }
        }

        public void Insert(int pc, int ps3, int xbox, int xone, int ps4)
        {
            if(this.OpenConnection() == true)
            {
                string dt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string query = "Insert Into " + table + " (datetime, pcPlayerCount, ps3PlayerCount, ps4PlayerCount, xboxPlayerCount, xonePlayerCount)"+
                    "VALUES ('"+dt+"','"+pc+"','"+ps3+"','"+ps4+"','"+xbox+"','"+xone+"')";

                MySqlCommand cmd = new MySqlCommand(query, this.connection);
                cmd.ExecuteNonQuery();
                this.CloseConnection();
            }
        }

        public string getStatString()
        {
            try
            {
                return this.client.DownloadString(this.url);
            }
            catch (WebException ex)
            {
                if(debug)
                    Console.WriteLine(ex.Message);
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
            else
            {
                stats[0] = -1;
                return stats;
            }
        }
    }
}
