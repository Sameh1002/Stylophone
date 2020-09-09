﻿using ColorThiefDotNet;
using FluentMPC.Helpers;
using FluentMPC.Services;
using Microsoft.Toolkit.Uwp.Helpers;
using MpcNET.Commands.Playback;
using MpcNET.Commands.Playlist;
using MpcNET.Types;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Color = Windows.UI.Color;
using Windows.UI.Xaml.Media.Imaging;
using System.Linq;
using Windows.UI.Core;
using FluentMPC.Views;
using Windows.UI;
using MpcNET.Commands.Queue;

namespace FluentMPC.ViewModels.Items
{
    public class TrackViewModel : Observable
    {
        private readonly CoreDispatcher _currentUiDispatcher;

        public IMpdFile File { get; }

        public string Name => File.HasTitle ? File.Title : File.Path.Split('/').Last();

        public bool IsPlaying => MPDConnectionService.CurrentStatus.SongId == File.Id;

        internal void UpdatePlayingStatus() => DispatcherHelper.ExecuteOnUIThreadAsync(() => OnPropertyChanged(nameof(IsPlaying)));

        public BitmapImage AlbumArt
        {
            get => _albumArt;
            private set
            {
                DispatcherHelper.AwaitableRunAsync(_currentUiDispatcher, () => Set(ref _albumArt, value));
            }
        }

        private BitmapImage _albumArt;

        public Color DominantColor
        {
            get => _albumColor;
            private set
            {
                DispatcherHelper.AwaitableRunAsync(_currentUiDispatcher, () => Set(ref _albumColor, value));
            }
        }

        private Color _albumColor;

        private bool _isLight;
        public bool IsLight
        {
            get => _isLight;
            private set
            {
                DispatcherHelper.ExecuteOnUIThreadAsync(() => Set(ref _isLight, value));
            }
        }


        private ICommand _playCommand;
        public ICommand PlayTrackCommand => _playCommand ?? (_playCommand = new RelayCommand<IMpdFile>(PlayTrack));

        private async void PlayTrack(IMpdFile file) => await MPDConnectionService.SafelySendCommandAsync(new PlayIdCommand(file.Id));

        private ICommand _removeCommand;
        public ICommand RemoveFromQueueCommand => _removeCommand ?? (_removeCommand = new RelayCommand<IMpdFile>(RemoveTrack));

        private async void RemoveTrack(IMpdFile file) => await MPDConnectionService.SafelySendCommandAsync(new DeleteIdCommand(file.Id));

        private ICommand _addToQueueCommand;
        public ICommand AddToQueueCommand => _addToQueueCommand ?? (_addToQueueCommand = new RelayCommand<IMpdFile>(AddToQueue));

        private async void AddToQueue(IMpdFile file)
        {
            var response = await MPDConnectionService.SafelySendCommandAsync(new AddIdCommand(file.Path));

            if (response != null)
                NotificationService.ShowInAppNotification($"Added to Queue!");
        }

        private ICommand _addToPlaylistCommand;
        public ICommand AddToPlayListCommand => _addToPlaylistCommand ?? (_addToPlaylistCommand = new RelayCommand<IMpdFile>(AddToPlaylist));

        private async void AddToPlaylist(IMpdFile file)
        {
            var playlistName = await DialogService.ShowAddToPlaylistDialog();
            if (playlistName == null) return;

            var req = await MPDConnectionService.SafelySendCommandAsync(new PlaylistAddCommand(playlistName, file.Path));

            if (req != null)
                NotificationService.ShowInAppNotification($"Added to Playlist {playlistName}!");
        }

        private ICommand _viewAlbumCommand;
        public ICommand ViewAlbumCommand => _viewAlbumCommand ?? (_viewAlbumCommand = new RelayCommand<IMpdFile>(GoToMatchingAlbum));

        private void GoToMatchingAlbum(IMpdFile file)
        {
            try
            {
                if (!file.HasAlbum)
                {
                    NotificationService.ShowInAppNotification($"Track has no associated album.", 0);
                    return;
                }

                // Build an AlbumViewModel from the album name and navigate to it
                var album = new AlbumViewModel(file.Album);
                NavigationService.Navigate<LibraryDetailPage>(album);
            }
            catch (Exception e)
            {
                NotificationService.ShowInAppNotification($"Couldn't get album: {e}", 0);
            }
        }

        public TrackViewModel(IMpdFile file, bool getAlbumArt = false, int albumArtWidth = -1, CoreDispatcher dispatcher = null)
        {
            MPDConnectionService.SongChanged += (s, e) => UpdatePlayingStatus();

            // Use specific UI dispatcher if given
            // (Used for the compact view scenario, which rolls its own dispatcher..)
            _currentUiDispatcher = dispatcher ?? Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher;

            File = file;
            DominantColor = Colors.Black;

            // Fire off an async request to get the album art from MPD.
            if (getAlbumArt)
                Task.Run(async () =>
                {
                    // For the now playing bar, the album art is rendered at 70px wide.
                    // Kinda hackish propagating the width all the way from PlaybackViewModel to here...
                    var art = await AlbumArtHelpers.GetAlbumArtAsync(File, default, _currentUiDispatcher);

                    // This is RAM-intensive as it has to convert the image, so we only do it if needed (aka now playing bar and full playback only)
                    if (art != null && albumArtWidth != -1)
                    {
                        var color = await AlbumArtHelpers.GetDominantColor(art, _currentUiDispatcher);
                        DominantColor = color.ToWindowsColor();
                        IsLight = !(color.IsDark);
                    }

                    if (art != null)
                        AlbumArt = await AlbumArtHelpers.WriteableBitmapToBitmapImageAsync(art, albumArtWidth, _currentUiDispatcher);

                });
        }

    }
}
