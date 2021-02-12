using System;
using System.Windows;

namespace GridSpriter
{
	public partial class TextDialog : Window
	{
		public string PrimaryText { get; set; }
		public string ResponseText { get; private set; }

		public string OKText { get; set; } = "Ok";

		public TextDialog()
		{
			InitializeComponent();

			labelPrimary.DataContext = this;
			buttonOk.DataContext = this;
		}

		private void Click(object sender, RoutedEventArgs e)
		{
			ResponseText = textboxResponse.Text;
			DialogResult = true;
		}

		private void Window_ContentRendered(object sender, EventArgs e)
		{
			textboxResponse.SelectAll();
			textboxResponse.Focus();
		}
	}
}
