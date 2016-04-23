using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FosuHelper;


namespace FosuHelper
{
    class fsurfing
    {
        // Username, your student ID
        public string USERNAME = "StudentID";

        // Password, your password for esurfing client, not for iNode client
        public string PASSWORD = "Password";

        public string IP;

        public string MAC;

        // Net Auth Server IP, the IP 113.105.243.254 is for FOSU
        private string NASIP = "113.105.243.254";

        // iswifi, the default value is 1050, I don't know what it mean
        // another values: 4060, 4070
        public string ISWIFI = "1050";

        private string LOGINURL = "http://enet.10000.gd.cn:10001/client/login";
        private string CHALLENGEURL = "http://enet.10000.gd.cn:10001/client/challenge";
        private string HEARTBEATURL = "http://enet.10000.gd.cn:8001/hbservice/client/active?";
        private string CHECKINTERNETURL = "http://www.qq.com";
        private string SECRET = "Eshore!@#";

        private string UA = "Mozilla/4.0 (compatible; MSIE 5.01; Windows NT 5.0)";

        public fsurfing(string username, string password, string ip, string mac)
        {
            USERNAME = username;
            PASSWORD = password;
            IP = ip;
            MAC = mac;
        }

        public string GetTimestamp()
        {
            TimeSpan timestamp = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(timestamp.TotalMilliseconds).ToString();  // 以毫秒为单位
        }

        public string GetMD5(string str)
        {
            MD5 md5 = MD5.Create();
            byte[] data = Encoding.UTF8.GetBytes(str);
            byte[] data2 = md5.ComputeHash(data);

            return GetbyteToString(data2);
        }

        public string GetJSONString(Dictionary<string, string> datas)
        {
            string JSONString = "{";
            int i = 1;
            foreach(KeyValuePair<string, string> data in datas)
            {
                JSONString += "\"" + data.Key + "\":\"" + data.Value + "\"";
                if(i < data.Key.Count())
                {
                    JSONString += ",";
                }
            }
            JSONString += "}";
            return JSONString;
        }

        private string GetbyteToString(byte[] data)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sb.Append(data[i].ToString("X2"));
            }
            return sb.ToString();
        }

        public string GetToken(string response)
        {
            if (response != "failed")
            {
                string[] temp = response.Split('"');
                return temp[3];
            }
            return "failed";
        }

        public string PostChallenge()
        {
            string timestamp = GetTimestamp();
            string authenticator = GetMD5(IP + NASIP + MAC + timestamp + SECRET);

            Dictionary<string, string> datas = new Dictionary<string, string>();
            datas.Add("username", USERNAME);
            datas.Add("clientip", IP);
            datas.Add("nasip", NASIP);
            datas.Add("mac", MAC);
            datas.Add("timestamp", timestamp);
            datas.Add("authenticator", authenticator);

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("User-agent", UA);

            string response = string.Empty;
            
            try
            {
                response = Common.HTTPPost(CHALLENGEURL, headers, GetJSONString(datas));
            }
            catch(Exception ex)
            {
                response = "failed";
            }
            if(response == null)
            {
                response = "failed";
            }
            return response;
        }

        public string PostLogin(string token)
        {
            string timestamp = GetTimestamp();
            string authenticator = GetMD5(IP + NASIP + MAC + timestamp  + token + SECRET);

            Dictionary<string, string> datas = new Dictionary<string, string>();
            datas.Add("username", USERNAME);
            datas.Add("password", PASSWORD);
            datas.Add("clientip", IP);
            datas.Add("nasip", NASIP);
            datas.Add("mac", MAC);
            datas.Add("timestamp", timestamp);
            datas.Add("authenticator", authenticator);
            datas.Add("iswifi", ISWIFI);

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("User-agent", UA);

            string response = string.Empty;

            try
            {
                response = Common.HTTPPost(LOGINURL, headers, GetJSONString(datas));
            }
            catch (Exception ex)
            {
                response = "failed";
            }
            return response;
        }

        public string Heartbeat()
        {
            string timestamp = GetTimestamp();
            string authenticator = GetMD5(IP + NASIP + MAC + timestamp + SECRET);
            string url = HEARTBEATURL + "username=" + USERNAME + "&clientip=" + IP + "&nasip=" + NASIP + "&mac=" + MAC + "&timestamp=" + timestamp + "&authenticator=" + authenticator;

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("User-agent", UA);

            string response = string.Empty;
            try
            {
                response = Common.HTTPGet(url, headers);
            }
            catch (Exception ex)
            {
                response = "failed";
            }
            return response;
        }
    }
}
