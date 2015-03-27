using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Drawing;
using System.Net;
using System.Web;
using System.Reflection;

/*! \mainpage thumbalizr-csharp is a free and open-source library for the Thumablizr API (https://api.thumbalizr.com/).
 * <p>For examples on how to use the library, take a look at the unit tests.</p>
 * <p>Before you use this library, please take a look at the API documentation 
 * at https://api.thumbalizr.com/.<br /></p>
 * <p>The source code can be found at https://github.com/juliensobrier/thumbalizr-csharp. Patches are welcome!/p>
 * Announcements about the API and the libraries are on our blog at http://blog.thumbalizr.com/
 * <p>The latest documentation for borwshot-csharp can be found at http://juliensobrier.github.com/thumbalizr-csharp/</p>
 * */

namespace Thumbalizr
{
    /// <summary>
    /// c# client to interact with the Thumbalizr API. See https://api.thumbalizr.com/ for information about the API.
    /// </summary>
    public class Client
    {
        private string baseUrl = @"https://api.thumbalizr.com/";

        #region /// @name Constructors


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">API Key</param>
        /// <param name="debug">Debug flag</param>
        public Client(string key = "", bool debug = false)
        {
            this.Key = key;
            this.Debug = debug;
        }

        #endregion

        #region /// @name Properties

        private string key = String.Empty;
        /// <summary>
        /// API key
        /// </summary>
        public string Key
        {
            get
            {
                return this.key;
            }

            set
            {
                this.key = value;
            }
        }

        private bool debug = false;
        /// <summary>
        /// Debug flag (not used currently)
        /// </summary>
        public bool Debug
        {
            get
            {
                return this.debug;
            }

            set
            {
                this.debug = value;
            }
        }

        #endregion

        /// <summary>
        /// Request a screenshot. All paarameters,. expect for the URL, are optional
        /// </summary>
        /// <param name="url">URL of the page to laod</param>
        /// <param name="width">Thumbnail width</param>
        /// <param name="quality">Jpeg quality (0-100)</param>
        /// <param name="encoding">Thumbnail format (PNG or JPEG)</param>
        /// <param name="mode">Screeen or ful page</param>
        /// <param name="generate">Force a new screenshot</param>
        /// <param name="delay">Delay after page load event</param>
        /// <param name="bwidth">Browser width</param>
        /// <param name="bheight">Browser height</param>
        /// <returns></returns>
        public Result Screenshot(string url, int width = 0, int quality = 0, Encoding encoding = Encoding.Jpg, Mode mode = Mode.Screen, 
            bool generate = false, int delay = 0, int bwidth = 0, int bheight = 0)
        {
            Hashtable arguments = new Hashtable();

            arguments.Add("url", url);

            if (width > 0)
            {
                arguments.Add("width", width);
            }

            if (quality > 0)
            {
                arguments.Add("quality", quality);
            }
            
            string format = "jpg";
            if (encoding == Encoding.Png)
                format = "png";
            arguments.Add("encoding", format);

            string size = "screen";
            if (mode == Mode.Page)
                size = "page";
            arguments.Add("mode", size);

            int force = 0;
            if (generate)
                force = 1;
            arguments.Add("generate", force);

            if(delay > 0)
            {
                arguments.Add("delay", delay);
            }

            if (bwidth > 0)
            {
                arguments.Add("bwidth", bwidth);
            }

            if (delay > 0)
            {
                arguments.Add("bheight", bheight);
            }



            Result result = new Result();
            result.error = String.Empty;
            result.url = url;
            result.encoding = encoding; // Get it from server response mime type
            result.status = Status.Processing;
            result.thumbnail = null;
            result.generated = DateTime.Now;

            HttpWebResponse reply = Reply(baseUrl, arguments);

            if (reply == null)
            {
                return result;
            }

            try
            {
                result.url = reply.GetResponseHeader("X-Thumbalizr-URL");

                string content = reply.GetResponseHeader("Content-Type");
                if (content.IndexOf("/jpeg") > 0)
                {
                    result.encoding = Encoding.Jpg;
                }
                else
                {
                    result.encoding = Encoding.Png;
                }


                string header = reply.GetResponseHeader("X-Thumbalizr-Status");
                if (this.Debug)
                    Console.WriteLine("X-Thumbalizr-Status: " + header);

                if (header.ToLower() == "queued")
                {
                    result.status = Status.Processing;
                }
                else if (header.ToLower() == "ok")
                {
                    result.status = Status.Finished;
                    result.generated = DateTime.Parse(reply.GetResponseHeader("X-Thumbalizr-Generated"));
                }
                else if (header.ToLower() == "failed")
                {
                    result.status = Status.Error;
                    result.error = reply.GetResponseHeader("X-Thumbalizr-Error");
                }

                using (Stream responseStream = reply.GetResponseStream())
                {
                    //Do not close the stream, this creates an error when saving a JPEG file
                    MemoryStream memoryStream = new MemoryStream();
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    do
                    {
                        bytesRead = responseStream.Read(buffer, 0, buffer.Length);
                        memoryStream.Write(buffer, 0, bytesRead);
                    } while (bytesRead != 0);

                    result.thumbnail = Image.FromStream(memoryStream);
                }

            }
            catch(Exception e)
            {
                if (this.Debug)
                    Console.WriteLine(e);
            }

            reply.Close();
            return result;
        }


        private HttpWebResponse Reply(string url, Hashtable arguments)
        {
            Uri uri = MakeUrl(url, arguments);

            if (this.Debug)
                Console.WriteLine(uri.ToString());

            HttpWebRequest request = HttpWebRequest.CreateHttp(uri);
            request.AllowAutoRedirect = true;
            request.UserAgent = String.Format("Thumbalizr-sharp {0}", typeof(Client).Assembly.GetName().Version);

            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                Console.WriteLine(ex);

                HttpWebResponse errorResponse = ex.Response as HttpWebResponse;
                if (errorResponse == null)
                {
                    if (this.Debug)
                        Console.WriteLine(ex);

                    return null;
                }

                if (errorResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    Console.WriteLine("404: " + url);
                }
                else if (errorResponse.StatusCode == HttpStatusCode.Forbidden)
                {
                    Console.WriteLine("403: " + url);
                }
                else if (errorResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("401: " + url);
                }
                else
                {
                    if (this.Debug)
                        Console.WriteLine(errorResponse.StatusDescription);

                    return null;
                }

                response = errorResponse;
            }
           

            return response;

        }

        private Uri MakeUrl(string url, Hashtable arguments)
        {
            UriBuilder builder = new UriBuilder(url);

            if (arguments == null)
                arguments = new Hashtable();

            if (key != String.Empty)
                arguments.Add("key", this.Key);

            if (arguments != null)
            {
                StringBuilder query = new StringBuilder();
                foreach (DictionaryEntry pair in arguments)
                {
                    query.Append("&");
                    query.Append(HttpUtility.UrlEncode(pair.Key.ToString()));
                    query.Append("=");
                    query.Append(HttpUtility.UrlEncode(pair.Value.ToString()));
                }

                builder.Query = query.ToString();
            }

            return builder.Uri;
        }

    }

    public enum Status { Finished, Processing, Error };
    public enum Encoding {  Png, Jpg};
    public enum Mode { Screen, Page };

    /// <summary>
    /// Result of a Thumbalizr API call
    /// </summary>
    public struct Result
    {
        internal Status status;

        /// <summary>
        /// Status of the screenshot
        /// </summary>
        public Status Status
        {
            get
            {
                return status;
            }
        }

        internal string error;
        public string Error
        {
            get
            {
                return error;
            }
        }

        internal DateTime generated;

        public DateTime Generated
        {
            get
            {
                return generated;
            }
        }


        internal Image thumbnail;

        /// <summary>
        /// Screenshot image
        /// </summary>
        public Image Thumbnail
        {
            get
            {
                return thumbnail;
            }
        }

        internal string url;

        public String Url
        {
            get
            {
                return url;
            }
        }

        internal Encoding encoding;

        public Encoding Encoding {
            get
            {
                return encoding;
            }
        }

        /// <summary>
        /// Save thumbnail to disk
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string Save(string path = "")
        {
            if (thumbnail == null)
            {
                return String.Empty;
            }

            string ext = "jpg";
            if (encoding == Encoding.Png)
            {
                ext = "jpg";
            }

            if(path == null || path == String.Empty)
            {
                if (url == null || url == String.Empty)
                {
                    return String.Empty;
                }

                Uri uri = new Uri(url);
                path = String.Format("{0}.{1}", uri.Host, ext);                
            }

            string filename = path;

            if (Directory.Exists(path))
            {
                Uri uri = new Uri(url);
                filename = System.IO.Path.Combine(path, String.Format("{0}.{1}", uri.Host, ext));
            }

            thumbnail.Save(filename);

            return filename;
        }
    }
}
