using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Hector.Core.Support.Web
{
    public static class WebDataRetriever
    {
        public static string RetrievePage(string pageUrl, Encoding encoding = null)
        {
            pageUrl.AssertNotNull("pageUrl");

            if(encoding.IsNull())
            {
                encoding = Encoding.UTF8;
            }

            string htmlCode = null;
            using (WebClient client = new WebClient())
            {
                client.Encoding = encoding;
                htmlCode = client.DownloadString(pageUrl);
            }

            return htmlCode;
        }

        public static bool TryDownloadFile(string fileUrl, string localFilePath)
        {
            fileUrl.AssertNotNull("fileUrl");
            localFilePath.AssertNotNull("localFilePath");

            bool succeded = true;

            using (WebClient client = new WebClient())
            {
                try
                {
                    client.Proxy = null;
                    client.DownloadFile(fileUrl, localFilePath);
                }
                catch(Exception)
                {
                    succeded = false;
                }
            }

            return succeded;
        }

        public static bool TryDownloadFile(string fileUrl, out byte[] fileContent)
        {
            fileUrl.AssertNotNull("fileUrl");

            bool succeded = true;
            fileContent = null;

            using (WebClient client = new WebClient())
            {
                try
                {
                    fileContent = client.DownloadData(fileUrl);
                }
                catch (Exception)
                {
                    succeded = false;
                }
            }

            return succeded;
        }

        public async static Task<byte[]> DownloadFileAsync(string fileUrl, IProgress<double> progress = null)
        {
            fileUrl.AssertNotNull("fileUrl");

            using (WebClient client = new WebClient())
            {
                client.DownloadProgressChanged += (sender, e) =>
                {
                    if (progress.IsNull())
                    {
                        return;
                    }

                    double bytesIn = e.BytesReceived.ConvertTo<double>();
                    double totalBytes = e.TotalBytesToReceive.ConvertTo<double>();
                    double percentage = bytesIn / totalBytes * 100;
                    progress.Report(percentage);
                };

                client.DownloadFileCompleted += (sender, e) =>
                {
                    if (progress.IsNull())
                    {
                        return;
                    }

                    progress.Report(100);
                };

                return await client.DownloadDataTaskAsync(new Uri(fileUrl));
            }
        }
    }
}
