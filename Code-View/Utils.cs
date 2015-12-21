using System;
using Android.App;
using Android.Content;

namespace Tetrim
{
	public static class Utils
	{
		public enum RequestCode
		{
			RequestEnableBluetooth = 1,
			RequestReconnect = 2
		};

		// Event triggered when the pop up (displayed by ShowAlert) is closed
		public delegate void PopUpEndDelegate();
		public static event PopUpEndDelegate PopUpEndEvent;
		// Turn a color name into an Android Color
		public static Android.Graphics.Color getAndroidColor(TetrisColor color)
		{
			switch (color)
			{
			case TetrisColor.Red:
				return Android.Graphics.Color.Red;
			case TetrisColor.Orange:
				return Android.Graphics.Color.Orange;
			case TetrisColor.Yellow:
				return Android.Graphics.Color.Yellow;
			case TetrisColor.Green:
				return Android.Graphics.Color.Green;
			case TetrisColor.Cyan:
				return Android.Graphics.Color.Cyan;
			case TetrisColor.Blue:
				return Android.Graphics.Color.Blue;
			case TetrisColor.Pink:
				return Android.Graphics.Color.Magenta;
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
					PopUpEndEvent = null; // Unset the event after invoking (we won't need it twice)
				}
			});
			AlertDialog alert = builder.Create();
			alert.Show();
		}
	}
}

