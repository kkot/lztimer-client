using Microsoft.CSharp;
using Newtonsoft.Json;
using System;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net;
using System.Net.Sockets;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace kkot.LzTimer
{
    interface WindowActivator
    {
        void ActivateInUiThread();
    }

    class TokenReceiver
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private string authorizationRequestUrl;

        private HttpListener httpListener;

        public String Token { get; private set; }

        private readonly WindowActivator windowActivator;

        public TokenReceiver(WindowActivator windowActivator)
        {
            this.windowActivator = windowActivator;
        }

        public void LogInWithGoogle()
        {
            if (this.httpListener == null)
            {
                var port = GetRandomUnusedPort();
                var redirectUri = string.Format("http://{0}:{1}/", IPAddress.Loopback, port);

                this.httpListener = new HttpListener();
                log.Info("Registering redirect URI " + redirectUri);
                httpListener.Prefixes.Add(redirectUri);
                httpListener.Start();

                var authorizationEndpoint = "http://localhost:8080/desktop/log_in";
                this.authorizationRequestUrl = string.Format("{0}?redirect_uri={1}&port={2}",
                    authorizationEndpoint, System.Uri.EscapeDataString(redirectUri), port);

                log.Info("Beginning listening for requests");
                httpListener.BeginGetContext(ProcessContext, httpListener);
            }

            log.Info("Opening browser " + authorizationRequestUrl);
            System.Diagnostics.Process.Start(authorizationRequestUrl);
        }

        public void InvalidateToken()
        {
            Token = null;
        }

        private int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private void ProcessContext(IAsyncResult result)
        {
            HttpListener listener = (HttpListener)result.AsyncState;
            var context = listener.EndGetContext(result);
            // Sends an HTTP response to the browser.  
            var response = context.Response;
            var request = context.Request;
            response.AppendHeader("Access-Control-Allow-Origin", "*");

            if (request.HttpMethod == "OPTIONS")
            {
                log.Info("Options request received");
                response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
                response.AddHeader("Access-Control-Allow-Methods", "GET, POST");
                response.AddHeader("Access-Control-Max-Age", "1728000");
                context.Response.StatusCode = 200;
                context.Response.OutputStream.Close();
                httpListener.BeginGetContext(ProcessContext, httpListener);
            }
            else
            {
                // TODO: check if POST
                log.Info("Post request received");

                // Brings this app back to the foreground.
                windowActivator.ActivateInUiThread();

                // Get the data from the HTTP stream
                var body = new StreamReader(context.Request.InputStream).ReadToEnd();
                log.Info("TokenJson: " + body);
                dynamic tokenJson = JsonConvert.DeserializeObject(body);
                this.Token = tokenJson.token;
                log.Info("Token: " + Token);

                string responseString = string.Format("ok");
                var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                var responseOutput = response.OutputStream;
                responseOutput.Write(buffer, 0, buffer.Length);
                responseOutput.Close();
                this.httpListener.Stop();
                this.httpListener = null;
                log.Info("HTTP server stopped.");
            }
        }
    }
}
