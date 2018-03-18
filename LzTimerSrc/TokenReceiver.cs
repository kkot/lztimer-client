using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace kkot.LzTimer
{
    interface IWindowActivator
    {
        void ActivateInUiThread();
    }

    class TokenReceiver
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private string authorizationRequestUrl;

        private HttpListener httpListener;

        public string Token
        {
            get => Properties.Settings.Default.Token;
            set
            {
                Properties.Settings.Default.Token = value;
                Properties.Settings.Default.Save();
            }
        }

        private readonly IWindowActivator windowActivator;

        private readonly IDataSenterSettingsProvider settingsProvider;

        public TokenReceiver(IWindowActivator windowActivator, IDataSenterSettingsProvider settingsProvider)
        {
            this.windowActivator = windowActivator;
            this.settingsProvider = settingsProvider;
        }

        public void LogInWithGoogle()
        {
            var address = settingsProvider.ServerAddress;
            if (address.Trim().Length == 0)
            {
                log.Error("Empty server name");
                return;
            }

            if (httpListener != null)
            {
                httpListener.Stop();
                httpListener.Close();
                httpListener = null;
            }

            var port = GetRandomUnusedPort();
            var redirectUri = $"http://{IPAddress.Loopback}:{port}/";

            httpListener = new HttpListener();
            log.Info("Registering redirect URI " + redirectUri);
            httpListener.Prefixes.Add(redirectUri);
            httpListener.Start();

            var authorizationEndpoint = $"{address}/desktop/log_in";
            authorizationRequestUrl = string.Format("{0}?redirect_uri={1}&port={2}",
                authorizationEndpoint, Uri.EscapeDataString(redirectUri), port);

            log.Info("Beginning listening for requests");
            httpListener.BeginGetContext(ProcessContext, httpListener);

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
            var port = ((IPEndPoint) listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private void ProcessContext(IAsyncResult result)
        {
            var listener = (HttpListener) result.AsyncState;
            if (!listener.IsListening)
            {
                return;
            }

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
                Token = tokenJson.token;
                log.Info("Token: " + Token);

                var responseString = "ok";
                var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                var responseOutput = response.OutputStream;
                responseOutput.Write(buffer, 0, buffer.Length);
                responseOutput.Close();
                httpListener.Stop();
                httpListener = null;
                log.Info("HTTP server stopped.");
            }
        }
    }
}