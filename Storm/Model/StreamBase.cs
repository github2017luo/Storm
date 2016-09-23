﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Storm.ViewModels;

namespace Storm.Model
{
    public abstract class StreamBase : ViewModelBase
    {
        #region Fields
        private string _apiUri = string.Empty;
        protected string ApiUri
        {
            get
            {
                return _apiUri;
            }
            set
            {
                _apiUri = value;
            }
        }

        private bool _hasUpdatedDisplayName = false;
        protected bool HasUpdatedDisplayName
        {
            get
            {
                return _hasUpdatedDisplayName;
            }
            set
            {
                _hasUpdatedDisplayName = value;
            }
        }
        #endregion

        #region Properties
        public abstract BitmapImage Icon { get; }

        public virtual string MouseOverTooltip
        {
            get
            {
                return IsLive
                    ? string.Format(CultureInfo.CurrentCulture, "{0} is LIVE", DisplayName)
                    : string.Format(CultureInfo.CurrentCulture, "{0} is offline", DisplayName);
            }
        }
        
        private Uri _uri = null;
        public Uri Uri
        {
            get
            {
                return _uri;
            }
            protected set
            {
                if (_uri != value)
                {
                    _uri = value;

                    RaisePropertyChanged(nameof(Uri));
                }
            }
        }

        private string _name = string.Empty;
        public string Name
        {
            get
            {
                return _name;
            }
            protected set
            {
                if (_name != value)
                {
                    _name = value;

                    RaisePropertyChanged(nameof(Name));
                }
            }
        }

        private string _displayName = string.Empty;
        public string DisplayName
        {
            get
            {
                return String.IsNullOrEmpty(_displayName) ? Name : _displayName;
            }
            protected set
            {
                if (_displayName != value)
                {
                    _displayName = value;

                    RaisePropertyChanged(nameof(DisplayName));
                    RaisePropertyChanged(nameof(MouseOverTooltip));
                }
            }
        }

        private bool _isLive = false;
        public bool IsLive
        {
            get
            {
                return _isLive;
            }
            protected set
            {
                if (_isLive != value)
                {
                    _isLive = value;

                    RaisePropertyChanged(nameof(IsLive));
                    RaisePropertyChanged(nameof(MouseOverTooltip));
                }
            }
        }
        
        private bool _updating = false;
        public bool Updating
        {
            get
            {
                return _updating;
            }
            set
            {
                if (_updating != value)
                {
                    _updating = value;

                    RaisePropertyChanged(nameof(Updating));
                }
            }
        }
        
        private readonly bool _hasLivestreamerSupport = false;
        public bool HasLivestreamerSupport { get; protected set; }
        #endregion

        protected StreamBase(Uri accountUri)
        {
            if (accountUri == null)
            {
                throw new ArgumentNullException(nameof(accountUri), "StreamBase ctor was passed a null uri");
            }

            Uri = accountUri;
            Name = SetAccountName(accountUri);
        }

        protected static bool IsLivestreamerOnPath()
        {
            string path = Environment.GetEnvironmentVariable("Path");

            return CultureInfo
                .CurrentCulture
                .CompareInfo
                .IndexOf(path, "livestreamer", CompareOptions.IgnoreCase) > -1;
        }

        protected static async Task<object> GetApiResponseAsync(HttpWebRequest request, bool isJson)
        {
            if (request == null) { throw new ArgumentNullException(nameof(request)); }

            string response = await Utils.DownloadWebsiteAsStringAsync(request).ConfigureAwait(false);
            
            if (String.IsNullOrEmpty(response)){ return null; }

            if (isJson)
            {
                return ParseToJson(response);
            }
            else
            {
                return response;
            }
        }
        
        private static JObject ParseToJson(string response)
        {
            if (response == null) { throw new ArgumentNullException(nameof(response)); }

            JObject j = null;

            try
            {
                j = JObject.Parse(response);
            }
            catch (JsonReaderException e)
            {
                Utils.LogException(e);
            }

            return j;
        }

        protected virtual string SetAccountName(Uri uri)
        {
            if (uri == null) { throw new ArgumentNullException(nameof(uri)); }
            
            return uri.Segments[1]; // [0] is the first slash
        }

        public abstract Task UpdateAsync();
        protected abstract Task DetermineIfLiveAsync();

        public virtual void NotifyIsNowLive(string serviceName)
        {
            string title = string.Format(CultureInfo.CurrentCulture, "{0} is LIVE", DisplayName);
            string description = string.Format(CultureInfo.CurrentCulture, "on {0}", serviceName);

            Action action = () => Utils.OpenUriInBrowser(Uri);

            NotificationService.Send(title, description, action);
        }

        public virtual void GoToStream()
        {
            if (HasLivestreamerSupport && IsLivestreamerOnPath())
            {
                LaunchLiveStreamer();
            }
            else
            {
                Utils.OpenUriInBrowser(Uri);
            }
        }

        private void LaunchLiveStreamer()
        {
            string args = string.Format(CultureInfo.CurrentCulture, @"/C livestreamer.exe {0} best", Uri.AbsoluteUri);

            ProcessStartInfo pInfo = new ProcessStartInfo
            {
                Arguments = args,
                FileName = "cmd.exe",
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(pInfo);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(GetType().ToString());
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "Uri: {0}", Uri));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "Name: {0}", Name));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "Display name: {0}", DisplayName));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "Is Live: {0}", IsLive.ToString()));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "MouseOverToolTip: {0}", MouseOverTooltip));

            return sb.ToString();
        }

#if DEBUG
        public void DEBUG_toggle_is_live()
        {
            IsLive = !IsLive;
        }
#endif
    }
}
