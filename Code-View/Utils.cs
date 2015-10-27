using System;
using Android.App;
using Android.Content;

namespace Tetris
{
	public static class Utils
	{
		// Event triggered when the pop up (displayed by ShowAlert) is closed
		public delegate void PopUpEndDelegate();
		public static event PopUpEndDelegate PopUpEndEvent;

		public static int StrokeWidth = 5;
		public static int StrokeWidthHightlight = 10;
		public static Android.Graphics.Color BorderColor = Android.Graphics.Color.Gainsboro;
		public static Android.Graphics.Color HightlightBorderColor = Android.Graphics.Color.White;
		public static Android.Graphics.Color BackgroundColor = Android.Graphics.Color.Rgb(25, 20, 35);

		public static Android.Graphics.Color getAndroidColor(Color color)
		{
			switch (color)
			{
			case Color.Red:
				return Android.Graphics.Color.Red;
			case Color.Orange:
				return Android.Graphics.Color.Orange;
			case Color.Yellow:
				return Android.Graphics.Color.Yellow;
			case Color.Green:
				return Android.Graphics.Color.Green;
			case Color.Cyan:
				return Android.Graphics.Color.Cyan;
			case Color.Blue:
				return Android.Graphics.Color.Blue;
			case Color.Pink:
				return Android.Graphics.Color.Pink;
			}
			return Android.Graphics.Color.Black;
		}

		// Display a simple pop up with a title, a text and an "OK" button
		// Trigger the PopUpEndEvent event when the OK button is pressed
		public static void ShowAlert(int idTitle, int idMessage, Context context)
		{
			AlertDialog.Builder builder = new AlertDialog.Builder(context);
			builder.SetTitle(idTitle);
			builder.SetMessage(idMessage);
			builder.SetCancelable(false);
			builder.SetNeutralButton("OK", delegate {
				if(PopUpEndEvent != null)
				{
					PopUpEndEvent.Invoke();
				}
			});
			AlertDialog alert = builder.Create();
			alert.Show();
		}
	}
}

