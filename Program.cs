using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;

namespace GridSpriter
{
	public class Frame
	{
		public int FrameIndex { get; set; }
		public long TimeMS { get; set; }
	}

	public class Animation
	{
		public string Name { get; set; }
		public List<Frame> Frames = new List<Frame>();
	}

	public static class Program
	{
		public static int FrameWidth = 32;
		public static int FrameHeight = 32;

		public static string GridImages = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
		public static string ImageSource = null;
		public static BitmapImage Image = null;

		public static List<Animation> Animations = new List<Animation>();
	}
}
