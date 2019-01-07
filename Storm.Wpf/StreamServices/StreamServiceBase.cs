﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Storm.Wpf.Extensions;
using Storm.Wpf.Streams;

namespace Storm.Wpf.StreamServices
{
    public abstract class StreamServiceBase
    {
        protected abstract Uri ApiRoot { get; }
        protected virtual bool HasStreamlinkSupport { get; } = false;
        protected virtual bool HasYouTubeDlSupport { get; } = false;

        public abstract Type HandlesStreamType { get; }

        public abstract Task UpdateAsync(IEnumerable<StreamBase> streams);

        public Action GetWatchingInstructions(StreamBase stream)
        {
            if (HasStreamlinkSupport && HasFeature("streamlink"))
            {
                string command = $"/C streamlink {stream.AccountLink} best";

                var info = new ProcessStartInfo
                {
                    Arguments = command,
                    FileName = "powershell.exe",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = true // maybe remove
                };

                return new Action(() => Process.Start(info));
            }
            else
            {
                return stream.AccountLink.OpenInBrowser;
            }
        }

        public Action GetRecordingInstructions(StreamBase stream)
        {
            if (HasYouTubeDlSupport && HasFeature("youtube-dl"))
            {
                string command = $"/C youtube-dl {stream.AccountLink}";

                var info = new ProcessStartInfo
                {
                    Arguments = command,
                    FileName = "powershell.exe",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = true // maybe remove
                };

                return new Action(() => Process.Start(info));
            }
            else
            {
                return null;
            }
        }

        private static bool HasFeature(string command)
        {
            string path = Environment.GetEnvironmentVariable("Path");

            var cc = CultureInfo.CurrentCulture;

            bool hasFeature = cc.CompareInfo.IndexOf(path, command, CompareOptions.OrdinalIgnoreCase) > -1;

            return hasFeature;
        }

        public override bool Equals(object obj)
        {
            if (obj is StreamServiceBase service)
            {
                return HandlesStreamType == service.HandlesStreamType;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode() => HandlesStreamType.GetHashCode();
    }
}
