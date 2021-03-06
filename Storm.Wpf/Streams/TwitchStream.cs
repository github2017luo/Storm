﻿using System;
using System.Text;
using Storm.Wpf.Common;
using Storm.Wpf.StreamServices;

namespace Storm.Wpf.Streams
{
    public class TwitchStream : StreamBase
    {
        protected override string ServiceName { get; } = "Twitch";

        public override string MouseOverToolTip
        {
            get
            {
                if (IsLive && !String.IsNullOrWhiteSpace(Game))
                {
                    return $"{DisplayName} is playing {Game}";
                }
                else
                {
                    return base.MouseOverToolTip;
                }
            }
        }

        private static readonly Uri _icon = new Uri($"{IconPackPrefix}Twitch.ico");
        public override Uri Icon => _icon;

        public Int64 UserId { get; set; }

        private string _game = string.Empty;
        public string Game
        {
            get => _game;
            set
            {
                SetProperty(ref _game, value, nameof(Game));

                RaisePropertyChanged(nameof(MouseOverToolTip));
            }
        }

        public TwitchStream(Uri account)
            : base(account)
        { }

        protected override void NotifyLive()
        {
            string title = $"{DisplayName} is LIVE";
            string description = String.IsNullOrWhiteSpace(Game) ? string.Empty : $"and playing {Game}";

            Action startWatching = ServicesManager.StartWatching(this);

            NotificationService.Send(title, description, startWatching);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(base.ToString());

            sb.AppendLine(String.IsNullOrWhiteSpace(Game) ? "no game" : $"game: {Game}");

            return sb.ToString();
        }
    }
}
