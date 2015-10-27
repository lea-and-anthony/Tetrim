using System;

namespace Tetris
{
	public static class Utils
	{
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
	}
}

