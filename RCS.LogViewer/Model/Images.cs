using System;
using System.Windows.Media.Imaging;

namespace RCS.LogViewer.Model;

static class Images
{
	public static BitmapImage NodeFolderOpen = new(new Uri("/Images/NodeFolderOpen.png", UriKind.Relative));
	public static BitmapImage NodeFolderClosed = new(new Uri("/Images/NodeFolderClosed.png", UriKind.Relative));
	public static BitmapImage NodeStorageAccount = new(new Uri("/Images/NodeStorageAccount.png", UriKind.Relative));
	public static BitmapImage NodeTable = new(new Uri("/Images/NodeTable.png", UriKind.Relative));
	public static BitmapImage NodeUnknown = new(new Uri("/Resources/NodeUnknown.png", UriKind.Relative));
}
