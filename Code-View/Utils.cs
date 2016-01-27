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

		public enum ColorShade
		{
			ReallyLight,
			Light,
			Normal,
			Dark,
			ReallyDark
		};

		public const string OpponentNameExtra = "OpponentName";

		public static Color getAndroidColor(TetrisColor color, ColorShade shade)
		{
			switch(shade)
			{
			case ColorShade.ReallyLight:
				return getAndroidReallyLightColor(color);
			case ColorShade.Light:
				return getAndroidLightColor(color);
			case ColorShade.Normal:
				return getAndroidColor(color);
			case ColorShade.Dark:
				return getAndroidDarkColor(color);
			case ColorShade.ReallyDark:
				return getAndroidReallyDarkColor(color);
			default:
				return Color.Black;
			};
		}
			
		// Turn a color name into an Android Color
		public static Color getAndroidColor(TetrisColor color)
		{
			switch (color)
			{
			case TetrisColor.Red:
				return Color.ParseColor("#ffff0000");
			case TetrisColor.Orange:
				return Color.ParseColor("#ffff8000");
			case TetrisColor.Yellow:
				return Color.ParseColor("#ffffff00");
			case TetrisColor.Green:
				return Color.ParseColor("#ff00ff00");
			case TetrisColor.Cyan:
				return Color.ParseColor("#ff00ffff");
			case TetrisColor.Blue:
				return Color.ParseColor("#ff0000ff");
			case TetrisColor.Magenta:
				return Color.ParseColor("#ffff00ff");
			default:
				return Color.ParseColor("#ff808080");
			}
		}

		public static Color getAndroidDarkColor(TetrisColor color)
		{
			switch (color)
			{
			case TetrisColor.Red:
				return Color.ParseColor("#ff800000");
			case TetrisColor.Orange:
				return Color.ParseColor("#ff804000");
			case TetrisColor.Yellow:
				return Color.ParseColor("#ff808000");
			case TetrisColor.Green:
				return Color.ParseColor("#ff008000");
			case TetrisColor.Cyan:
				return Color.ParseColor("#ff008080");
			case TetrisColor.Blue:
				return Color.ParseColor("#ff000080");
			case TetrisColor.Magenta:
				return Color.ParseColor("#ff800080");
			default:
				return Color.ParseColor("#ff404040");
			}
		}

		public static Color getAndroidLightColor(TetrisColor color)
		{
			switch (color)
			{
			case TetrisColor.Red:
				return Color.ParseColor("#ffff8080");
			case TetrisColor.Orange:
				return Color.ParseColor("#ffffbf80");
			case TetrisColor.Yellow:
				return Color.ParseColor("#ffffff80");
			case TetrisColor.Green:
				return Color.ParseColor("#ff80ff80");
			case TetrisColor.Cyan:
				return Color.ParseColor("#ff80ffff");
			case TetrisColor.Blue:
				return Color.ParseColor("#ff8080ff");
			case TetrisColor.Magenta:
				return Color.ParseColor("#ffff80ff");
			default:
				return Color.ParseColor("#ffbfbfbf");
			}
		}

		public static Color getAndroidReallyLightColor(TetrisColor color)
		{
			switch (color)
			{
			case TetrisColor.Red:
				return Color.ParseColor("#ffffe5e5");
			case TetrisColor.Orange:
				return Color.ParseColor("#fffff2e5");
			case TetrisColor.Yellow:
				return Color.ParseColor("#ffffffe5");
			case TetrisColor.Green:
				return Color.ParseColor("#ffe5ffe5");
			case TetrisColor.Cyan:
				return Color.ParseColor("#ffe5ffff");
			case TetrisColor.Blue:
				return Color.ParseColor("#ffe5e5ff");
			case TetrisColor.Magenta:
				return Color.ParseColor("#ffffe5ff");
			default:
				return Color.ParseColor("#ffffffff");
			}
		}

		public static Color getAndroidReallyDarkColor(TetrisColor color)
		{
			switch (color)
			{
			case TetrisColor.Red:
				return Color.ParseColor("#ff1a0000");
			case TetrisColor.Orange:
				return Color.ParseColor("#ff1a0d00");
			case TetrisColor.Yellow:
				return Color.ParseColor("#ff1a1a00");
			case TetrisColor.Green:
				return Color.ParseColor("#ff001a00");
			case TetrisColor.Cyan:
				return Color.ParseColor("#ff001a1a");
			case TetrisColor.Blue:
				return Color.ParseColor("#ff00001a");
			case TetrisColor.Magenta:
				return Color.ParseColor("#ff1a001a");
			default:
				return Color.ParseColor("#ff000000");
			}
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

