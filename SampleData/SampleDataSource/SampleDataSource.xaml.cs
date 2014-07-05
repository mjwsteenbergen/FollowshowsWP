﻿//      *********    DO NOT MODIFY THIS FILE     *********
//      This file is regenerated by a design tool. Making
//      changes to this file can cause errors.
namespace Blend.SampleData.SampleDataSource
{
	using System; 
	using System.ComponentModel;

// To significantly reduce the sample data footprint in your production application, you can set
// the DISABLE_SAMPLE_DATA conditional compilation constant and disable sample data at runtime.
#if DISABLE_SAMPLE_DATA
	internal class SampleDataSource { }
#else

	public class SampleDataSource : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (this.PropertyChanged != null)
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public SampleDataSource()
		{
			try
			{
				Uri resourceUri = new Uri("ms-appx:/SampleData/SampleDataSource/SampleDataSource.xaml", UriKind.RelativeOrAbsolute);
				Windows.UI.Xaml.Application.LoadComponent(this, resourceUri);
			}
			catch
			{
			}
		}

		private queue _queue = new queue();

		public queue queue
		{
			get
			{
				return this._queue;
			}
		}

		private shows _shows = new shows();

		public shows shows
		{
			get
			{
				return this._shows;
			}
		}

		private Show _Show = new Show();

		public Show Show
		{
			get
			{
				return this._Show;
			}

			set
			{
				if (this._Show != value)
				{
					this._Show = value;
					this.OnPropertyChanged("Show");
				}
			}
		}
	}

	public class queue : System.Collections.ObjectModel.ObservableCollection<queueItem>
	{ 
	}

	public class queueItem : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (this.PropertyChanged != null)
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		private string _SeriesName = string.Empty;

		public string SeriesName
		{
			get
			{
				return this._SeriesName;
			}

			set
			{
				if (this._SeriesName != value)
				{
					this._SeriesName = value;
					this.OnPropertyChanged("SeriesName");
				}
			}
		}

		private Windows.UI.Xaml.Media.ImageSource _Image = null;

		public Windows.UI.Xaml.Media.ImageSource Image
		{
			get
			{
				return this._Image;
			}

			set
			{
				if (this._Image != value)
				{
					this._Image = value;
					this.OnPropertyChanged("Image");
				}
			}
		}

		private string _EpisodeName = string.Empty;

		public string EpisodeName
		{
			get
			{
				return this._EpisodeName;
			}

			set
			{
				if (this._EpisodeName != value)
				{
					this._EpisodeName = value;
					this.OnPropertyChanged("EpisodeName");
				}
			}
		}

		private double _EpisodePos = 0;

		public double EpisodePos
		{
			get
			{
				return this._EpisodePos;
			}

			set
			{
				if (this._EpisodePos != value)
				{
					this._EpisodePos = value;
					this.OnPropertyChanged("EpisodePos");
				}
			}
		}
	}

	public class shows : System.Collections.ObjectModel.ObservableCollection<showsItem>
	{ 
	}

	public class showsItem : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (this.PropertyChanged != null)
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		private string _Name = string.Empty;

		public string Name
		{
			get
			{
				return this._Name;
			}

			set
			{
				if (this._Name != value)
				{
					this._Name = value;
					this.OnPropertyChanged("Name");
				}
			}
		}

		private double _Watched = 0;

		public double Watched
		{
			get
			{
				return this._Watched;
			}

			set
			{
				if (this._Watched != value)
				{
					this._Watched = value;
					this.OnPropertyChanged("Watched");
				}
			}
		}

		private Windows.UI.Xaml.Media.ImageSource _Image = null;

		public Windows.UI.Xaml.Media.ImageSource Image
		{
			get
			{
				return this._Image;
			}

			set
			{
				if (this._Image != value)
				{
					this._Image = value;
					this.OnPropertyChanged("Image");
				}
			}
		}
	}

	public class Show : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (this.PropertyChanged != null)
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		private string _Name = string.Empty;

		public string Name
		{
			get
			{
				return this._Name;
			}

			set
			{
				if (this._Name != value)
				{
					this._Name = value;
					this.OnPropertyChanged("Name");
				}
			}
		}

		private string _Genre = string.Empty;

		public string Genre
		{
			get
			{
				return this._Genre;
			}

			set
			{
				if (this._Genre != value)
				{
					this._Genre = value;
					this.OnPropertyChanged("Genre");
				}
			}
		}

		private double _Followers = 0;

		public double Followers
		{
			get
			{
				return this._Followers;
			}

			set
			{
				if (this._Followers != value)
				{
					this._Followers = value;
					this.OnPropertyChanged("Followers");
				}
			}
		}

		private string _Actors = string.Empty;

		public string Actors
		{
			get
			{
				return this._Actors;
			}

			set
			{
				if (this._Actors != value)
				{
					this._Actors = value;
					this.OnPropertyChanged("Actors");
				}
			}
		}

		private Windows.UI.Xaml.Media.ImageSource _Image = null;

		public Windows.UI.Xaml.Media.ImageSource Image
		{
			get
			{
				return this._Image;
			}

			set
			{
				if (this._Image != value)
				{
					this._Image = value;
					this.OnPropertyChanged("Image");
				}
			}
		}

		private string _Airs = string.Empty;

		public string Airs
		{
			get
			{
				return this._Airs;
			}

			set
			{
				if (this._Airs != value)
				{
					this._Airs = value;
					this.OnPropertyChanged("Airs");
				}
			}
		}

		private string _Summary = string.Empty;

		public string Summary
		{
			get
			{
				return this._Summary;
			}

			set
			{
				if (this._Summary != value)
				{
					this._Summary = value;
					this.OnPropertyChanged("Summary");
				}
			}
		}
	}
#endif
}
