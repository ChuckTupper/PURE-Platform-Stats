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
using System.IO;

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

        private static string exePath;
        private static string configPath;
        private static string logPath;
        private static string message;

        public PlatformStats()
        {
            exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            configPath = Path.Combine(exePath, @"config.txt");
            logPath = Path.Combine(exePath, @"log.txt");
            message = "PureLog Platform Stats v1.1";
        }

        static void Main()
        {
            PlatformStats pStats = new PlatformStats();
            Console.WriteLine(message + "\n");
            try
            {
                pStats.Initialize();
            }
            catch(FileNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Press any key to exit program");
                Console.ReadLine();
                Environment.Exit(0);
            }

            pStats.writeToLog(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " PURE Platform Stats started");
            pStats.initiateTimer();

            //Attempt to find last record date and set interval. If not successful, interval remains 1 hour. If difference is greater than 1 hour, timer is set to 5 seconds.
            //DateTime now = new DateTime(2014, 08, 21, 15, 30, 51);
            DateTime dt = new DateTime(1900, 1, 1);
            DateTime latestDT = pStats.getLatestRecordDate();
            if (latestDT != dt)
            {
                int interval = pStats.getInterval();
                double dif = DateTime.Now.Subtract(latestDT).TotalMilliseconds;
                if (dif < interval && dif > 0)
                    pStats.setInterval(Convert.ToInt32(interval - dif));
                else if (dif > interval)
                    pStats.setInterval(5000);
            }
            
            Console.WriteLine("Program running");
            Console.WriteLine("Press any key to stop program");
            Console.ReadLine();
            pStats.writeToLog(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " PURE Platform Stats stopped");
        }

        private void Initialize() 
        {
            //pulls info from config file
            //string[] lines = File.ReadAllLines(@".\config.txt");
            string[] lines = File.ReadAllLines(configPath);

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

            this.client = new WebClient();
            this.aTimer = new Timer();

            string connectionString = "SERVER=" + server + ";" + "PORT=" + port + ";" + "DATABASE=" + database + ";" + "UID=" + uid + ";" + "PWD=" + password + ";";
            this.connection = new MySqlConnection(connectionString);
        }

        private void initiateTimer()
        {
            this.aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            this.aTimer.Interval = timerInterval;
            this.aTimer.Enabled = true;
            this.aTimer.Start();
        }

        private void stopTimer()
        {
            this.aTimer.Stop();
        }

        private int getInterval()
        {
            return timerInterval;
        }
        private void setInterval(int i)
        {
            this.aTimer.Interval = i;
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            setInterval(timerInterval);
            int[] stats = this.getCounts();
            if (stats[0] == -1)
                return;
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
                    Console.WriteLine("Error opening Connection: " + ex.Message);
                writeToLog(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Error occured during OpenConnection(): " + ex.Message);
                setInterval(5000);
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
                    Console.WriteLine("Error closing connection: " + ex.Message);
                writeToLog(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Error occured during CloseConnection(): " + ex.Message);
                return false;
            }
        }
        private DateTime getLatestRecordDate()
        {
            string query = "select datetime from pure_platformstats.platformstats order by datetime desc limit 1 ";
           // List<DateTime>[] list = new List<DateTime>[1];
            DateTime dt = new DateTime(1900, 1, 1);
            if(this.OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(query, this.connection);
                MySqlDataReader dataReader = cmd.ExecuteReader();

                while (dataReader.Read())
                {
                    dt = Convert.ToDateTime(dataReader["datetime"]);
                }

                dataReader.Close();
                this.CloseConnection();
                return dt;
            }
            else
            {
                return dt;
            }
        }

        private void Insert(int pc, int ps3, int xbox, int xone, int ps4)
        {
            if(this.OpenConnection() == true)
            {
                string dt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string query = "Insert Into " + table + " (datetime, pcPlayerCount, ps3PlayerCount, ps4PlayerCount, xboxPlayerCount, xonePlayerCount)"+
                    "VALUES ('"+dt+"','"+pc+"','"+ps3+"','"+ps4+"','"+xbox+"','"+xone+"')";

                MySqlCommand cmd = new MySqlCommand(query, this.connection);
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    if (debug)
                        Console.WriteLine("Error when executing SQL query: " + ex.Message);
                    writeToLog(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Error occured during Insert(): " + ex.Message);
                    setInterval(5000);
                }
                this.CloseConnection();
            }
            else
            {
                setInterval(5000);
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
                    Console.WriteLine("Error when retrieving stats: " + ex.Message);
                writeToLog(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Error occured during getStatString(): " + ex.Message);
                setInterval(5000);
                return "";
            }
        }

        public void writeToLog(string s)
        {
            if (!File.Exists(logPath))
            {
                using (StreamWriter file = File.CreateText(logPath))
                {
                    file.WriteLine(s);
                    Console.WriteLine("Log file created at: " + logPath + "\n");
                   
                }
            }
            else
            {
                using (StreamWriter file = new System.IO.StreamWriter(logPath, true))
                {
                    file.WriteLine(s);
                }
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
