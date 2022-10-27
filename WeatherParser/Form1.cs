using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Diagnostics;

namespace WeatherParser
{
    public partial class Form1 : Form
    {
        const string key = "83ca2441a3f3d50b7d33f56758adad1d";
        List<CityInfo> cities=null;
        Task t;
        public Form1()
        {
            InitializeComponent();
            Task.Run(()=>LoadCities());
            textBox1.Enabled = false;
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        private void Save()
        {
            if (cities.Count > 0)
            {
                this.progressBar1.Visible = true;
                this.progressBar1.Style = ProgressBarStyle.Marquee;
                File.WriteAllText("cities.json", JsonConvert.SerializeObject(this.cities));
                this.progressBar1.Visible = false;
            }
        }

        private void LoadCities()
        {
            if (InvokeRequired)
            {
                
                Invoke(new Action(() => { this.Enabled = false; progressBar1.Style = ProgressBarStyle.Marquee; }));
            }
            else
            {
                this.Enabled = false;
                progressBar1.Style = ProgressBarStyle.Marquee;
            }
            if (File.Exists("cities.json"))
            {
                cities = JsonConvert.DeserializeObject<List<CityInfo>>(File.ReadAllText("cities.json")); 
                if(cities==null||cities.Count==0)
                {
                    MessageBox.Show("Error!");
                    this.Close();
                    return;
                }
            }
            else
            {
                string cities = "http://bulk.openweathermap.org/sample/city.list.json.gz";

                WebClient webClient = new WebClient();

                try
                {
                    webClient.DownloadFile(cities, "cities.gz");
                }
                catch(Exception e)
                {
                    MessageBox.Show(e.Message + "Your internet is slow. Reconnect and try again.");
                    this.Close();
                    return;
                }
                Process proc = Process.Start("WinRAR.exe", $"e \"cities.gz\" {Environment.CurrentDirectory}");
                File.Delete("cities.gz");

                this.cities = JsonConvert.DeserializeObject<List<CityInfo>>(File.ReadAllText("city.list.json"));

                File.Delete("city.list.json");
            }
            Action<CityInfo> a= (obj) =>
            {
                comboBox1.Items.Add(obj.City);
                if (!comboBox2.Items.Contains(obj.Country))
                    comboBox2.Items.Add(obj.Country);
            };
            foreach (CityInfo item in this.cities)
            {
                if (InvokeRequired)
                {
                    this.Invoke((MethodInvoker)(()=>a(item)));
                }
                else
                {
                    comboBox1.Items.Add(item.City);
                    if (!comboBox2.Items.Contains(item.Country))
                        comboBox2.Items.Add(item.Country);
                }                
            }
            if (InvokeRequired)
            {
                Invoke(new Action(() => { 
                    progressBar1.Visible = false;
                    this.Enabled = true;
                    comboBox2.SelectedIndex = 0;
                    comboBox1.SelectedIndex = 0;
                }));
            }
            else
            {
                progressBar1.Visible = false;
                this.Enabled = true;
                comboBox2.SelectedIndex = 0;
                comboBox1.SelectedIndex = 0;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            string query = $@"https://api.openweathermap.org/data/2.5/weather?q={comboBox1.SelectedItem.ToString()}&appid={key}&units=metric";

            var request = HttpWebRequest.CreateHttp(query);
            var response=(HttpWebResponse)request.GetResponse();
            string json = null;
            using(var streamreader=new StreamReader(response.GetResponseStream()))
            {
                json=streamreader.ReadToEnd();
            }
            if(json==null)
            {
                MessageBox.Show("Error!");
                return;
            }

            textBox1.Text = JsonConvert.DeserializeObject<Weather>(json).ToString();
            button1.Enabled = true;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            foreach (var item in this.cities)
            {
                if(item.Country==comboBox2.SelectedItem.ToString())
                {
                    comboBox1.Items.Add(item.City);
                }
            }
            comboBox1.SelectedIndex = 0;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Save();
        }
    }

    public class CityInfo
    {
        [JsonProperty("country")]
        public string Country { get; set; }
        [JsonProperty("name")]
        public string City { get; set; }
        [JsonConstructor]
        public CityInfo()
        {

        }

        public override string ToString()
        {
            return City;
        }
    }

    public class WeatherInfo
    {
        [JsonProperty("humidity")]
        public string Humidity { get; set; }

        [JsonProperty("temp")]
        public string Temp { get; set; }

        [JsonProperty("pressure")]
        public string Pressure { get; set; }

        [JsonConstructor]
        public WeatherInfo()
        {

        }

        public override string ToString()
        {
            return $"Humidity: {Humidity}, Temp: {Temp}, Pressure: {Pressure}";
        }
    }

    public class Weather
    {
        [JsonProperty("main")]
        public WeatherInfo weather { get; set; }

        public Weather()
        {
        
        }

        public override string ToString()
        {
            return weather.ToString();
        }
    }
}
