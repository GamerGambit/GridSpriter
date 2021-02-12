using System.Windows;

namespace GridSpriter
{
	/// <summary>
	/// Interaction logic for FrameSizeDialog.xaml
	/// </summary>
	public partial class FrameSizeDialog : Window
	{
		public int FrameWidth { get; set; } = Program.FrameWidth;
		public int FrameHeight { get; set; } = Program.FrameHeight;

		public FrameSizeDialog()
		{
			InitializeComponent();
			frameWidth.DataContext = this;
			frameHeight.DataContext = this;
		}

		private void Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}
	}
}
