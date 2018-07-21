using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Voto.Consolidate
{
    /// <summary>
    /// Interaction logic for WindowGoogleLogin.xaml
    /// </summary>
    public partial class WindowGoogleLogin : Window
    {
        private string email { get; set; }

        public class Photo
        {
            public string url { get; set; }
            public string id { get; set; }
            public string width { get; set; }
            public string height { get; set; }
            public string size { get; set; }
            public string title { get; set; }
            public string comment { get; set; }
            public DateTime timeStamp { get; set; }
        }

        public class Album
        {
            public string title { get; set; }
            public string id { get; set; }
            public bool isSelected { get; set; }
            public List<Photo> photos;
        }

        public List<Album> _albums { get; set; }

        public string cookies { get; set; }

        public WindowGoogleLogin()
        {
            _albums = new List<Album>();
            InitializeComponent();
        }

        internal sealed class NativeMethods
        {
            #region enums

            public enum ErrorFlags
            {
                ERROR_INSUFFICIENT_BUFFER = 122,
                ERROR_INVALID_PARAMETER = 87,
                ERROR_NO_MORE_ITEMS = 259
            }

            public enum InternetFlags
            {
                INTERNET_COOKIE_HTTPONLY = 8192, //Requires IE 8 or higher   
                INTERNET_COOKIE_THIRD_PARTY = 131072,
                INTERNET_FLAG_RESTRICTED_ZONE = 16
            }

            #endregion

            #region DLL Imports

            [SuppressUnmanagedCodeSecurity, SecurityCritical, DllImport("wininet.dll", EntryPoint = "InternetGetCookieExW", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
            internal static extern bool InternetGetCookieEx([In] string Url, [In] string cookieName, [Out] StringBuilder cookieData, [In, Out] ref uint pchCookieData, uint flags, IntPtr reserved);

            #endregion
        }


        /// <SUMMARY></SUMMARY>   
        /// WebBrowserCookie?   
        /// webBrowser1.Document.CookieHttpOnlyCookie   
        ///    
        private class FullWebBrowserCookie 
        {
            [SecurityCritical]
            public static string GetCookieInternal(Uri uri, bool throwIfNoCookie)
            {
                uint pchCookieData = 0;
                string url = UriToString(uri);
                uint flag = (uint)NativeMethods.InternetFlags.INTERNET_COOKIE_HTTPONLY;

                //Gets the size of the string builder   
                if (NativeMethods.InternetGetCookieEx(url, null, null, ref pchCookieData, flag, IntPtr.Zero))
                {
                    pchCookieData++;
                    StringBuilder cookieData = new StringBuilder((int)pchCookieData);

                    //Read the cookie   
                    if (NativeMethods.InternetGetCookieEx(url, null, cookieData, ref pchCookieData, flag, IntPtr.Zero))
                    {
                        DemandWebPermission(uri);
                        return cookieData.ToString();
                    }
                }

                int lastErrorCode = Marshal.GetLastWin32Error();

                if (throwIfNoCookie || (lastErrorCode != (int)NativeMethods.ErrorFlags.ERROR_NO_MORE_ITEMS))
                {
                    //throw new Win32Exception(lastErrorCode);
                }

                return null;
            }

            private static void DemandWebPermission(Uri uri)
            {
                string uriString = UriToString(uri);

                if (uri.IsFile)
                {
                    string localPath = uri.LocalPath;
                    new FileIOPermission(FileIOPermissionAccess.Read, localPath).Demand();
                }
                else
                {
                    new WebPermission(NetworkAccess.Connect, uriString).Demand();
                }
            }

            private static string UriToString(Uri uri)
            {
                if (uri == null)
                {
                    throw new ArgumentNullException("uri");
                }

                UriComponents components = (uri.IsAbsoluteUri ? UriComponents.AbsoluteUri : UriComponents.SerializationInfoString);
                return new StringBuilder(uri.GetComponents(components, UriFormat.SafeUnescaped), 2083).ToString();
            }
        }

        private void GetAlbums(string cookies)
        {
            var request = (HttpWebRequest)WebRequest.Create("https://picasaweb.google.com/data/feed/api/user/" + email);

            request.Headers.Add("Cookie: " + cookies);

            try
            {
                var response = (HttpWebResponse)request.GetResponse();

                using (var stream = response.GetResponseStream())
                {
                    if (stream == null) return;
                    using (var reader = new StreamReader(stream))
                    {
                        var responseString = reader.ReadToEnd();
                        responseString = responseString.Substring(responseString.IndexOf("<entry>", StringComparison.OrdinalIgnoreCase));
                        var entries = responseString.Split(new string[] { "</entry>" }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var entry in entries)
                        {
                            Album a = new Album();
                            var s = GetElementData(entry, "<gphoto:id>");
                            if (string.IsNullOrEmpty(s)) continue;
                            a.id = s;

                            var t = GetElementData(entry, "<gphoto:name>");
                            a.title = t;

                            GetAlbumPhotos(ref a);
                            _albums.Add(a);
                        }
                    }
                }
            }
            catch { }
        }

        private void GetAlbumPhotos(ref Album album)
        {
            //https://picasaweb.google.com/data/feed/api/user/userID/albumid/albumID

            album.photos = new List<Photo>();

            //Because the Picasa API only allows 1000 photos at a time we must do this
            var currentPhotoIndex = 1;
            var totalPhotoCount = 1000;
            while (currentPhotoIndex < totalPhotoCount)
            {
                var request =
                    (HttpWebRequest) WebRequest.Create("https://picasaweb.google.com/data/feed/api/user/" + email +
                                                       "/albumid/" + album.id + "?start-index=" + currentPhotoIndex +
                                                       "&amp;max-results=1000");

                request.Headers.Add("Cookie: " + cookies);

                var response = (HttpWebResponse) request.GetResponse();
                using (var stream = response.GetResponseStream())
                {
                    if (stream == null) return;
                    using (var reader = new StreamReader(stream))
                    {
                        var responseString = reader.ReadToEnd();
                        if (responseString.Contains("<entry>") == false) return;

                        totalPhotoCount = int.Parse(GetElementData(responseString, "<gphoto:numphotos>"));
                        currentPhotoIndex += 1000;

                        responseString =
                            responseString.Substring(
                                responseString.IndexOf("<entry>", StringComparison.OrdinalIgnoreCase));
                        var entries = responseString.Split(new string[] {"</entry>"},
                            StringSplitOptions.RemoveEmptyEntries);

                        foreach (var entry in entries)
                        {
                            Photo p = new Photo();
                            var i = GetElementData(entry, "<gphoto:id>");
                            if (i == album.id || string.IsNullOrEmpty(i)) continue;
                            p.id = i;

                            var w = GetElementData(entry, "<gphoto:width>");
                            p.width = w;

                            var h = GetElementData(entry, "<gphoto:height>");
                            p.height = h;

                            var s = GetElementData(entry, "<gphoto:size>");
                            p.size = s;

                            var t = GetElementData(entry, "<title type='text'>");
                            p.title = t;

                            var c = GetElementData(entry, "<summary type='text'>");
                            p.comment = c;

                            var time = GetElementData(entry, "<gphoto:timestamp>");
                            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                            dtDateTime = dtDateTime.AddMilliseconds(long.Parse(time));
                            p.timeStamp = dtDateTime;

                            album.photos.Add(p);
                        }
                    }
                }
            }
        }

        public string GetPhotoUrl(string albumId, string photoId)
        {
            var request =
                (HttpWebRequest) WebRequest.Create("https://picasaweb.google.com/data/feed/api/user/" + email +
                                                   "/albumid/" + albumId +
                                                   "/photoid/" + photoId +
                                                   "?imgmax=d");

            request.Headers.Add("Cookie: " + cookies);
            try
            {
                var response = (HttpWebResponse) request.GetResponse();
                using (var stream = response.GetResponseStream())
                {
                    if (stream == null) return "";
                    using (var reader = new StreamReader(stream))
                    {
                        var responseString = reader.ReadToEnd();
                        var i = 0;
                        var li = 0;
                        while (i != -1)
                        {
                            li = i;
                            i = responseString.IndexOf("<media:content url=", i + 1);
                        }

                        if (li == 0) return "";

                        var start = responseString.IndexOf('\'', li);
                        var end = responseString.IndexOf('\'', start + 1);

                        return responseString.Substring(start + 1, end - start - 1);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return "";
        }

        public List<string> GetAlbumPhotoUrls(string albumId)
        {
            List<string> photoUrls = new List<string>();

            foreach (var album in _albums)
            {
                if(album.id.Equals(albumId) == false) continue;

                foreach (var photo in album.photos)
                {
                    var p = GetPhotoUrl(album.id, photo.id);
                    if (string.IsNullOrEmpty(p)) continue;
                    photoUrls.Add(p);
                }
            }

            return photoUrls;
        }

        private string GetElementData(string text, string element, ref int index)
        {
            var start = text.IndexOf(element, index);
            if (start > -1)
            {
                index = start + 1;
                return text.Substring(start + element.Length, text.IndexOf('<', start + 1) - (start + element.Length));
            }

            index = -1;
            return "";
        }

        private string GetElementData(string text, string element)
        {
            int index = 0;
            return GetElementData(text, element, ref index);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            webBrowser1.Navigated += WebBrowser1_Navigated1;
            webBrowser1.SourceUpdated += WebBrowser1_SourceUpdated;

            webBrowser1.Navigate("https://accounts.google.com");
        }

        private void WebBrowser1_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            
        }

        private void WebBrowser1_Navigated1(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            dynamic doc = webBrowser1.Document;
            string htmlText = doc.documentElement.InnerHtml;

            if (htmlText.Contains("AccountSettingsLightUi"))
            {
                cookies = FullWebBrowserCookie.GetCookieInternal(webBrowser1.Source, false);

                var end = htmlText.IndexOf("@gmail.com");
                var start = end - 1;
                while (htmlText[start] != '\"') start--;
                start++;
                email = htmlText.Substring(start, end - start);

                GetAlbums(cookies);

                if(System.Windows.MessageBox.Show($"You have logged in under {email}@gmail.com.\nDo you want to close this window?", "Close window?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    this.Close();
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_albums.Count == 0)
            {
                cookies = FullWebBrowserCookie.GetCookieInternal(webBrowser1.Source, false);

                GetAlbums(cookies);
            }
        }
    }
}
