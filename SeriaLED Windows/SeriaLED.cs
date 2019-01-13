using Newtonsoft.Json;
using SeriaLED.Configs;
using SeriaLED.Tools;
using SeriaLED.User32;
using Switchando.Network;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace SeriaLED.Mains
{
    public class SeriaLED
    {
        static bool _continue;
        static SerialPort _serialPort;

        static MQTTClient Client;
        static Configuration Config;
        static Dictionary<string, string> OldMessages;
        static byte Red, Green, Blue, White, Dimmer;
        static bool Fade = true;
        static bool Power = true;

        public static void Main()
        {
            ConsoleTools.ShowTray();
            if (File.Exists(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/myconfig.json"))
            {
                var json = File.ReadAllText(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/myconfig.json");
                Config = JsonConvert.DeserializeObject<Configuration>(json);
            }
            else Config = new Configuration();
            _serialPort = new SerialPort();
            if (Config.Address == null)
            {
                ConsoleTools.Show(true);
                Console.Write("MQTT Broker Address: ");
                Config.Address = Console.ReadLine();
            }
            Config.ClientID = new Guid().ToString();
            if (Config.Username == null)
            {
                ConsoleTools.Show(true);
                Console.Write("MQTT Broker Username: ");
                Config.Username = Console.ReadLine();
            }
            if (Config.Password == null)
            {
                ConsoleTools.Show(true);
                Console.Write("MQTT Broker Password: ");
                Config.Password = Console.ReadLine();
            }
            if (Config.Topic == null)
            {
                ConsoleTools.Show(true);
                Console.Write("MQTT Topic: ");
                Config.Topic = Console.ReadLine();
            }
            if (Config.COMPort == null)
            {
                ConsoleTools.Show(true);
                Console.Write("COM Port: ");
                Config.COMPort = SetPortName(_serialPort.PortName);
            }
            Config.Save();
            ConsoleTools.Show(false);
            ConsoleTools.AddApplicationToStartup();

            string message;
            StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
            Thread readThread = new Thread(Read);
            _serialPort.PortName = Config.COMPort;
            _serialPort.BaudRate = 9600;
            _serialPort.Parity = Parity.None;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Handshake = Handshake.None;
            _serialPort.ReadTimeout = 60000;
            _serialPort.WriteTimeout = 500;
            _serialPort.Open();
            _continue = true;
            readThread.Start();

            Client = new MQTTClient(Config.Address, Config.Username, Config.Password);
            Client.Connect(Config.ClientID);
            OldMessages = new Dictionary<string, string>();
            Client.Subscribe("cmnd/" + Config.Topic + "/#", OnMQTT);

            Console.WriteLine("Type QUIT to exit");

            while (_continue)
            {
                message = Console.ReadLine();
                Console.WriteLine(message);
                if (stringComparer.Equals("quit", message))
                {
                    _continue = false;
                }
                else
                {
                    _serialPort.WriteLine(message);
                }
            }

            readThread.Join();
            _serialPort.Close();
        }

        public static void Read()
        {
            while (_continue)
            {
                try
                {
                    string message = _serialPort.ReadLine();
                }
                catch (TimeoutException) { }
            }
        }
        public static string SetPortName(string defaultPortName)
        {
            string portName;

            Console.WriteLine("Available Ports:");
            foreach (string s in SerialPort.GetPortNames())
            {
                Console.WriteLine("   {0}", s);
            }

            Console.Write("Enter COM port value (Default: {0}): ", defaultPortName);
            portName = Console.ReadLine();

            if (portName == "" || !(portName.ToLower()).StartsWith("com"))
            {
                portName = defaultPortName;
            }
            return portName;
        }
        public static void OnMQTT(MqttClient sender, MqttMsgPublishEventArgs e)
        {
            var message = Encoding.UTF8.GetString(e.Message);
            if (OldMessages.ContainsKey(e.Topic))
            {
                if (OldMessages[e.Topic].Equals(message))
                {
                    return;
                }
                else
                {
                    OldMessages.Remove(e.Topic);
                }
            }
            OldMessages.Add(e.Topic, message);
            ClearOlds();

            if (e.Topic.StartsWith("cmnd/" + Config.Topic))
            {
                var cmnd = e.Topic.Substring(("cmnd/" + Config.Topic + "/").Length);
                if (cmnd.Equals("POWER"))
                {
                    if (message.Equals("OFF"))
                    {
                        if (!Power)
                        {
                            Client.Publish("stat/" + Config.Topic + "/POWER", Encoding.UTF8.GetString(e.Message));
                            return;
                        }
                        if (Fade)
                        {
                            _serialPort.WriteLine("z");
                            Thread.Sleep(100);
                        }
                        _serialPort.WriteLine("00000000");
                        Power = false;
                    }
                    else
                    {
                        if (Power)
                        {
                            Client.Publish("stat/" + Config.Topic + "/POWER", Encoding.UTF8.GetString(e.Message));
                            return;
                        }
                        if (Red == 0 && Green == 0 && Blue == 0 && White == 0)
                        {
                            if (Fade) _serialPort.WriteLine("x"); else _serialPort.WriteLine("ffffffff");
                        }
                        else
                        {
                            if (Fade) _serialPort.WriteLine("x");
                            else
                            {
                                int r = Red; int g = Green; int b = Blue;
                                r = (r * Dimmer) / 100;
                                g = (g * Dimmer) / 100;
                                b = (b * Dimmer) / 100;
                                var payload = r.ToString("x2");
                                payload += g.ToString("x2");
                                payload += b.ToString("x2");
                                var w = (White * Dimmer) / 100;
                                payload += w.ToString("x2");
                                _serialPort.WriteLine(payload);
                            }
                        }
                        Power = true;
                    }
                    Client.Publish("stat/" + Config.Topic + "/POWER", Encoding.UTF8.GetString(e.Message));
                }
                if (cmnd.Equals("DIMMER"))
                {
                    Dimmer = byte.Parse(Encoding.UTF8.GetString(e.Message));
                    var r = Red; var g = Green; var b = Blue;
                    r /= 100; g /= 100; b /= 100;
                    r *= Dimmer; g *= Dimmer; b *= Dimmer;
                    var payload = r.ToString("x2");
                    payload += g.ToString("x2");
                    payload += b.ToString("x2");
                    var w = (White / 100) * Dimmer;
                    payload += w.ToString("x2");
                    _serialPort.WriteLine(payload);
                    if (Red != 0 || Green != 0 || Blue != 0 || White != 0) Client.Publish("stat/" + Config.Topic + "/POWER", "ON"); else Client.Publish("stat/" + Config.Topic + "/POWER", "OFF");
                    Client.Publish("stat/" + Config.Topic + "/DIMMER", Dimmer + "");
                }
                if (cmnd.Equals("CT"))
                {
                    if (Fade)
                    {
                        _serialPort.WriteLine("z");
                        Thread.Sleep(100);
                    }
                    int r = 255, g = 255, b = 255, w = 255;
                    uint amb = uint.Parse(Encoding.UTF8.GetString(e.Message));
                    amb -= 153;
                    amb = (uint)(amb * 0.7349);
                    if (amb == 127)
                    {

                    }
                    if (amb > 127)
                    {
                        var diff = amb - 127;
                        r -= ((int)diff * 2) - 1;
                        g -= ((int)diff * 2) - 1;
                        b -= ((int)diff * 2) - 1;
                        w = 255;
                    }
                    if (amb < 127)
                    {
                        var diff = 127 - amb;
                        w -= ((int)diff * 2) + 1;
                        r = 255;
                        g = 255;
                        b = 255;
                    }
                    var payload = r.ToString("x2");
                    payload += g.ToString("x2");
                    payload += b.ToString("x2");
                    payload += w.ToString("x2");
                    Red = (byte)r; Green = (byte)g; Blue = (byte)b; White = (byte)w;
                    _serialPort.WriteLine(payload);
                    Client.Publish("stat/" + Config.Topic + "/CT", Encoding.UTF8.GetString(e.Message));
                    Client.Publish("stat/" + Config.Topic + "/EFFECT", "None");
                }
                if (cmnd.Equals("COLOR"))
                {
                    if (Fade)
                    {
                        _serialPort.WriteLine("z");
                        Thread.Sleep(100);
                    }
                    var color = HexToColor(message.Substring(0, 6));
                    ColorTools.RgbToHls(color.R, color.G, color.B, out double h, out double l, out double s);
                    var customL = 0.5;
                    if (l == 1) customL = 1;
                    ColorTools.HlsToRgb(h, customL, s, out int r, out int g, out int b);
                    Red = (byte)r; Green = (byte)g; Blue = (byte)b;
                    r = (r * Dimmer) / 100;
                    g = (g * Dimmer) / 100;
                    b = (b * Dimmer) / 100;
                    var payload = r.ToString("x2");
                    payload += g.ToString("x2");
                    payload += b.ToString("x2");
                    White = GetWhite(color.R, color.G, color.B);
                    var w = (White * Dimmer) / 100;
                    payload += w.ToString("x2");
                    _serialPort.WriteLine(payload);
                    //Console.WriteLine("PAYLOAD -> " + payload);
                    if (Red != 0 || Green != 0 || Blue != 0 || White != 0) Client.Publish("stat/" + Config.Topic + "/POWER", "ON"); else Client.Publish("stat/" + Config.Topic + "/POWER", "OFF");
                    Client.Publish("stat/" + Config.Topic + "/COLOR", color.R + "," + color.G + "," + color.B);
                    Client.Publish("stat/" + Config.Topic + "/DIMMER", Dimmer + "");
                    Client.Publish("stat/" + Config.Topic + "/EFFECT", "None");
                }
                if (cmnd.Equals("EFFECT"))
                {
                    if (message.Equals("None"))
                    {
                        Fade = false;
                        _serialPort.WriteLine("z");
                    }
                    if (message.Equals("Fade"))
                    {
                        Fade = true;
                        _serialPort.WriteLine("x");
                    }
                    Client.Publish("stat/" + Config.Topic + "/EFFECT", message);
                }
            }
        }
        static byte GetWhite(uint Ri, uint Gi, uint Bi)
        {
            float tM = Math.Max(Ri, Math.Max(Gi, Bi));
            if (tM == 0) return 0;
            float multiplier = 255.0f / tM;
            float hR = Ri * multiplier;
            float hG = Gi * multiplier;
            float hB = Bi * multiplier;
            float M = Math.Max(hR, Math.Max(hG, hB));
            float m = Math.Min(hR, Math.Min(hG, hB));
            float Luminance = ((M + m) / 2.0f - 127.5f) * (255.0f / 127.5f) / multiplier;
            return Convert.ToByte(Luminance);
        }
        public static Color HexToColor(string hexColor)
        {
            if (hexColor.IndexOf('#') != -1)
                hexColor = hexColor.Replace("#", "");

            int red = 0;
            int green = 0;
            int blue = 0;

            if (hexColor.Length == 6)
            {
                red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
            }
            else if (hexColor.Length == 3)
            {
                red = int.Parse(hexColor[0].ToString() + hexColor[0].ToString(), NumberStyles.AllowHexSpecifier);
                green = int.Parse(hexColor[1].ToString() + hexColor[1].ToString(), NumberStyles.AllowHexSpecifier);
                blue = int.Parse(hexColor[2].ToString() + hexColor[2].ToString(), NumberStyles.AllowHexSpecifier);
            }

            return Color.FromArgb(red, green, blue);
        }
        static async void ClearOlds()
        {
            await Task.Delay(2000);
            OldMessages.Clear();
        }
    }
}