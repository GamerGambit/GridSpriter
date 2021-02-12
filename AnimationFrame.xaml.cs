using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GridSpriter
{
	public partial class AnimationFrame : UserControl
	{
		private float _FrameTimeSeconds = 1;
		public float FrameTimeSeconds
		{
			get => _FrameTimeSeconds;
			set
			{
				var ms = (long)(value * 1000);

				if (FrameTimeMS == ms)
					return;

				Program.Animations.Find(a => a.Name == (string)Tag).Frames.Find(f => f.FrameIndex == FrameIndex).TimeMS = ms;
				_FrameTimeSeconds = value;
			}
		}

		public long FrameTimeMS => (long)(FrameTimeSeconds * 1000.0f) ;

		private int _FrameIndex = 0;
		public int FrameIndex
		{
			get => _FrameIndex;
			set
			{
				_FrameIndex = value;

				var tilesPerRow = Program.Image.PixelWidth / Program.FrameWidth;
				var x = (_FrameIndex % tilesPerRow) * Program.FrameWidth;
				var y = (_FrameIndex / tilesPerRow) * Program.FrameHeight;

				imgFrame.Source = new CroppedBitmap(Program.Image, new Int32Rect()
				{
					X = x,
					Y = y,
					Width = Program.FrameWidth,
					Height = Program.FrameHeight
				});

				ToolTip = $"Frame #{FrameIndex}";
			}
		}

		public AnimationFrame()
		{
			InitializeComponent();
			floatFrameTime.DataContext = this;
			imgFrame.DataContext = this;
			imgFrame.MouseDown += (s, e) =>
			{
				if (e.ChangedButton == System.Windows.Input.MouseButton.Left && e.ClickCount == 2)
				{
					var dialog2 = new FrameSelector();
					if (dialog2.ShowDialog() == true)
					{
						var newFrameIndex = dialog2.SelectedFrameIndex;

						var frame = Program.Animations.Find(a => a.Name == (string)Tag).Frames.Find(f => f.FrameIndex == FrameIndex);
						frame.FrameIndex = newFrameIndex;

						FrameIndex = newFrameIndex;

						// I dont know if I need to properly detect if the selected frame is the same as this one.
						// In some quick tests, double clicking on an AnimationFrame makes it the selected frame anyway,
						// So just set the preview source.
						var window = Window.GetWindow(this);
						var preview = window.FindChild<Image>("Preview");

						if (preview != null)
						{
							preview.Source = imgFrame.Source;
						}
					}
				}
			};

			RenderOptions.SetBitmapScalingMode(imgFrame, BitmapScalingMode.NearestNeighbor);
		}

		public AnimationFrame(long frameTimeMS)
			: this()
		{
			_FrameTimeSeconds = frameTimeMS / 1000.0f;
		}
	}
}
