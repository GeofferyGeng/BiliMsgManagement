using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;

namespace BiliMsgManagement
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            log.DataContext = _messageQueue;
        }

        private class msgObject
        {
            public Int64 num { get; set; }

            public Int64 uid { get; set; }

            public string stat { get; set; }

            public string uname { get; set; }

            public string isfollower { get; set; }

            public string utime { get; set; }

            public string msg { get; set; }

            public msgObject(Int64 num, string stat, Int64 uid, string uname, string isfollower, string utime, string msg)
            {
                this.num = num;
                this.stat = stat;
                this.uid = uid;
                this.uname = uname;
                this.isfollower = isfollower;
                this.utime = utime;
                this.msg = msg;
            }
        }

        private readonly ObservableCollection<string> _messageQueue = new ObservableCollection<string>();

        private TextBlock tbCell = null;

        private List<string> ls = new List<string>();

        private List<string> unreadls = new List<string>();

        private List<string> alllist = new List<string>();

        private Dictionary<string, msgObject> msgdictionary = new Dictionary<string, msgObject>();

        public static string COOKIE { get; set; }

        public static string CSRF { get; set; }

        public bool IsVerticalScrollBarAtBottom
        {
            get
            {
                bool atBottom = false;
                log.Dispatcher.Invoke(delegate
                {
                    double verticalOffset = log.VerticalOffset;
                    double viewportHeight = log.ViewportHeight;
                    double extentHeight = log.ExtentHeight;
                    if (verticalOffset + viewportHeight >= extentHeight)
                    {
                        atBottom = true;
                    }
                    else
                    {
                        atBottom = false;
                    }
                });
                return atBottom;
            }
        }

        private void get_bili_msg_click(object sender, RoutedEventArgs e)
        {
            try
            {
                string text = bcookietxt.Text;
                Dictionary<string, string> requestParameters = GetRequestParameters(text);
                CSRF = requestParameters["bili_jct"];
                COOKIE = bcookietxt.Text;
                logging("get cookie successfully!");
            }
            catch (Exception)
            {
                logging("get cookie failed！");
            }
            if (CSRF != null)
            {
                get_bili_info();
                get_bili_msg();
            }
        }

        private static Dictionary<string, string> GetRequestParameters(string row)
        {
            if (string.IsNullOrEmpty(row))
            {
                return null;
            }
            string[] array = Regex.Split(row, ";");
            if (array == null || array.Count() <= 0)
            {
                return null;
            }
            return array.ToDictionary((string e) => Regex.Split(e, "=")[0].Replace(" ", ""), (string e) => Regex.Split(e, "=")[1].Replace(" ", ""));
        }

        private void ListView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbCell = e.OriginalSource as TextBlock;
        }

        private void OnCopy(object sender, RoutedEventArgs e)
        {
            if (tbCell != null)
            {
                Clipboard.SetText(tbCell.Text);
            }
        }

        private void cbxxxx_Clicked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            string text = checkBox.Tag.ToString();
            if (checkBox.IsChecked == true)
            {
                ls.Add(text);
                logging(text + "is checked!");
            }
            else if (checkBox.IsChecked == false)
            {
                ls.Remove(text);
                logging(text + "is unchecked!");
            }
        }
        private void get_bili_info()
        {
            string url = "https://api.bilibili.com/x/space/myinfo";
            string httpResponse = GetHttpResponse(url, 1000);
            JObject jObject = JObject.Parse(httpResponse);
            string text = jObject["data"]["name"].ToString();
            tid.Text = text;
            string requestUriString = jObject["data"]["face"].ToString();
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(requestUriString);
            WebResponse response = httpWebRequest.GetResponse();
            System.Drawing.Image original = System.Drawing.Image.FromStream(response.GetResponseStream());
            Bitmap bitmap = new Bitmap(original);
            IntPtr hbitmap = bitmap.GetHbitmap();
            ImageSource source = Imaging.CreateBitmapSourceFromHBitmap(hbitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            face.Source = source;
            string url2 = "https://api.bilibili.com/x/web-interface/nav/stat";
            string httpResponse2 = GetHttpResponse(url2, 1000);
            JObject jObject2 = JObject.Parse(httpResponse2);
            string text2 = jObject2["data"]["following"].ToString();
            tfl.Text = text2;
            string text3 = jObject2["data"]["follower"].ToString();
            tfans.Text = text3;
            string url3 = "https://api.vc.bilibili.com/session_svr/v1/session_svr/single_unread?unread_type=0&build=0&mobi_app=web";
            string httpResponse3 = GetHttpResponse(url3, 1000);
            JObject jObject3 = JObject.Parse(httpResponse3);
            string text4 = ((Int64)jObject3["data"]["follow_unread"] + (Int64)jObject3["data"]["unfollow_unread"]).ToString();
            tmsgn.Text = text4;
            logging("name:" + text);
            logging("following:" + text2);
            logging("follower:" + text3);
            logging("unread msg:" + text4);
        }

        private void get_bili_msg()
        {
            msglistview.Items.Clear();
            Int64 num = 0;
            string text = Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds) + "000";
            string url = "https://api.vc.bilibili.com/session_svr/v1/session_svr/single_unread?unread_type=0&build=0&mobi_app=web";
            string httpResponse = GetHttpResponse(url, 1000);
            JObject jObject = JObject.Parse(httpResponse);
            Int64 num2 = (Int64)jObject["data"]["follow_unread"] + (Int64)jObject["data"]["unfollow_unread"];
            bool flag = true;
            Int64 num3 = 0;
            bool flag2 = true;
            flag2 = advanunreadmsg.IsChecked != true || num != num2;
            while (flag2 && flag)
            {
                string url2 = "https://api.vc.bilibili.com/session_svr/v1/session_svr/get_sessions?session_type=1&group_fold=1&unfollow_fold=" + Convert.ToInt32(advanfanmsg.IsChecked) + "&sort_rule=2&end_ts=" + text;
                string httpResponse2 = GetHttpResponse(url2, 1000);
                JObject jObject2 = JObject.Parse(httpResponse2);
                string text2 = jObject2["data"]["session_list"].ToString();
                Int64 num4 = jObject2["data"]["session_list"].Count();
                string text3 = "";
                for (int i = 0; i < num4; i++)
                {
                    string text4 = jObject2["data"]["session_list"][i]["talker_id"].ToString();
                    text3 += text4;
                    text3 += ",";
                }
                string url3 = "https://api.vc.bilibili.com/account/v1/user/infos?uids=" + text3.Substring(0, text3.Length - 1);
                string httpResponseWithoutCookie = GetHttpResponseWithoutCookie(url3, 1000);
                JObject jObject3 = JObject.Parse(httpResponseWithoutCookie);
                Dictionary<Int64, string> dictionary = new Dictionary<Int64, string>();
                Int64 num5 = jObject3["data"].Count();
                for (int j = 0; j < num5; j++)
                {
                    dictionary.Add((Int64)long.Parse(jObject3["data"][j]["mid"].ToString()), jObject3["data"][j]["uname"].ToString());
                }
                for (int k = 0; k < num4; k++)
                {
                    Int64 index = num3 * 20 + k + 1;
                    string stat = jObject2["data"]["session_list"][k]["unread_count"].ToString().Replace("0", "");
                    Int64 talker_id = (Int64)long.Parse(jObject2["data"]["session_list"][k]["talker_id"].ToString());
                    if (talker_id == 0)
                    {
                        continue;
                    }
                    if (Convert.ToBoolean((Int64)long.Parse(jObject2["data"]["session_list"][k]["unread_count"].ToString())))
                    {
                        num++;
                        unreadls.Add(talker_id.ToString());
                    }
                    if (!dictionary.ContainsKey(talker_id))
                    {
                        continue;
                    }
                    string uname = dictionary[talker_id];
                    string is_follow = Convert.ToBoolean((Int64)long.Parse(jObject2["data"]["session_list"][k]["is_follow"].ToString())).ToString().Replace("False", "");
                    string text7 = jObject2["data"]["session_list"][k]["session_ts"].ToString();
                    string utime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1)).AddMilliseconds((Int64)long.Parse(text7.Substring(0, 13))).ToString("yyyy-MM-dd HH:mm:ss");
                    string text8 = jObject2["data"]["session_list"][k]["last_msg"]["content"].ToString();
                    JObject jObject4 = JObject.Parse(text8);
                    string msg = "";
                    if (jObject4.ContainsKey("text"))
                    {
                        msg += jObject4["text"].ToString().Replace("\n", "");
                    }
                    if (jObject4.ContainsKey("content"))
                    {
                        msg += jObject4["content"].ToString().Replace("\n", "");
                    }
                    bool flag3 = true;
                    if (advantimeset.IsChecked == true && startdata.SelectedDate.HasValue && enddata.SelectedDate.HasValue)
                    {
                        string value = Convert.ToInt64((startdata.SelectedDate - new DateTime(1970, 1, 1, 0, 0, 0, 0)).Value.TotalMilliseconds) + "000";
                        string value2 = Convert.ToInt64((enddata.SelectedDate - new DateTime(1970, 1, 1, 0, 0, 0, 0)).Value.TotalMilliseconds) + "000";
                        if (Convert.ToInt64(text7) < Convert.ToInt64(value) || Convert.ToInt64(text7) > Convert.ToInt64(value2))
                        {
                            flag3 = false;
                        }
                    }
                    if (advanfanmsg.IsChecked == true && is_follow != "True")
                    {
                        flag3 = false;
                    }
                    if (advanunreadmsg.IsChecked == true)
                    {
                        string text9 = Convert.ToBoolean((Int64)long.Parse(jObject2["data"]["session_list"][k]["unread_count"].ToString())).ToString();
                        if (text9 != "True")
                        {
                            flag3 = false;
                        }
                    }
                    if (flag3)
                    {
                        msglistview.Items.Add(new msgObject(index, stat, talker_id, uname, is_follow, utime, msg));
                        msgdictionary[talker_id.ToString()] = new msgObject(index, stat, talker_id, uname, is_follow, utime, msg);
                        alllist.Add(talker_id.ToString());
                        logging("load " + talker_id + " " + uname);
                    }
                    text = text7;
                }
                num3++;
                flag = Convert.ToBoolean((Int64)long.Parse(jObject2["data"]["has_more"].ToString()));
                Thread.Sleep(20);
            }
        }

        public static string GetHttpResponse(string url, Int64 Timeout)
        {
            string cOOKIE = COOKIE;
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.Method = "GET";
            httpWebRequest.ContentType = "text/html;charset=UTF-8";
            httpWebRequest.Headers["cookie"] = cOOKIE;
            httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36";
            HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            Stream responseStream = httpWebResponse.GetResponseStream();
            StreamReader streamReader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
            string result = streamReader.ReadToEnd();
            streamReader.Close();
            responseStream.Close();
            return result;
        }

        public static string GetHttpResponseWithoutCookie(string url, Int64 Timeout)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.Method = "GET";
            httpWebRequest.ContentType = "text/html;charset=UTF-8";
            httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36";
            HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            Stream responseStream = httpWebResponse.GetResponseStream();
            StreamReader streamReader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
            string result = streamReader.ReadToEnd();
            streamReader.Close();
            responseStream.Close();
            return result;
        }

        private void show_msg_detail(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            string text = button.Tag.ToString();
            string url = "https://api.vc.bilibili.com/svr_sync/v1/svr_sync/fetch_session_msgs?talker_id=" + text + "&session_type=1&size=20";
            string httpResponse = GetHttpResponse(url, 1000);
            JObject jObject = JObject.Parse(httpResponse);
            int num = jObject["data"]["messages"].Count();
            string text2 = jObject["data"]["messages"][0]["receiver_id"].ToString() + "," + jObject["data"]["messages"][0]["sender_uid"].ToString();
            string url2 = "https://api.vc.bilibili.com/account/v1/user/infos?uids=" + text2;
            string httpResponseWithoutCookie = GetHttpResponseWithoutCookie(url2, 1000);
            JObject jObject2 = JObject.Parse(httpResponseWithoutCookie);
            Dictionary<Int64, string> dictionary = new Dictionary<Int64, string>();
            for (int i = 0; i < 2; i++)
            {
                dictionary.Add((Int64)long.Parse(jObject2["data"][i]["mid"].ToString()), jObject2["data"][i]["uname"].ToString());
            }
            string text3 = "";
            for (int j = 0; j < num; j++)
            {
                string s = jObject["data"]["messages"][num - 1 - j]["timestamp"].ToString();
                string text4 = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1)).AddSeconds(long.Parse(s)).ToString("yyyy-MM-dd HH:mm:ss");
                text3 = text3 + dictionary[(Int64)long.Parse(jObject["data"]["messages"][num - 1 - j]["sender_uid"].ToString())] + "-->" + dictionary[(Int64)long.Parse(jObject["data"]["messages"][num - 1 - j]["receiver_id"].ToString())] + "   " + text4 + "\n";
                string text5 = jObject["data"]["messages"][num - 1 - j]["content"].ToString();
                string text6;
                try
                {
                    JObject jObject3 = JObject.Parse(text5);
                    text6 = jObject3["content"].ToString();
                }
                catch (Exception)
                {
                    text6 = text5;
                }
                text3 += text6;
                text3 += "\n===========================================\n";
            }
            MsgDetail msgdetail2 = new MsgDetail(text3);
            msgdetail2.Show();
        }

        private void set_checked_read(object sender, RoutedEventArgs e)
        {
            Int64 num = ls.Count();
            string text = "https://api.vc.bilibili.com/session_svr/v1/session_svr/update_ack?talker_id=33271826&session_type=1&ack_seqno=1&build=0&mobi_app=web&csrf_token=f3a30df5edad95806cd2059b2c24805b&csrf=f3a30df5edad95806cd2059b2c24805b";
            for (int i = 0; i < num; i++)
            {
                string text2 = "https://api.vc.bilibili.com/session_svr/v1/session_svr/update_ack?";
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                dictionary["talker_id"] = ls[i];
                dictionary["session_type"] = "1";
                dictionary["ack_seqno"] = "1";
                dictionary["csrf_token"] = CSRF;
                dictionary["csrf"] = CSRF;
                dictionary["build"] = "0";
                dictionary["mobi_app"] = "web";
                string text3 = "";
                Int64 num2 = 0;
                foreach (string key in dictionary.Keys)
                {
                    if (num2 > 0)
                    {
                        text3 += "&";
                        text3 += key;
                        text3 += "=";
                        text3 += dictionary[key];
                    }
                    else
                    {
                        text3 += key;
                        text3 += "=";
                        text3 += dictionary[key];
                    }
                    num2++;
                }
                text2 += text3;
                string httpResponse = GetHttpResponse(text2, 1000);
                JObject jObject = JObject.Parse(httpResponse);
                if (jObject["code"].ToString() != "0")
                {
                    MessageBox.Show("Something is wrong with No." + ls[i]);
                    logging("Something is wrong with No." + ls[i]);
                }
                else
                {
                    logging(ls[i] + "had been read");
                }
            }
        }

        private void set_unread_read(object sender, RoutedEventArgs e)
        {
            Int64 num = unreadls.Count();
            string text = "https://api.vc.bilibili.com/session_svr/v1/session_svr/update_ack?talker_id=33271826&session_type=1&ack_seqno=1&build=0&mobi_app=web&csrf_token=f3a30df5edad95806cd2059b2c24805b&csrf=f3a30df5edad95806cd2059b2c24805b";
            for (int i = 0; i < num; i++)
            {
                string text2 = "https://api.vc.bilibili.com/session_svr/v1/session_svr/update_ack?";
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                dictionary["talker_id"] = unreadls[i];
                dictionary["session_type"] = "1";
                dictionary["ack_seqno"] = "1";
                dictionary["csrf_token"] = CSRF;
                dictionary["csrf"] = CSRF;
                dictionary["build"] = "0";
                dictionary["mobi_app"] = "web";
                string text3 = "";
                Int64 num2 = 0;
                foreach (string key in dictionary.Keys)
                {
                    if (num2 > 0)
                    {
                        text3 += "&";
                        text3 += key;
                        text3 += "=";
                        text3 += dictionary[key];
                    }
                    else
                    {
                        text3 += key;
                        text3 += "=";
                        text3 += dictionary[key];
                    }
                    num2++;
                }
                text2 += text3;
                string httpResponse = GetHttpResponse(text2, 1000);
                JObject jObject = JObject.Parse(httpResponse);
                if (jObject["code"].ToString() != "0")
                {
                    MessageBox.Show("Something is wrong with No." + unreadls[i]);
                    logging("Something is wrong with No." + unreadls[i]);
                }
                else
                {
                    logging(unreadls[i] + "had been read");
                }
            }
        }

        private void show_info_author(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("本工具由 @无华(uid=39991275) 制作 \n开源地址： https://github.com/GeofferyGeng/BiliMsgManagement； \n版本：0.1.1  2021.06.26 ", "关于");
        }

        private void output_checked(object sender, RoutedEventArgs e)
        {
            if (ls.Count == 0)
            {
                return;
            }
            string currentDirectory = Environment.CurrentDirectory;
            string fileName = Path.Combine(currentDirectory, "导出.txt");
            FileInfo fileInfo = new FileInfo(fileName);
            if (!fileInfo.Directory.Exists)
            {
                fileInfo.Directory.Create();
            }
            using (StreamWriter streamWriter = new StreamWriter(Path.Combine(currentDirectory, "导出.txt"), append: true))
            {
                streamWriter.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            for (int i = 0; i < ls.Count; i++)
            {
                string text = msgdictionary[ls[i]].uid.ToString();
                string text2 = msgdictionary[ls[i]].uname.ToString();
                string text3 = msgdictionary[ls[i]].msg.ToString();
                string text4 = msgdictionary[ls[i]].utime.ToString();
                using (StreamWriter streamWriter2 = new StreamWriter(Path.Combine(currentDirectory, "导出.txt"), append: true))
                {
                    streamWriter2.WriteLine(text + "   " + text2 + "   " + text4 + "   " + text3);
                    streamWriter2.WriteLine("---------------------------");
                }
                logging("output " + ls[i] + " successfully");
            }
        }

        private void output_all(object sender, RoutedEventArgs e)
        {
            if (msgdictionary.Count == 0)
            {
                return;
            }
            string currentDirectory = Environment.CurrentDirectory;
            string fileName = Path.Combine(currentDirectory, "导出.txt");
            FileInfo fileInfo = new FileInfo(fileName);
            if (!fileInfo.Directory.Exists)
            {
                fileInfo.Directory.Create();
            }
            using (StreamWriter streamWriter = new StreamWriter(Path.Combine(currentDirectory, "导出.txt"), append: true))
            {
                streamWriter.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            for (int i = 0; i < msgdictionary.Count; i++)
            {
                string text = msgdictionary[alllist[i]].uid.ToString();
                string text2 = msgdictionary[alllist[i]].uname.ToString();
                string text3 = msgdictionary[alllist[i]].msg.ToString();
                string text4 = msgdictionary[alllist[i]].utime.ToString();
                using (StreamWriter streamWriter2 = new StreamWriter(Path.Combine(currentDirectory, "导出.txt"), append: true))
                {
                    streamWriter2.WriteLine(text + "   " + text2 + "   " + text4 + "   " + text3);
                    streamWriter2.WriteLine("---------------------------");
                }
                logging("output " + alllist[i] + " successfully");
            }
        }

        public void logging(string text)
        {
            if (!log.Dispatcher.CheckAccess())
            {
                return;
            }
            lock (_messageQueue)
            {
                if (_messageQueue.Count >= 100)
                {
                    _messageQueue.RemoveAt(0);
                }
                _messageQueue.Add(DateTime.Now.ToString("T") + " : " + text);
            }
            bool flag = true;
            try
            {
                log.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " : " + text + Environment.NewLine);
                if (IsVerticalScrollBarAtBottom)
                {
                    log.ScrollToEnd();
                }
            }
            catch (Exception)
            {
                log.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " : Something is wrong! " + Environment.NewLine);
            }
        }
    }
}