using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;

namespace MCLauncher {

    class BadUpdateIdentityException: ArgumentException{
        public BadUpdateIdentityException() : base("Bad updateIdentity") { }
    }

    class VersionDownloader {

        private HttpClient client = new HttpClient();
        private WUProtocol protocol = new WUProtocol();
        
        private async Task<XDocument> PostXmlAsync(string url, XDocument data) {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
            using (var stringWriter = new StringWriter()) {
                using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = false, OmitXmlDeclaration = true })) {
                    data.Save(xmlWriter);
                }
                request.Content = new StringContent(stringWriter.ToString(), Encoding.UTF8, "application/soap+xml");
            }
            using (var resp = await client.SendAsync(request)) {
                string str = await resp.Content.ReadAsStringAsync();
                return XDocument.Parse(str);
            }
        }

        private long? NormalizeTotalSize(long totalSize) {
            return totalSize > 0 ? (long?)totalSize : null;
        }

        private async Task DownloadFile(string url, string to, int chunkCount, DownloadProgress progress, CancellationToken cancellationToken) {
            int sanitizedChunkCount = Math.Max(1, chunkCount);
            var downloadOpt = new Downloader.DownloadConfiguration() {
                BufferBlockSize = 1024 * 1024,
                ChunkCount = sanitizedChunkCount,
                ParallelCount = sanitizedChunkCount,
                ParallelDownload = sanitizedChunkCount > 1,
                ClearPackageOnCompletionWithFailure = true
            };
            var downloader = new Downloader.DownloadService(downloadOpt);
            AsyncCompletedEventArgs downloadCompletedArgs = null;

            downloader.DownloadStarted += (sender, args) => {
                var startedArgs = args as Downloader.DownloadStartedEventArgs;
                progress(0, NormalizeTotalSize(startedArgs?.TotalBytesToReceive ?? 0));
            };
            downloader.DownloadProgressChanged += (sender, args) => {
                var progressArgs = args as Downloader.DownloadProgressChangedEventArgs;
                if (progressArgs != null) {
                    progress(progressArgs.ReceivedBytesSize, NormalizeTotalSize(progressArgs.TotalBytesToReceive));
                }
            };
            downloader.DownloadFileCompleted += (sender, args) => {
                downloadCompletedArgs = args as AsyncCompletedEventArgs;
            };

            if (File.Exists(to)) {
                File.Delete(to);
            }

            await downloader.DownloadFileTaskAsync(url, to, cancellationToken);

            if (downloadCompletedArgs == null) {
                throw new Exception("Download completed without reporting a final status.");
            }
            if (downloadCompletedArgs.Cancelled || cancellationToken.IsCancellationRequested) {
                throw new TaskCanceledException("Download cancelled.");
            }
            if (downloadCompletedArgs.Error != null) {
                throw downloadCompletedArgs.Error;
            }
        }

        private async Task<string> GetDownloadUrl(string updateIdentity, string revisionNumber) {
            XDocument result = await PostXmlAsync(protocol.GetDownloadUrl(),
                protocol.BuildDownloadRequest(updateIdentity, revisionNumber));
            Debug.WriteLine($"GetDownloadUrl() response for updateIdentity {updateIdentity}, revision {revisionNumber}:\n{result.ToString()}");
            foreach (string s in protocol.ExtractDownloadResponseUrls(result)) {
                if (s.StartsWith("http://tlu.dl.delivery.mp.microsoft.com/"))
                    return s;
            }
            return null;
        }

        public void EnableUserAuthorization() {
            protocol.SetMSAUserToken(WUTokenHelper.GetWUToken());
        }

        public async Task DownloadAppx(string updateIdentity, string revisionNumber, string destination, int chunkCount, DownloadProgress progress, CancellationToken cancellationToken) {
            string link = await GetDownloadUrl(updateIdentity, revisionNumber);
            if (link == null)
                throw new BadUpdateIdentityException();
            Debug.WriteLine("Resolved download link: " + link);
            await DownloadFile(link, destination, chunkCount, progress, cancellationToken);
        }

        public async Task DownloadMsixvc(string downloadUrl, string destination, int chunkCount, DownloadProgress progress, CancellationToken cancellationToken) {
            await DownloadFile(downloadUrl, destination, chunkCount, progress, cancellationToken);
        }

        public delegate void DownloadProgress(long current, long? total);



    }
}
