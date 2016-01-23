using System;

using Java.Lang.Reflect;

using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;

namespace Tetrim
{
	public static class Utils
	{
		public enum RequestCode
		{
			RequestEnableBluetooth = 1,
			RequestReconnect = 2,
			RequestGameOnePlayer = 3,
			RequestGameTwoPlayer = 4,
			RequestUserName = 5,
		};
			


		// Turn a color name into an Android Color
		public static Color getAndroidColor(TetrisColor color)
		{
			switch (color)
			{
			case TetrisColor.Red:
				return Color.ParseColor("#ffd50000");
			case TetrisColor.Orange:
				return Color.ParseColor("#ffff6d00");
			case TetrisColor.Yellow:
				return Color.ParseColor("#ffffc400");
			case TetrisColor.Green:
				return Color.ParseColor("#ff64dd17");
			case TetrisColor.Cyan:
				return Color.ParseColor("#ff00e5ff");
			case TetrisColor.Blue:
				return Color.ParseColor("#ff2962ff");
			case TetrisColor.Pink:
				return Color.ParseColor("#ffd500f9");
			}
			return Color.Gray;
		}

		public static Color getAndroidDarkColor(TetrisColor color)
		{
			Color androidColor = Utils.getAndroidColor(color);
			androidColor.R /= 2;
			androidColor.G /= 2;
			androidColor.B /= 2;
			return androidColor;
		}

		public static Color getAndroidLightColor(TetrisColor color)
		{
			/*Color androidColor = Utils.getAndroidColor(color);
			androidColor.R *= 2;
			androidColor.G *= 2;
			androidColor.B *= 2;
			return androidColor;*/
			switch (color)
			{
			case TetrisColor.Red:
				return Color.Pink;
			case TetrisColor.Orange:
				return Color.LightSalmon;
			case TetrisColor.Yellow:
				return Color.LightGoldenrodYellow;
			case TetrisColor.Green:
				return Color.LightGreen;
			case TetrisColor.Cyan:
				return Color.LightCyan;
			case TetrisColor.Blue:
				return Color.LightBlue;
			case TetrisColor.Pink:
				return Color.LightPink;
			}
			return Color.White;
		}

		public static Paint createPaintWithStyle(Paint paint, Paint.Style style)
		{
			paint.SetStyle(style);
			return paint;
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

		public static void SetDefaultFont()
		{
			replaceFont("DEFAULT", UtilsUI.TextFont);
			replaceFont("DEFAULT_BOLD", UtilsUI.TextFont);
			replaceFont("MONOSPACE", UtilsUI.TextFont);
			replaceFont("SERIF", UtilsUI.TextFont);
			replaceFont("SANS_SERIF", UtilsUI.TextFont);
		}

		public static void AddByteArrayToOverArray(ref byte[] message, byte[] over, int offset)
		{
			for(int i = 0; i < over.Length; i++)
			{
				message[offset + i] = over[i];
			}
		}

		private static void replaceFont(string staticTypefaceFieldName, Typeface newTypeface)
		{
			try
			{
				Field staticField = newTypeface.Class.GetDeclaredField(staticTypefaceFieldName);
				staticField.Accessible = true;
				staticField.Set(null, newTypeface);
				staticField.Accessible = false;
			}
			catch (Java.Lang.NoSuchFieldException e)
			{
				e.PrintStackTrace();
			}
			catch (Java.Lang.IllegalAccessException e)
			{
				e.PrintStackTrace();
			}
		}
	}
}

