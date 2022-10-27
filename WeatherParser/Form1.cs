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
using System.Threading;
using System.Security.Cryptography;

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
            LoadCities();
            textBox1.Enabled = false;
            comboBox2.Sorted = true;
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox2.SelectedIndex = 0;
        }

        private void LoadCities()
        {
            this.Enabled = false;
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
                using(var stream = new GZipStream(File.OpenRead("cities.gz"), CompressionMode.Decompress))
                {
                    using (var sw = new BinaryWriter(File.OpenWrite("cities.json")))
                    {
                        int bytes = 1;
                        while (bytes>0)
                        {
                            byte[] buff = new byte[256];
                            bytes = stream.Read(buff, 0, 256);
                            sw.Write(buff, 0, bytes);
                        }
                    }
                }
                File.Delete("cities.gz");

                this.cities = JsonConvert.DeserializeObject<List<CityInfo>>(File.ReadAllText("cities.json"));
            }

            foreach (CityInfo item in this.cities)
            {
                comboBox1.Items.Add(item.City);
                if (!comboBox2.Items.Contains(item.Country))
                    comboBox2.Items.Add(item.Country);
            }
            this.Enabled = true;
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
