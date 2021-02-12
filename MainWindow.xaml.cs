
using Microsoft.WindowsAPICodePack.Dialogs;

using NLua;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

using Application = System.Windows.Application;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using DragEventHandler = System.Windows.DragEventHandler;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MouseEventHandler = System.Windows.Input.MouseEventHandler;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Timer = System.Timers.Timer;

namespace GridSpriter
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private Timer timer;
		private readonly ObservableCollection<AnimationFrame> animFrames;

		private int _AnimFrameIndex = 0;
		private int AnimFrameIndex
		{
			get => _AnimFrameIndex;
			set
			{
				_AnimFrameIndex = Math.Max(0, value);

				var selected = AnimationList.SelectedItem;
				if (selected == null)
					return;

				if (animFrames.Count == 0)
				{
					Preview.Source = null;
					return;
				}

				_AnimFrameIndex = Math.Min(animFrames.Count - 1, _AnimFrameIndex);

				Preview.Source = animFrames[_AnimFrameIndex].imgFrame.Source;
			}
		}

		private void PopulateProgramAnimationsFrames()
		{
			var animName = ((TextBlock)AnimationList.SelectedItem).Text;
			var newFrames = new List<Frame>();

			foreach (var frame in animFrames)
			{
				newFrames.Add(new Frame()
				{
					FrameIndex = frame.FrameIndex,
					TimeMS = frame.FrameTimeMS
				});
			}

			Program.Animations.Find(a => a.Name == animName).Frames = newFrames;
		}

		public MainWindow()
		{
			DataContext = this;
			InitializeComponent();
			animFrames = new ObservableCollection<AnimationFrame>();
			AnimationFrames.ItemsSource = animFrames;

			var style = new Style(typeof(ListBoxItem));
			style.Setters.Add(new Setter(AllowDropProperty, true));
			style.Setters.Add(new EventSetter(PreviewMouseMoveEvent, new MouseEventHandler(FrameDrag)));
			style.Setters.Add(new EventSetter(DropEvent, new DragEventHandler(FrameDrop)));
			AnimationFrames.ItemContainerStyle = style;
		}

		private void SetImagesRoot(object sender, RoutedEventArgs e)
		{
			var dialog = new CommonOpenFileDialog()
			{
				IsFolderPicker = true,
				Title = "Selected the grid-sdk/images folder",
				AllowNonFileSystemItems = false
			};

			if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
			{
				Program.GridImages = dialog.FileName;
			}
		}

		private void ChangeFrameSize(object sender, RoutedEventArgs e)
		{
			var dialog = new FrameSizeDialog();

			if (dialog.ShowDialog() == true)
			{
				Program.FrameWidth = dialog.FrameWidth;
				Program.FrameHeight = dialog.FrameHeight;
			}
		}

		private void FileOpen(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog()
			{
				Multiselect = false,
				Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|Lua Files (*.lua)|*.lua|All Files (*.*)|*.*"
			};

			if (dialog.ShowDialog() == true)
			{
				var fileName = dialog.FileName;

				animFrames.Clear();
				AnimationList.Items.Clear();
				Program.Animations = new List<Animation>();

				if (fileName.EndsWith(".lua"))
				{
					FileOpenLua(fileName);
				}
				else
				{
					Program.ImageSource = fileName;
					Program.Image = new BitmapImage();
					Program.Image.BeginInit();
					Program.Image.UriSource = new Uri(fileName);
					Program.Image.EndInit();
				}
			}
		}

		private void FileOpenLua(string filename)
		{
			var lua = new Lua();
			try
			{
				var ret = lua.DoFile(filename);

				if (ret.Length != 1 || !(ret[0] is LuaTable retTbl))
				{
					MessageBox.Show("Lua File does not look like a valid Planimeter/grid-sdk Animation!.", "Error loading Lua File", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				var img = retTbl["image"] as string;
				var Lwidth = retTbl["width"];
				var Lheight = retTbl["height"];
				var LframeTime = retTbl["frametime"];
				var animations = retTbl["animations"] as LuaTable;

				if (
					string.IsNullOrWhiteSpace(img) ||
					Lwidth == null ||
					Lheight == null ||
					animations == null)
				{
					MessageBox.Show("Invalid animation table format!", "Error loading Lua file", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				var width = (long)Lwidth;
				var height = (long)Lheight;
				var frametime = (int)(Convert.ToDouble(LframeTime) * 1000.0);

				// setup image
				var src = Program.GridImages.ToString() + "/" + img;
				Program.ImageSource = src;

				try
				{
					Program.Image = new BitmapImage();
					Program.Image.BeginInit();
					Program.Image.UriSource = new Uri(src, UriKind.Absolute);
					Program.Image.EndInit();
				}
				catch (DirectoryNotFoundException e)
				{
					MessageBox.Show(string.Format("{0}\nDid you forget to set the Grid Image Root?", e.Message), "Invalid Image File", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				// load animations
				foreach (KeyValuePair<object, object> entry in animations)
				{
					var animName = entry.Key as string;

					if (string.IsNullOrWhiteSpace(animName))
					{
						MessageBox.Show("Invalid animation table format!", "Error loading animations", MessageBoxButton.OK, MessageBoxImage.Error);
						return;
					}

					var anim = new Animation()
					{
						Name = animName
					};

					void processLuaTable(LuaTable lt)
					{
						var LframeTime2 = lt["frameTime"];
						var frames = lt["frames"];

						// frameTime and frames exist
						if ((LframeTime2 is double || LframeTime2 is long) && frames != null)
						{
							var frametime2 = (int)(Convert.ToDouble(LframeTime2) * 1000.0);

							if (frames is LuaTable lt2)
							{
								foreach (KeyValuePair<object, object> pair in lt2) // lets assume this is a table of frameIndexes and not a nested frame
								{
									anim.Frames.Add(new Frame()
									{
										FrameIndex = Convert.ToInt32(pair.Value) - 1,
										TimeMS = frametime2
									});
								}
							}
							else if (frames is long l)
							{
								anim.Frames.Add(new Frame()
								{
									FrameIndex = Convert.ToInt32(l) - 1,
									TimeMS = frametime2
								});
							}
						}
						else // this is just a table. means nested table with different frameTime, or a table of frame indices
						{
							foreach (KeyValuePair<object, object> pair in lt)
							{
								if (pair.Value is long l)
								{
									anim.Frames.Add(new Frame()
									{
										FrameIndex = Convert.ToInt32(l) - 1,
										TimeMS = frametime
									});
								}
								else if (pair.Value is LuaTable lt2)
								{
									processLuaTable(lt2);
								}
							}
						}
					}

					switch (entry.Value)
					{
						case long l:
							anim.Frames.Add(new Frame()
							{
								FrameIndex = Convert.ToInt32(l) - 1,
								TimeMS = frametime
							});
							break;

						case LuaTable lt:
							processLuaTable(lt);
							break;
					}

					Program.Animations.Add(anim);
					AnimationList.Items.Add(new TextBlock()
					{
						Text = anim.Name,
						ToolTip = anim.Name,
						Margin = new Thickness(0, 2, 0, 0)
					});
				}
				AnimationList.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Text", System.ComponentModel.ListSortDirection.Ascending));
			}
			catch (NLua.Exceptions.LuaScriptException e)
			{
				MessageBox.Show(e.Message, "Error loading Lua File", MessageBoxButton.OK);
				return;
			}
		}

		private void FileExport(object sender, RoutedEventArgs e)
		{
			if (Program.ImageSource == null || Program.Animations.Count == 0)
			{
				MessageBox.Show("There is no data to export", "Bad Export", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				return;
			}

			var dialog = new SaveFileDialog()
			{
				AddExtension = true,
				DefaultExt = ".lua",
				Filter = "Lua File (*.lua)|*.lua|All Files (*.*)|*.*"
			};

			if (dialog.ShowDialog() == true)
			{
				// Check is any of the animations have different frame times
				var firstFT = Program.Animations.First().Frames.First().TimeMS;
				var differentFT = Program.Animations.Any(a => a.Frames.Any(f => f.TimeMS != firstFT));

				var sb = new StringBuilder();
				var relativeSrc = Program.ImageSource.Remove(0, Program.GridImages.Length + 1);
				sb.AppendLine("return {")
					.AppendFormat("\timage = \"{0}\",\n", relativeSrc)
					.AppendFormat("\twidth = {0},\n", Program.FrameWidth)
					.AppendFormat("\theight = {0},\n", Program.FrameHeight)
					.AppendFormat("\tframetime = {0},\n", differentFT ? 0 : Math.Round(firstFT / 1000.0f, 2))
					.AppendLine  ("\tanimations = {");

				Program.Animations.Sort((a, b) => string.Compare(a.Name, b.Name));
				for (var animI = 0; animI < Program.Animations.Count; ++animI)
				{
					var anim = Program.Animations[animI];

					if (animI > 0)
					{
						sb.AppendLine(",");
					}

					sb.AppendFormat("\t\t[\"{0}\"] = ", anim.Name);

					// Build up a dictionary
					var frameTimes = new List<KeyValuePair<long, List<int>>>();
					foreach (var frame in anim.Frames)
					{
						if (frameTimes.Count == 0 || frameTimes.Last().Key != frame.TimeMS)
						{
							frameTimes.Add(new KeyValuePair<long, List<int>>(frame.TimeMS, new List<int>()));
						}

						frameTimes.Last().Value.Add(frame.FrameIndex + 1);
					}

					void printFrameTime(KeyValuePair<long, List<int>> frameTime, int indentation = 3)
					{
						var localDifferentFT = frameTime.Key != firstFT;
						// single frame AND same FT
						if (frameTime.Value.Count == 1 && !localDifferentFT)
						{
							sb.Append(frameTime.Value.First());
							return;
						}

						// multi-frame OR different FT
						if (localDifferentFT)
						{

							sb.AppendLine("{")
								.Append(new string('\t', indentation))
								.AppendFormat("frameTime = {0},\n", frameTime.Key / 1000.0f);

							sb.Append(new string('\t', indentation))
								.AppendFormat("frames = ");
						}

						if (frameTime.Value.Count == 1) // single frame
						{
							sb.Append(frameTime.Value.First());
						}
						else
						{
							sb.AppendFormat("{{ {0} }}", string.Join(", ", frameTime.Value));
						}

						if (localDifferentFT)
						{
							sb.AppendLine()
								.Append(new string('\t', indentation - 1))
								.Append('}');
						}
					}

					if (frameTimes.Count == 1 && frameTimes.First().Value.Count == 1) // single frame animation
					{
						printFrameTime(frameTimes.First());
					}
					else // multi-frame animation
					{
						if (frameTimes.Count > 1)
						{
							sb.AppendLine("{")
								.Append("\t\t\t");
						}

						var ftCount = frameTimes.Count;
						for (var fti = 0; fti < ftCount; fti++)
						{
							var ft = frameTimes.ElementAt(fti);

							if (fti > 0)
							{
								sb.AppendLine(",")
									.Append("\t\t\t");
							}

							printFrameTime(ft, frameTimes.Count > 1 ? 4 : 3);
						}

						if (frameTimes.Count > 1)
						{
							sb.AppendLine()
								.Append("\t\t}");
						}
					}
				}

				sb.AppendLine()
					.AppendLine("\t}")
					.AppendLine("}");

				File.WriteAllText(dialog.FileName, sb.ToString());
			}
		}

		private void Exit(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void AddAnimation(object sender, RoutedEventArgs e)
		{
			var dialog = new TextDialog()
			{
				Title = "New Animation",
				PrimaryText = "Please enter a name for the new animation:"
			};

			if (dialog.ShowDialog() != true)
				return;

			if (string.IsNullOrWhiteSpace(dialog.ResponseText))
				return;

			if (Program.Animations.Exists(a => a.Name == dialog.ResponseText))
				return; // TODO should probably warn about conflicting anim names

			AnimationList.Items.Add(new TextBlock()
			{
				Text = dialog.ResponseText,
				ToolTip = dialog.ResponseText,
				Margin = new Thickness(0, 2, 0, 0)
			});

			Program.Animations.Add(new Animation()
			{
				Name = dialog.ResponseText
			});

			// Set the selected index AFTER adding it to Program.Animations
			AnimationList.SelectedIndex = AnimationList.Items.Count - 1;
		}

		private void AnimationRename(object sender, RoutedEventArgs e)
		{
			if (AnimationList.SelectedIndex == -1)
				return;

			var selected = (TextBlock)AnimationList.SelectedItem;
			var oldName = selected.Text;

			var dialog = new TextDialog()
			{
				Title = "Rename Animation",
				PrimaryText = $"Please enter the new name for the animation (\"{oldName}\"):"
			};

			if (dialog.ShowDialog() == true)
			{
				var newName = dialog.ResponseText;
				selected.Text = newName;

				Program.Animations.Find(a => a.Name == oldName).Name = newName;
				foreach (var af in animFrames.Where(af => (string)af.Tag == oldName))
				{
					af.Tag = newName;
				}
			}
		}

		private void AnimationRemove(object sender, RoutedEventArgs e)
		{
			if (AnimationList.SelectedIndex == -1)
				return;

			var animName = ((TextBlock)AnimationList.SelectedItem).Text;

			AnimationList.Items.RemoveAt(AnimationList.SelectedIndex);
			AnimationList.UnselectAll();

			Program.Animations.RemoveAll(a => a.Name == animName);
			animFrames.Clear();
			Preview.Source = null;
		}

		private void AddAnimationFrame(object sender, RoutedEventArgs e)
		{
			if (Program.ImageSource == null || AnimationList.SelectedItem == null)
				return; // TODO should probably warn about invalid program state


			var dialog = new FrameSelector();
			if (dialog.ShowDialog() == true)
			{
				var animName = ((TextBlock)AnimationList.SelectedItem).Text;

				var frame = new AnimationFrame()
				{
					Tag = animName,
					FrameIndex = dialog.SelectedFrameIndex
				};

				animFrames.Add(frame);
				AnimationFrames.Items.Refresh();

				Program.Animations.Find(a => a.Name == animName).Frames.Add(new Frame()
				{
					FrameIndex = frame.FrameIndex,
					TimeMS = frame.FrameTimeMS
				});

				LastFrame(null, null);
			}
		}

		private void AnimationSelected(object sender, RoutedEventArgs e)
		{
			StopTimer();

			if (AnimationList.SelectedIndex == -1)
			{
				animFrames.Clear();
				return;
			}

			var animName = ((TextBlock)AnimationList.SelectedItem).Text;

			animFrames.Clear();// = new ObservableCollection<AnimationFrame>();

			foreach (var frame in Program.Animations.Find(a => a.Name == animName).Frames)
			{
				animFrames.Add(new AnimationFrame(frame.TimeMS)
				{
					Tag = animName,
					FrameIndex = frame.FrameIndex
				});
			}

			AnimationFrames.Items.Refresh();

			if (animFrames.Count > 0)
			{
				AnimationFrames.SelectedIndex = 0;
			}

			AnimFrameIndex = 0;
		}

		private void FrameSelected(object sender, RoutedEventArgs e)
		{
			var frame = (AnimationFrame)AnimationFrames.SelectedItem;

			if (frame == null)
				return;

			Preview.Source = frame.imgFrame.Source;
		}

		private void FrameDrag(object sender, MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				var dragged = sender as ListBoxItem;

				if (dragged == null || dragged.DataContext is not AnimationFrame)
					return;

				if (dragged.Tag is bool b && b)
					return;

				dragged.Tag = true;

				Dispatcher.BeginInvoke(new Action(() =>
				{
					DragDrop.DoDragDrop(dragged, dragged.DataContext, DragDropEffects.Move);
				}), System.Windows.Threading.DispatcherPriority.Normal);
			}
		}

		private void FrameDrop(object sender, DragEventArgs e)
		{
			((ListBoxItem)sender).Tag = null;

			var droppedData = (AnimationFrame)e.Data.GetData(typeof(AnimationFrame));
			var target = (AnimationFrame)((ListBoxItem)sender).DataContext;

			var removedIdx = AnimationFrames.Items.IndexOf(droppedData);
			var targetIdx = AnimationFrames.Items.IndexOf(target);

			if (removedIdx < targetIdx)
			{
				animFrames.Insert(targetIdx + 1, droppedData);
				animFrames.RemoveAt(removedIdx);
				AnimationFrames.Items.Refresh();
				AnimationFrames.SelectedItem = droppedData;
				PopulateProgramAnimationsFrames();
			}
			else
			{
				var remIdx = removedIdx + 1;

				if (animFrames.Count + 1 > remIdx)
				{
					animFrames.Insert(targetIdx, droppedData);
					animFrames.RemoveAt(remIdx);
					AnimationFrames.SelectedItem = droppedData;
					AnimationFrames.Items.Refresh();
					PopulateProgramAnimationsFrames();
				}	
			}
		}

		private void FrameRemove(object sender, RoutedEventArgs e)
		{
			if (AnimationFrames.SelectedIndex == -1)
				return;

			var selected = (AnimationFrame)AnimationFrames.SelectedItem;
			var animName = ((TextBlock)AnimationList.SelectedItem).Text;
			var selectedIdx = AnimationFrames.SelectedIndex;
			var frameIndex = selected.FrameIndex;

			AnimationFrames.UnselectAll();
			animFrames.RemoveAt(selectedIdx);
			AnimationFrames.Items.Refresh();
			Preview.Source = null;

			Program.Animations.Find(a => a.Name == animName).Frames.RemoveAll(f => f.FrameIndex == frameIndex);
		}

		private void FirstFrame(object sender, RoutedEventArgs e) => AnimFrameIndex = 0;
		private void PrevFrame(object sender, RoutedEventArgs e) => --AnimFrameIndex;
		private void NextFrame(object sender, RoutedEventArgs e) => ++AnimFrameIndex;
		private void LastFrame(object sender, RoutedEventArgs e) => AnimFrameIndex = animFrames.Count - 1;

		private void PlayPause(object sender, RoutedEventArgs e)
		{
			// Prevents animations playing if no animation is selected, or the selected animation has 0 or 1 frame.
			if (AnimationList.SelectedIndex == -1 || animFrames.Count < 2)
				return;

			if (timer != null && timer.Enabled)
			{
				StopTimer();
				return;
			}

			var animName = ((TextBlock)AnimationList.SelectedItem).Text;

			if (Program.Animations.Find(a => a.Name == animName).Frames.Any(f => f.TimeMS == 0))
			{
				MessageBox.Show("Cannot play animation if one or more frames have a 0s frame time", "Play Animation", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				return;
			}

			timer = new Timer
			{
				Interval = Program.Animations.Find(a => a.Name == animName).Frames[AnimFrameIndex].TimeMS
			};

			timer.Elapsed += (s, e) =>
			{
				Dispatcher.Invoke(() =>
				{
					if (AnimFrameIndex == animFrames.Count - 1)
					{
						AnimFrameIndex = 0;
					}
					else
					{
						AnimFrameIndex++;
					}

					timer.Interval = Program.Animations.Find(a => a.Name == animName).Frames[AnimFrameIndex].TimeMS;
				});
			};
			timer.Start();

			PlayPauseBtn.Content = "⏸";
		}

		private void StopTimer()
		{
			timer?.Stop();
			PlayPauseBtn.Content = "▶";
		}
	}
}
