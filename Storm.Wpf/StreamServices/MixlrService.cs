using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Storm.Wpf.Common;
using Storm.Wpf.Streams;

namespace Storm.Wpf.StreamServices
{
    public class MixlrService : StreamServiceBase
    {

// we CAN get a live stream for VLC - https://api.mixlr.com/users/jeff-gerstmann (when live) -> broadcast_ids[] -> listen1.mixlr.com/{broadcast_id}

        protected override Uri ApiRoot { get; } = new Uri("https://api.mixlr.com/users");
        public override Type HandlesStreamType { get; } = typeof(MixlrStream);

        public MixlrService() { }

        public override Task UpdateAsync(IEnumerable<StreamBase> streams)
        {
            if (streams is null) { throw new ArgumentNullException(nameof(streams)); }
            if (!streams.Any()) { return Task.CompletedTask; }

            var updateTasks = new List<Task>(streams.Select(stream => UpdateMixlrStreamAsync(stream)));

            return Task.WhenAll(updateTasks);
        }

        private async Task UpdateMixlrStreamAsync(StreamBase stream)
        {
            Uri uri = new Uri($"{ApiRoot}/{stream.AccountName}");

            (string displayName, bool isLive) = await GetMixlrApiResponseAsync(uri);

            if (!String.IsNullOrEmpty(displayName))
            {
                stream.DisplayName = displayName;
            }

            stream.IsLive = isLive;
        }

        private static async Task<(string, bool)> GetMixlrApiResponseAsync(Uri uri)
        {
            (string, bool) failure = (string.Empty, false);

            (HttpStatusCode status, string text) = await Web.DownloadStringAsync(uri).ConfigureAwait(false);

            if (status != HttpStatusCode.OK) { return failure; }
            if (!Json.TryParse(text, out JObject json)) { return failure; }
            if (!json.HasValues) { return failure; }

            if (json.TryGetValue("username", out JToken usernameToken)
                && json.TryGetValue("is_live", out JToken isLiveToken))
            {
                string username = (string)usernameToken;
                bool isLive = (bool)isLiveToken;

                return (username, isLive);
            }

            return failure;
        }

        public override Action GetWatchingInstructions(StreamBase stream)
        {
            if (stream is null) { throw new ArgumentNullException(nameof(stream)); }

            return null;
        }
    }
}
