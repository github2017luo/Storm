﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Storm.Model;

namespace Storm.DataAccess
{
    public class TxtRepo : IRepository
    {
        private string _filePath = string.Empty;
        public string FilePath { get { return _filePath; } }
        
        public TxtRepo(string filePath)
        {
            if (String.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("filePath was null or whitespace");
            }

            _filePath = filePath;
        }

        public void SetFilePath(string newPath)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<StreamBase>> LoadAsync()
        {
            List<StreamBase> streams = new List<StreamBase>();

            FileStream fsAsync = new FileStream(FilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.None,
                2048,
                true);

            try
            {
                using (StreamReader sr = new StreamReader(fsAsync))
                {
                    fsAsync = null;

                    string line = string.Empty;

                    while ((line = await sr.ReadLineAsync().ConfigureAwait(false)) != null)
                    {
                        StreamBase sb = ParseIntoStream(line);

                        if (sb != null)
                        {
                            streams.Add(sb);
                        }
                    }
                }
            }
            catch (FileNotFoundException e)
            {
                Utils.LogException(e);
            }
            finally
            {
                fsAsync?.Dispose();
            }

            return streams;
        }

        private static StreamBase ParseIntoStream(string line)
        {
            // # means line is a comment
            if (line.StartsWith("#", StringComparison.CurrentCultureIgnoreCase)) { return null; }

            // if this is not done subsequent Uri.TryCreate will fail
            if (line.StartsWith("http://", StringComparison.CurrentCultureIgnoreCase) == false
                && line.StartsWith("https://", StringComparison.CurrentCultureIgnoreCase) == false)
            {
                line = string.Concat("http://", line);
            }

            // C# 7 ALERT - use new feature
            // Uri.TryCreate(line, UriKind.Absolute, out Uri uri))
            Uri uri = null;
            if (!Uri.TryCreate(line, UriKind.Absolute, out uri))
            {
                return new UnsupportedService("invalid Uri");
            }

            return DetermineStreamingService(uri);
        }
        
        private static StreamBase DetermineStreamingService(Uri uri)
        {
            StreamBase sb = null;

            switch (uri.DnsSafeHost)
            {
                case "twitch.tv":
                    sb = new Twitch(uri);
                    break;
                case "ustream.tv":
                    sb = new Ustream(uri);
                    break;
                case "mixlr.com":
                    sb = new Mixlr(uri);
                    break;
                case "hitbox.tv":
                    sb = new Hitbox(uri);
                    break;
                case "beam.pro":
                    sb = new Beam(uri);
                    break;
                case "chaturbate.com":
                    sb = new Chaturbate(uri);
                    break;
                default:
                    sb = new UnsupportedService(uri.AbsoluteUri);
                    break;
            }

            return sb;
        }

        public Task SaveAsync(IEnumerable<StreamBase> streams)
        {
            throw new NotImplementedException("editing of urls to be done through invoking notepad.exe");
        }
    }
}
