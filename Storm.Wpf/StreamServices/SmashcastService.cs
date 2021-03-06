﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Storm.Wpf.Common;
using Storm.Wpf.Streams;

namespace Storm.Wpf.StreamServices
{
    public class SmashcastService : StreamServiceBase
    {
        protected override Uri ApiRoot { get; } = new Uri("https://api.smashcast.tv");
        public override Type HandlesStreamType { get; } = typeof(SmashcastStream);

        public SmashcastService() { }

        public override Task UpdateAsync(IEnumerable<StreamBase> streams)
        {
            if (streams is null) { throw new ArgumentNullException(nameof(streams)); }
            if (!streams.Any()) { return Task.CompletedTask; }

            var updateTasks = new List<Task>(streams.Select(stream => UpdateSmashcastStreamAsync(stream)));

            return Task.WhenAll(updateTasks);
        }

        private async Task UpdateSmashcastStreamAsync(StreamBase stream)
        {
            Uri uri = new Uri($"{ApiRoot}/user/{stream.AccountName}");

            stream.IsLive = await GetSmashcastApiResponse(uri);
        }

        private static async Task<bool> GetSmashcastApiResponse(Uri uri)
        {
            (HttpStatusCode status, string rawJson) = await Web.DownloadStringAsync(uri).ConfigureAwait(false);

            if (status != HttpStatusCode.OK) { return false; }
            if (!Json.TryParse(rawJson, out JObject json)) { return false; }
            if (!json.HasValues) { return false; }

            if (json.TryGetValue("is_live", out JToken isLiveToken))
            {
                // "is_live":"0" means not live
                // "is_live":"1" means live
                // hence the cast to int, then converting to bool

                return Convert.ToBoolean((int)isLiveToken);
            }

            return false;
        }
    }
}
