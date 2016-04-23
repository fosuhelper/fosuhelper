using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation; //获取网卡信息
using System.Net.Sockets;
using System.Net.Http;

namespace FosuHelper
{
    class Common
    {
        public static Dictionary<string, string[]> getNetworkInterfaces()
        {
            Dictionary<string, string[]> Adapters = new Dictionary<string, string[]>();

            NetworkInterface[] NICs = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface NIC in NICs)
            {
                if (NIC.NetworkInterfaceType == NetworkInterfaceType.Ethernet || NIC.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                {
                    IPInterfaceProperties IP = NIC.GetIPProperties();
                    UnicastIPAddressInformationCollection IPCollection = IP.UnicastAddresses;
                    foreach (UnicastIPAddressInformation IPAddress in IPCollection)
                    {
                        if (IPAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            //if(IPAddress.Address.ToString().Contains("169.254"))
                            //{
                            //    continue;
                            //}
                            string tempMAC = NIC.GetPhysicalAddress().ToString();
                            int i = 0;
                            IList<char> MAC = new List<char>();
                            foreach(char mac in tempMAC)
                            {
                                MAC.Add(mac);
                                i++;
                                if(i % 2 == 0 && i < 12)
                                {
                                    MAC.Add('-');
                                }
                            }
                            string AdapterName = NIC.Description.ToString();
                            string[] AdapterInfo = new string[3];
                            AdapterInfo[0] = IPAddress.Address.ToString();
                            AdapterInfo[1] = new string(MAC.ToArray());
                            AdapterInfo[2] = "\\Device\\NPF_" + NIC.Id.ToString();
                            Adapters.Add(AdapterName, AdapterInfo);
                        }
                    }
                }
            }
            return Adapters;
        }

        public static string GetTimeString(bool full = false)
        {
            if(full)
            {
                return "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] ";
            }
            return "[" + DateTime.Now.ToString("HH:mm:ss") + "] ";
        }

        public static string HTTPPost(string url, Dictionary<string, string> headers, string data)
        {
            HttpClient client = new HttpClient();
            foreach(KeyValuePair<string, string> header in headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
            var response = client.PostAsync(new Uri(url), new StringContent(data, Encoding.UTF8, "application/json")).Result;
            return response.Content.ReadAsStringAsync().Result;
        }

        public static string HTTPGet(string url, Dictionary<string, string> headers)
        {
            HttpClient client = new HttpClient();
            foreach (KeyValuePair<string, string> header in headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
            var response = client.GetAsync(new Uri(url)).Result;
            return response.Content.ReadAsStringAsync().Result;
        }

        public static void SaveConfig(string Group, string Item, string Value)
        {
            INIFile config = new INIFile(System.Windows.Forms.Application.StartupPath + "\\config.ini");
            config.Write(Group, Item, Value);
        }

        public static string GetConfig(string Group, string Item)
        {
            INIFile config = new INIFile(System.Windows.Forms.Application.StartupPath + "\\config.ini");
            return config.Read(Group, Item);
        }
    }
}
