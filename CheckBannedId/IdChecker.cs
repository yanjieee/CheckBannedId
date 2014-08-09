using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Collections;
using System.IO;
using System.IO.Compression;

namespace CheckBannedId
{
    class IdChecker
    {
        private TAccount _account;
        private Form1 _form;
        private String _host = "";
        private String _code = "";
        private String _refer = "";

        public IdChecker(Form1 form, TAccount acc)
        {
            _host = acc.host;
            _code = acc.code;
            _refer = acc.refer;
            _form = form;
            _account = acc;
        }

        public void run()
        {
            string host = _host;
            string code = _code;
            string refer = _refer;
            HttpResponse response;
            response = GetHTTPPage("http://" + host + code, refer);
            if (processAPN(ref host, ref code, response))
            {
                response = GetHTTPPage("http://" + host + code, refer);
                processAPN(ref host, ref code, response);
            }
            _form._checkedCount++;
        }

        /// <summary>
        /// 处理APN的情况
        /// </summary>
        /// <param name="host"></param>
        /// <param name="code"></param>
        /// <param name="html"></param>
        private Boolean processAPN(ref String host, ref String code, HttpResponse rsp)
        {
            String url = "";
            if (rsp.StatusCode != 200)
            {
                return false;
            }
            string html = rsp.Html;
            if (html.IndexOf("bdref") != -1)
            {
                url = GetMid(html, "http://", "'");
                url += Uri.EscapeDataString(_refer);
                url += "&bdtop=true&bdifs=0&id" + GetMid(html, "&id", "\"");
                host = url.Substring(0, url.IndexOf("/"));
                code = GetMid(url, host, "");
                return true;
            }
            else
            {
                if (html.IndexOf("http") == -1)
                {
                    _form.SetAccountBanned(_account.id);
                    _form._bannedCount++;
                }
                return false;
            }
        }

        struct HttpResponse
        {
            public int StatusCode;
            public string Html;
        }

        private HttpResponse GetHTTPPage(string url, string _refer)
        {
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            StreamReader reader = null;
            HttpResponse res;
            res.StatusCode = -1;
            res.Html = "";
            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                request.UserAgent = "Mozilla/5.0 (Windows NT 5.1) AppleWebKit/537.1 (KHTML, like Gecko) Chrome/21.0.1180.89 Safari/537.1";
                request.Timeout = 30000;
                request.Headers.Add("Accept-Encoding", "gzip, deflate");
                request.Proxy = null;
                request.CookieContainer = CC;
                request.KeepAlive = false;
                request.AllowAutoRedirect = true;
                request.Referer = _refer;
                response = (HttpWebResponse)request.GetResponse();
                res.StatusCode = (int)response.StatusCode;
                BugFix_CookieDomain(CC);
                if (response.StatusCode == HttpStatusCode.OK && response.ContentLength < 1024 * 1024)
                {
                    Stream stream = response.GetResponseStream();
                    stream.ReadTimeout = 30000;
                    if (response.ContentEncoding == "gzip")
                    {
                        reader = new StreamReader(new GZipStream(stream, CompressionMode.Decompress), Encoding.Default);
                    }
                    else
                    {
                        reader = new StreamReader(stream, Encoding.Default);
                    }
                    string html = reader.ReadToEnd();
                    res.Html = html;
                    return res;
                }
            }
            catch (Exception ex)
            {
                //System.Console.WriteLine(ex.Message);
                return res;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                    response = null;
                }
                if (reader != null)
                    reader.Close();
                if (request != null)
                    request = null;
            }
            return res;
        }
        

        private String GetMid(String input, String s, String e)
        {
            int pos = input.IndexOf(s);
            if (pos == -1)
            {
                return "";
            }

            pos += s.Length;

            int pos_end = 0;
            if (e == "")
            {
                pos_end = input.Length;
            }
            else
            {
                pos_end = input.IndexOf(e, pos);
            }

            if (pos_end == -1)
            {
                return "";
            }

            return input.Substring(pos, pos_end - pos);
        }

        private CookieContainer CC = new CookieContainer();

        private void BugFix_CookieDomain(CookieContainer cookieContainer)
        {
            System.Type _ContainerType = typeof(CookieContainer);
            Hashtable table = (Hashtable)_ContainerType.InvokeMember("m_domainTable",
                                       System.Reflection.BindingFlags.NonPublic |
                                       System.Reflection.BindingFlags.GetField |
                                       System.Reflection.BindingFlags.Instance,
                                       null,
                                       cookieContainer,
                                       new object[] { });
            ArrayList keys = new ArrayList(table.Keys);
            foreach (string keyObj in keys)
            {
                string key = (keyObj as string);
                if (key[0] == '.')
                {
                    string newKey = key.Remove(0, 1);
                    table[newKey] = table[keyObj];
                }
            }
        }
    }
}
