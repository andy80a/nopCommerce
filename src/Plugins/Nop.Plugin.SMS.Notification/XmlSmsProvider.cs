using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace TestSendSms
{
    public class XmlSmsProvider
    {
        const string BalanceUrl = "http://api.atompark.com/members/sms/xml.php";;

        private readonly string userName;

        private readonly string password;

        public XmlSmsProvider(string userName, string password)
        {
            this.userName = userName;
            this.password = password;
        }

        private string ExtractNumber(string text)
        {
            string res = string.Empty;
            for (int i = 0; i < text.Length; i++)
            {
                if (Char.IsDigit(text[i]))
                    res += text[i];
            }
            if (res.Length < 9)
            {
                return string.Empty;
            }
            return "+380" + res.Substring(res.Length - 9, 9);
        }

        public Status Send(string sender, string message, string numberS)
        {
            var number = ExtractNumber(numberS);
            if (number == "+380677334294")
            {
                return new Status(){ status = "0" };
            }
            if (string.IsNullOrEmpty(number))
            {
                return new Status() { status = "-4" };
            }
            
            if (string.IsNullOrEmpty(message))
            {
                return new Status(){status = "-8"};
            }

            new Thread(delegate () {
                SendInternal(sender, message, number);
            }).Start();
            return new Status() { status = "0" };
        }

        private void SendInternal(string sender, string message, string number)
        {
            var xml = @"<?xml version='1.0' encoding='UTF-8'?><SMS>     
                <operations>      
                <operation>SEND</operation>     
                </operations>     
                <authentification>        
                <username>{0}</username>       
                <password>{1}</password>       
                </authentification>   
                <message>     
                <sender>{2}</sender>
                <text><![CDATA[{3}]]></text>       
                </message>        
                <numbers>     
                <number>{4}</number> 
                </numbers>
                </SMS>";
            xml = string.Format(xml, userName, password, sender, message, number);
            Request<Status>(BalanceUrl, xml);
        }


        public Balance Balance()
        {
            var xml = @"<?xml version='1.0' encoding='UTF-8'?><SMS>     
                <operations>      
                <operation>BALANCE</operation>      
                </operations>     
                <authentification>        
                <username>{0}</username>       
                <password>{1}</password>       
                </authentification>       
                </SMS>";
            xml = string.Format(xml, userName, password);
            return Request<Balance>(BalanceUrl, xml);
        }

        private static T Request<T>(string url, string xml) where T : Status,new()
        {
            var result = new T();
            try
            {
                var req = (HttpWebRequest) WebRequest.Create(url);

                //var requestBytes = Encoding.ASCII.GetBytes(xml);
                var requestBytes = Encoding.UTF8.GetBytes(xml);
                req.Method = "POST";
                req.ContentType = "text/xml;charset=utf-8";
                req.ContentLength = requestBytes.Length;
                Stream requestStream = req.GetRequestStream();
                requestStream.Write(requestBytes, 0, requestBytes.Length);
                requestStream.Close();

                string resultXml;
                using (var res = (HttpWebResponse) req.GetResponse())
                {
                    using (var sr = new StreamReader(res.GetResponseStream(), Encoding.Default))
                    {
                        resultXml = sr.ReadToEnd();
                    }
                }
                if (resultXml != null)
                {
                    var doc = XElement.Parse(resultXml);

                    var properties = typeof (T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
                    foreach (var p in properties)
                    {
                        var value = doc.Element(p.Name).Value;
                        p.SetValue(result, value);
                    }
                }
            }
            catch (Exception)
            {
                result.status = "-9";
            }
            return result;
        }
    }

    public class Balance : Status
    {
        public string credits { get; set; }
    }

    public class Status
    {
        public string status { get; set; }

        public string GetError()
        {
            switch (status)
            {
                case "-1":
                    return "Неправильний логін або пароль";
                case "-3":
                    return "Недостатньо кредитів на рахунку";
                case "-4":
                    return "Невірний номер отримувача";
                case "-8":
                    return "Повідомлення пусте";
                case "-9":
                    return "Exception";
            }
            return null;
        }

        public bool HasError()
        {
            return GetError() != null;
        }
    }
}
