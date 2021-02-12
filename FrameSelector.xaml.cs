using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GridSpriter
{
	/// <summary>
	/// Interaction logic for FrameSelector.xaml
	/// </summary>
	public partial class FrameSelector : Window
	{
		public int SelectedFrameIndex { get; private set; }
		public FrameSelector()
		{
			InitializeComponent();

			
			var cols = Program.Image.PixelWidth / Program.FrameWidth;
			var rows = Program.Image.PixelHeight / Program.FrameHeight;
			for (var row = 0; row < rows; ++row)
			{
				for (var col = 0; col < cols; ++col)
				{
					var rect = new Int32Rect(col * Program.FrameWidth, row * Program.FrameHeight, Program.FrameWidth, Program.FrameHeight);
					var index = row * cols + col;
					var oldIndex = col + (row * Program.Image.PixelWidth);

					var btn = new Button()
					{
						Width = Program.FrameWidth,
						Height = Program.FrameHeight,
						Tag = index,
						Content = new Image()
						{
							Source = new CroppedBitmap(Program.Image, rect),
						},
						ToolTip = $"Frame #{index}"
					};

					btn.Click += ImgClick;

					RenderOptions.SetBitmapScalingMode((Image)btn.Content, BitmapScalingMode.NearestNeighbor);

					UniGrid.Children.Add(btn);
				}
			}
		}

		private void ImgClick(object sender, RoutedEventArgs e)
		{
			var btn = (Button)sender;
			SelectedFrameIndex = (int)btn.Tag;
			DialogResult = true;
		}

		private void ZoomOut(object sender, RoutedEventArgs e)
		{
			Zoom(-8);
		}

		private void ZoomIn(object sender, RoutedEventArgs e)
		{
			Zoom(8);
		}

		private void Zoom(int increment)
		{
			foreach (var child in UniGrid.Children)
			{
				var btn = (Button)child;

				btn.Width += increment;
				btn.Height += increment;
			}
		}
	}
}
