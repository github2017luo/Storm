﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Cache;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Storm.ViewModels;

namespace Storm
{
    class Ustream : StreamBase
    {
        #region Fields
        private string channelId = string.Empty;
        #endregion

        #region Properties
        public override string MouseOverTooltip
        {
            get
            {
                if (this.IsLive)
                {
                    return string.Format("{0} is live", this.DisplayName);
                }
                else
                {
                    return string.Format("{0} is offline", this.DisplayName);
                }
            }
        }
        #endregion

        public Ustream(Uri u)
            : base(u)
        {
            this.apiUri = "https://api.ustream.tv";
        }

        public async override Task UpdateAsync()
        {
            this.Updating = true;

            List<Task> updateTasks = new List<Task>
            {
                DetermineIfLive(),
                DetermineChannelId()
            };

            bool wasLive = IsLive;

            await Task.WhenAll(updateTasks).ConfigureAwait(false);

            if (wasLive == false && IsLive == true)
            {
                NotifyIsNowLive();
            }

            this.Updating = false;
        }

        protected async override Task DetermineIfLive()
        {
            if (String.IsNullOrEmpty(channelId))
            {
                channelId = await DetermineChannelId();
            }

            string apiAddressToQuery = string.Format("{0}/channels/{1}.json", this.apiUri, channelId);
            HttpWebRequest req = BuildUstreamHttpWebRequest(new Uri(apiAddressToQuery));

            JObject resp = await GetApiResponseAsync(req).ConfigureAwait(false);

            if (resp != null)
            {
                if (resp["channel"].HasValues)
                {
                    if (this.hasUpdatedDisplayName == false)
                    {
                        SetDisplayName(resp);
                    }

                    IsLive = WasUserLive(resp);
                }
            }
        }

        private async Task<string> DetermineChannelId()
        {
            HttpWebRequest req = BuildUstreamHttpWebRequest(this.Uri);
            string response = await Utils.DownloadWebsiteAsStringAsync(req).ConfigureAwait(false);

            if (String.IsNullOrWhiteSpace(response))
            {
                return null;
            }
            else
            {
                string beginning = "<div class=\"viewer-bg\" data-content-id=\"";
                string ending = "\" data-content-type=\"channel\"><div class=\"transparent-bg\"></div></div>";

                string id = response.FromBetween(beginning, ending);

                return id;
            }
        }

        private void SetDisplayName(JObject resp)
        {
            string displayName = (string)resp["channel"]["title"];

            if (String.IsNullOrEmpty(displayName) == false)
            {
                this.DisplayName = displayName;

                this.hasUpdatedDisplayName = true;
            }
        }

        private bool WasUserLive(JObject resp)
        {
            string statusValue = (string)resp["channel"]["status"];

            if (String.IsNullOrEmpty(statusValue) == false)
            {
                if (statusValue.Equals("live"))
                {
                    return true;
                }
            }

            return false;
        }

        protected override void NotifyIsNowLive()
        {
            string title = string.Format("{0} is now live", this.DisplayName);

            NotificationService.Send(title, new Action(() => MainWindowViewModel.GoToStream(this)));
        }

        private static HttpWebRequest BuildUstreamHttpWebRequest(Uri uri)
        {
            HttpWebRequest req = HttpWebRequest.CreateHttp(uri);

            req.Accept = "application/json; charset=UTF-8";
            req.AllowAutoRedirect = true;
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            req.CachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
            req.Host = uri.DnsSafeHost;
            req.KeepAlive = false;
            req.Method = "GET";
            req.ProtocolVersion = HttpVersion.Version11;
            req.Referer = string.Format("{0}://{1}", uri.GetLeftPart(UriPartial.Scheme), uri.DnsSafeHost);
            req.Timeout = 2500;
            req.UserAgent = "IE11: Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko";

            if (uri.Scheme.Equals("https"))
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            }

            req.Headers.Add("DNT", "1");
            req.Headers.Add("Accept-Encoding", "gzip, deflate");

            return req;
        }
    }
}
