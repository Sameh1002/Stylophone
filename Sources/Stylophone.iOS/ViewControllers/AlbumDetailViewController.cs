// This file has been autogenerated from a class added in the UI designer.

using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Strings = Stylophone.Localization.Strings.Resources;
using Stylophone.Common.ViewModels;
using Stylophone.iOS.Helpers;
using UIKit;
using CoreGraphics;
using SkiaSharp.Views.iOS;
using System.ComponentModel;

namespace Stylophone.iOS.ViewControllers
{
	public partial class AlbumDetailViewController : UITableViewController, IViewController<AlbumDetailViewModel>
	{
		public AlbumDetailViewController (IntPtr handle) : base (handle)
		{
		}

		public AlbumDetailViewModel ViewModel => Ioc.Default.GetRequiredService<AlbumDetailViewModel>();
		public PropertyBinder<AlbumDetailViewModel> Binder => new(ViewModel);

		private PropertyBinder<AlbumViewModel> _albumBinder;
		private UIBarButtonItem _settingsBtn;

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();
			NavigationItem.LargeTitleDisplayMode = UINavigationItemLargeTitleDisplayMode.Never;
			
			var trackDataSource = new TrackTableViewDataSource(TableView, ViewModel.Source, GetRowContextMenu, true, OnScroll);
			TableView.DataSource = trackDataSource;
			TableView.Delegate = trackDataSource;

			Binder.Bind<bool>(EmptyView, "hidden", nameof(ViewModel.IsSourceEmpty),
				valueTransformer: NSValueTransformer.GetValueTransformer(nameof(ReverseBoolValueTransformer)));
			Binder.Bind<string>(AlbumTrackInfo, "text", nameof(ViewModel.PlaylistInfo));
		}

        private void OnScroll(UIScrollView scrollView)
        {
            if (scrollView.ContentOffset.Y > 192)
            {
				Title = ViewModel?.Item.Name;
				NavigationItem.RightBarButtonItem = _settingsBtn;
			}	
			else
            {
				Title = "";
				NavigationItem.RightBarButtonItem = null;
			}
		}

        void IPreparableViewController.Prepare(object parameter)
        {
			TableView.ScrollRectToVisible(new CGRect(0, 0, 1, 1), false);

			if (ViewModel.Item != null)
				ViewModel.Item.PropertyChanged -= UpdateAlbumArt;

			var album = parameter as AlbumViewModel;
			ViewModel.Initialize(album);
			ViewModel.Item.PropertyChanged += UpdateAlbumArt;

			if (ViewModel.Item.AlbumArtLoaded)
				SetAlbumArt();

			// Reset label texts
			AlbumArtists.Text = "...";

			// Rebindall to our new album VM
			_albumBinder?.Dispose();
			_albumBinder = new PropertyBinder<AlbumViewModel>(ViewModel.Item);

			_albumBinder.Bind<string>(AlbumTitle, "text", nameof(album.Name));
			_albumBinder.Bind<string>(AlbumArtists, "text", nameof(album.Artist));
			_albumBinder.Bind<bool>(AlbumArtLoadingIndicator, "animating", nameof(album.AlbumArtLoaded),
				valueTransformer: NSValueTransformer.GetValueTransformer(nameof(ReverseBoolValueTransformer)));

			_albumBinder.BindButton(PlayButton, Strings.ContextMenuPlay, album.PlayAlbumCommand);
			PlayButton.Layer.CornerRadius = 8;
			_albumBinder.BindButton(AddToQueueButton, Strings.ContextMenuAddToQueue, album.AddAlbumCommand);
			AddToQueueButton.Layer.CornerRadius = 8;
			_albumBinder.BindButton(PlaylistButton, Strings.ContextMenuAddToPlaylist, album.AddToPlaylistCommand);
			PlaylistButton.Layer.CornerRadius = 8;

			_settingsBtn = CreateSettingsButton();

			// Add radius to AlbumArt
			AlbumArt.Layer.CornerRadius = 8;
			AlbumArt.Layer.MasksToBounds = true;

			ArtContainer.Layer.ShadowColor = UIColor.Black.CGColor;
			ArtContainer.Layer.ShadowOpacity = 0.5F;
			ArtContainer.Layer.ShadowOffset = new CGSize(0, 0);
			ArtContainer.Layer.ShadowRadius = 4;
		}

        private void UpdateAlbumArt(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.Item.AlbumArt))
				SetAlbumArt();
        }

        private void SetAlbumArt()
        {
			AlbumArt.Image = ViewModel.Item.AlbumArt.ToUIImage();
			BackgroundArt.Image = ViewModel.Item.AlbumArt.ToUIImage();
			PlayButton.BackgroundColor = ViewModel.Item.DominantColor.ToUIColor();
			AddToQueueButton.BackgroundColor = ViewModel.Item.DominantColor.ToUIColor();
			PlaylistButton.BackgroundColor = ViewModel.Item.DominantColor.ToUIColor();
		}

        private UIMenu GetRowContextMenu(NSIndexPath indexPath)
		{
			// The common commands take a list of objects
			var trackList = new List<object>();

			if (TableView.IndexPathsForSelectedRows == null)
			{
				trackList.Add(ViewModel?.Source[indexPath.Row]);
			}
			else
			{
				trackList = TableView.IndexPathsForSelectedRows.Select(indexPath => ViewModel?.Source[indexPath.Row])
				.ToList<object>();
			}

			var queueAction = Binder.GetCommandAction(Strings.ContextMenuAddToQueue, "plus", ViewModel.AddToQueueCommand, trackList);
			var playlistAction = Binder.GetCommandAction(Strings.ContextMenuAddToPlaylist, "music.note.list", ViewModel.AddToPlayListCommand, trackList);

			return UIMenu.Create(new[] { queueAction, playlistAction });
		}

		private UIBarButtonItem CreateSettingsButton()
		{
			var playAlbumAction = Binder.GetCommandAction(Strings.ContextMenuPlay, "play", ViewModel.Item.PlayAlbumCommand);
			var addAlbumAction = Binder.GetCommandAction(Strings.ContextMenuAddToQueue, "plus", ViewModel.Item.AddAlbumCommand);
			var addToPlaylistAction = Binder.GetCommandAction(Strings.ContextMenuAddToPlaylist, "music.note.list", ViewModel.Item.AddToPlaylistCommand);

			var barButtonMenu = UIMenu.Create(new[] { playAlbumAction, addAlbumAction, addToPlaylistAction });
			return new UIBarButtonItem(UIImage.GetSystemImage("ellipsis.circle"), barButtonMenu);
		}
	}
}
