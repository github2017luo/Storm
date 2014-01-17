﻿using System.Net;
using Newtonsoft.Json.Linq;

namespace Storm
{
    class JustinStream : StreamBase
    {
        public JustinStream(string s)
            : base(s)
        {
            this._apiUri = "https://api.justin.tv/api";
        }

        public override void Update()
        {
            if (!this._hasUpdatedDisplayName)
            {
                this._hasUpdatedDisplayName = TrySetDisplayName();
            }

            // the url to query, the JSON from which we will determine if the user is live or not
            string streamApiAddress = string.Format("{0}/stream/summary.json?channel={1}", this._apiUri, this._name);

            HttpWebRequest updateRequest = BuildJustinHttpWebRequest(streamApiAddress);
            JObject apiResponse = GetApiResponse(updateRequest);

            if (apiResponse != null)
            {
                ProcessApiResponse(apiResponse);
            }
        }

        protected override bool TrySetDisplayName()
        {
            string apiAddressToQueryForDisplayName = string.Format("{0}/channel/show/{1}.json", this._apiUri, this._name);
            HttpWebRequest justinRequest = BuildJustinHttpWebRequest(apiAddressToQueryForDisplayName);
            JObject response = GetApiResponse(justinRequest);

            if (response != null)
            {
                this.DisplayName = (string)response["title"];
                return true;
            }

            return false;
        }

        private static HttpWebRequest BuildJustinHttpWebRequest(string fullApiRequestAddress)
        {
            HttpWebRequest req = HttpWebRequest.CreateHttp(fullApiRequestAddress);

            req.KeepAlive = false;
            req.Method = "GET";
            req.Referer = "justin.tv";
            req.Timeout = 2000;
            req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko";

            return req;
        }

        protected override void ProcessApiResponse(JObject jobj)
        {
            if (jobj != null)
            {
                if (jobj.HasValues)
                {
                    int streams_count = (int)jobj["streams_count"];

                    if (streams_count == 0)
                    {
                        if (this.IsLive == true)
                        {
                            this.IsLive = false;
                        }
                    }
                    else
                    {
                        if (this.IsLive == false)
                        {
                            this.IsLive = true;

                            this.OnHasGoneLive(this);
                        }
                    }
                }
            }
        }
    }
}
