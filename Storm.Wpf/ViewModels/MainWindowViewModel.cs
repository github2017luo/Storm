﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Storm.Wpf.Common;
using Storm.Wpf.Extensions;
using Storm.Wpf.Streams;
using Storm.Wpf.StreamServices;

namespace Storm.Wpf.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region Fields
        private readonly FileLoader fileLoader = null;
        private readonly TimeSpan updateInterval = TimeSpan.FromSeconds(120d);
        private DispatcherCountdownTimer updateTimer = null;
        #endregion

        #region Properties
        private bool _isActive = false;
        /// <summary>
        /// Is any asynchronous operation in progress.
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value, nameof(IsActive));
        }

        private readonly ObservableCollection<StreamBase> _streams = new ObservableCollection<StreamBase>();
        /// <summary>
        /// The streams.
        /// </summary>
        public IReadOnlyCollection<IStream> Streams => _streams;
        #endregion

        #region Commands
        private DelegateCommand<Window> _exitCommand = null;
        public DelegateCommand<Window> ExitCommand
        {
            get
            {
                if (_exitCommand == null)
                {
                    _exitCommand = new DelegateCommand<Window>(window => window.Close(), _ => true);
                }

                return _exitCommand;
            }
        }

        private DelegateCommandAsync _refreshCommand = null;
        public DelegateCommandAsync RefreshCommand
        {
            get
            {
                if (_refreshCommand == null)
                {
                    _refreshCommand = new DelegateCommandAsync(RefreshAsync, canExecuteAsync);
                }

                return _refreshCommand;
            }
        }

        private DelegateCommand _openStreamsFileCommand = null;
        public DelegateCommand OpenStreamsFileCommand
        {
            get
            {
                if (_openStreamsFileCommand == null)
                {
                    _openStreamsFileCommand = new DelegateCommand(OpenStreamsFile, canExecute);
                }

                return _openStreamsFileCommand;
            }
        }

        private DelegateCommandAsync _loadStreamsCommand = null;
        public DelegateCommandAsync LoadStreamsCommand
        {
            get
            {
                if (_loadStreamsCommand == null)
                {
                    _loadStreamsCommand = new DelegateCommandAsync(LoadStreams, canExecuteAsync);
                }

                return _loadStreamsCommand;
            }
        }

        private DelegateCommand<StreamBase> _openStreamCommand = null;
        public DelegateCommand<StreamBase> OpenStreamCommand
        {
            get
            {
                if (_openStreamCommand == null)
                {
                    _openStreamCommand = new DelegateCommand<StreamBase>(OpenStream, canExecute);
                }

                return _openStreamCommand;
            }
        }

        private DelegateCommand<StreamBase> _openAccountPageCommand = null;
        public DelegateCommand<StreamBase> OpenAccountPageCommand
        {
            get
            {
                if (_openAccountPageCommand == null)
                {
                    _openAccountPageCommand = new DelegateCommand<StreamBase>(OpenAccountPage, canExecute);
                }

                return _openAccountPageCommand;
            }
        }

        /// <summary>
        /// Figures out whether an asynchronous command can start.
        /// </summary>
        /// <param name="_"></param>
        /// <returns></returns>
        private bool canExecuteAsync(object _) => !IsActive;

        /// <summary>
        /// Figures out whether a synchronous command can start.
        /// </summary>
        /// <param name="_"></param>
        /// <returns></returns>
        private bool canExecute(object _) => true;
        #endregion

        public MainWindowViewModel(FileLoader fileLoader)
        {
            this.fileLoader = fileLoader ?? throw new ArgumentNullException(nameof(fileLoader));
        }

        public void StartUpdateTimer()
        {
            updateTimer = new DispatcherCountdownTimer(updateInterval, async () => await RefreshAsync());

            updateTimer.Start();
        }

        public void StopUpdateTimer()
        {
            if (updateTimer is DispatcherCountdownTimer)
            {
                updateTimer.Stop();

                updateTimer = null;
            }
        }

        /// <summary>
        /// Updates the status of every stream in Streams.
        /// </summary>
        /// <returns></returns>
        public Task RefreshAsync() => RefreshAsync(Streams);

        /// <summary>
        /// Updates the status of the supplied streams.
        /// </summary>
        /// <param name="streams">The streams you want to update.</param>
        /// <returns></returns>
        public Task RefreshAsync(IEnumerable<IStream> streams)
        {
            var updateTasks = new List<Task>
            {
                TwitchService.UpdateAsync(streams.OfType<TwitchStream>()),
                ChaturbateService.UpdateAsync(streams.OfType<ChaturbateStream>())
            };

            return Task.WhenAll(updateTasks);
        }

        /// <summary>
        /// Loads streams from the on disk file,
        /// adds any new ones to the view model, and removes any that are no longer present.
        /// </summary>
        /// <returns></returns>
        public async Task LoadStreams()
        {
            string[] lines = await fileLoader.LoadLinesAsync();

            List<StreamBase> loadedStreams = new List<StreamBase>();

            foreach (string line in lines)
            {
                if (StreamFactory.TryCreate(line, Char.Parse("#"), out StreamBase stream))
                {
                    loadedStreams.Add(stream);
                }
            }

            RemoveOld(loadedStreams);

            var newlyAdded = AddNew(loadedStreams);

            await RefreshAsync(newlyAdded);
        }

        /// <summary>
        /// Removes streams from the view model that are no longer in the on-disk file.
        /// </summary>
        /// <param name="loadedStreams">The streams newly loaded from disk.</param>
        private void RemoveOld(IEnumerable<StreamBase> loadedStreams)
        {
            List<StreamBase> toBeRemoved = new List<StreamBase>();

            foreach (StreamBase each in _streams)
            {
                if (!loadedStreams.Contains(each))
                {
                    toBeRemoved.Add(each);
                }
            }

            foreach (StreamBase each in toBeRemoved)
            {
                _streams.Remove(each);
            }
        }

        /// <summary>
        /// Adds streams from the on-disk file that the view model doesn't have yet.
        /// </summary>
        /// <param name="loadedStreams">The streams newly loaded from disk.</param>
        /// <returns></returns>
        private IEnumerable<StreamBase> AddNew(IEnumerable<StreamBase> loadedStreams)
        {
            List<StreamBase> added = new List<StreamBase>();

            foreach (StreamBase each in loadedStreams)
            {
                if (!_streams.Contains(each))
                {
                    _streams.Add(each);

                    added.Add(each);
                }
            }

            return added;
        }

        /// <summary>
        /// Opens the streams file in the .txt-handler program, typically notepad.exe
        /// </summary>
        private void OpenStreamsFile() => fileLoader.File.Launch();

        /// <summary>
        /// Opens a stream.
        /// </summary>
        /// <param name="stream"></param>
        private void OpenStream(StreamBase stream)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Navigates to the stream account page in the OS-default web browser.
        /// </summary>
        /// <param name="stream"></param>
        private void OpenAccountPage(StreamBase stream) => stream.AccountLink.OpenInBrowser();
    }
}
