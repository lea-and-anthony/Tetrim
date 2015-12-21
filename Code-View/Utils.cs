using System;
using Android.App;
using Android.Content;

using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using Android.Runtime;
using Android.Util;

namespace Tetrim
{
	public static class Utils
	{
		// Event triggered when the pop up (displayed by ShowAlert) is closed
		public delegate void PopUpEndDelegate();
		public static event PopUpEndDelegate PopUpEndEvent;
		// Turn a color name into an Android Color
		public static Android.Graphics.Color getAndroidColor(TetrisColor color)
		{
			switch (color)
			{
			case TetrisColor.Red:
				return Android.Graphics.Color.ParseColor("#ffd50000");
				return Android.Graphics.Color.Red;
			case TetrisColor.Orange:
				return Android.Graphics.Color.ParseColor("#ffff6d00");
				return Android.Graphics.Color.Orange;
			case TetrisColor.Yellow:
				return Android.Graphics.Color.ParseColor("#ffffc400");
				return Android.Graphics.Color.Yellow;
			case TetrisColor.Green:
				return Android.Graphics.Color.ParseColor("#ff64dd17");
				return Android.Graphics.Color.Green;
			case TetrisColor.Cyan:
				return Android.Graphics.Color.ParseColor("#ff00e5ff");
				return Android.Graphics.Color.Cyan;
			case TetrisColor.Blue:
				return Android.Graphics.Color.ParseColor("#ff2962ff");
				return Android.Graphics.Color.Blue;
			case TetrisColor.Pink:
				return Android.Graphics.Color.ParseColor("#ffd500f9");
				return Android.Graphics.Color.Magenta;
			}
			return Android.Graphics.Color.Gray;
		}

		public static Android.Graphics.Color getAndroidDarkColor(TetrisColor color)
		{
			Color androidColor = Utils.getAndroidColor(color);
			androidColor.R /= 2;
			androidColor.G /= 2;
			androidColor.B /= 2;
			return androidColor;
			/*switch (color)
			{
			case TetrisColor.Red:
				return Android.Graphics.Color.DarkRed;
			case TetrisColor.Orange:
				return Android.Graphics.Color.DarkOrange;
			case TetrisColor.Yellow:
				return Android.Graphics.Color.DarkGoldenrod;
			case TetrisColor.Green:
				return Android.Graphics.Color.DarkGreen;
			case TetrisColor.Cyan:
				return Android.Graphics.Color.DarkCyan;
			case TetrisColor.Blue:
				return Android.Graphics.Color.DarkBlue;
			case TetrisColor.Pink:
				return Android.Graphics.Color.DarkMagenta;
			}
			return Android.Graphics.Color.Black;*/
		}

		public static Android.Graphics.Color getAndroidLightColor(TetrisColor color)
		{
			Color androidColor = Utils.getAndroidColor(color);
			androidColor.R *= 2;
			androidColor.G *= 2;
			androidColor.B *= 2;
			return androidColor;
			/*switch (color)
			{
			case TetrisColor.Red:
				return Android.Graphics.Color.Pink;
			case TetrisColor.Orange:
				return Android.Graphics.Color.LightSalmon;
			case TetrisColor.Yellow:
				return Android.Graphics.Color.LightGoldenrodYellow;
			case TetrisColor.Green:
				return Android.Graphics.Color.LightGreen;
			case TetrisColor.Cyan:
				return Android.Graphics.Color.LightCyan;
			case TetrisColor.Blue:
				return Android.Graphics.Color.LightBlue;
			case TetrisColor.Pink:
				return Android.Graphics.Color.LightPink;
			}
			return Android.Graphics.Color.White;*/
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

		public static int GetPixelsFromDP(Context context, int dp)
		{
			DisplayMetrics metrics = new DisplayMetrics();
			IWindowManager windowManager = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
			windowManager.DefaultDisplay.GetMetrics(metrics);
			return (int)Math.Round(dp*metrics.Density);
		}

		public static int GetDPFromPixels(Context context, int pixels)
		{
			DisplayMetrics metrics = new DisplayMetrics();
			IWindowManager windowManager = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
			windowManager.DefaultDisplay.GetMetrics(metrics);
			return (int)Math.Round(pixels/metrics.Density);
		}

		public static int ConvertTextSize(Context context, ComplexUnitType unit, int size)
		{
			switch(unit)
			{
			case ComplexUnitType.Px:
				return size;
			default:
				return Utils.GetPixelsFromDP(context, size);
			}
		}
	}
}

