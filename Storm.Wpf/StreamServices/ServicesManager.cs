using System;
using System.Collections.ObjectModel;
using System.Linq;
using Storm.Wpf.Streams;

namespace Storm.Wpf.StreamServices
{
    public static class ServicesManager
    {
        private static readonly Collection<StreamServiceBase> services = new Collection<StreamServiceBase>
        {
            new TwitchService(),
            //new YouTubeService(),
            new ChaturbateService(),
            new MixerService(),
            new MixlrService(),
            new SmashcastService()
        };

        public static StreamServiceBase GetService(Type streamType)
        {
            return services.SingleOrDefault(s => s.HandlesStreamType == streamType);
        }

        public static Action StartWatching(StreamBase stream)
        {
            if (stream is null) { throw new ArgumentNullException(nameof(stream)); }

            var service = GetService(stream.GetType()) is StreamServiceBase s ? s : null;

            return service?.GetWatchingInstructions(stream);
        }

        public static Action StartRecording(StreamBase stream)
        {
            if (stream is null) { throw new ArgumentNullException(nameof(stream)); }

            var service = GetService(stream.GetType()) is StreamServiceBase s ? s : null;

            return service?.GetRecordingInstructions(stream);
        }
    }
}
