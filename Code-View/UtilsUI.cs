﻿using System;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Android.Runtime;
using Android.Util;

namespace Tetrim
{
	public static class UtilsUI
	{
		//public static Color Player1Background = Color.ParseColor("#ff00171a");
		//public static Color Player2Background = Color.ParseColor("#ff250000");
		public static Color Player1Background = Color.ParseColor("#ff000000");
		public static Color Player2Background = Color.ParseColor("#ff000000");

		public static Typeface TextFont;
		public static Typeface TitleFont;
		public static Typeface ArrowFont;
		public static int MenuButtonHeight;

		public static void SetTitleTextView(TextView titleTextView, TetrisColor color)
		{
			titleTextView.SetTypeface(UtilsUI.TitleFont, TypefaceStyle.Normal);
			titleTextView.SetTextColor(Utils.getAndroidColor(color));
		}

		public static void SetTextFont(TextView titleTextView)
		{
			titleTextView.SetTypeface(UtilsUI.TextFont, TypefaceStyle.Normal);
		}

		public static void SetMenuButton(ButtonStroked button, TetrisColor color)
		{
			button.SetTypeface(UtilsUI.TextFont, TypefaceStyle.Normal);
			button.StrokeBorderWidth = 15;
			button.StrokeTextWidth = 7;
			button.RadiusIn = 10;
			button.RadiusOut = 7;
			button.StrokeColor = Utils.getAndroidDarkColor(color);
			button.FillColor = Utils.getAndroidColor(color);
		}

		public static void SetMenuButtonWithHeight(ButtonStroked button, TetrisColor color)
		{
			SetMenuButton(button, color);
			button.LayoutParameters.Width = LinearLayout.LayoutParams.MatchParent;
			button.LayoutParameters.Height = MenuButtonHeight;
		}

		private static void SetIconButton(ButtonStroked button, TetrisColor color, Typeface font, int height)
		{
			button.IsSquared = true;
			button.SetTypeface(font, TypefaceStyle.Normal);
			button.Text = button.Tag.ToString();
			button.SetMaxHeight(height);
			button.SetMinimumHeight(height);
			button.SetTextSize(ComplexUnitType.Px, height);
			button.StrokeBorderWidth = 7;
			button.StrokeTextWidth = 5;
			button.RadiusIn = 7;
			button.RadiusOut = 5;
			button.StrokeColor = Utils.getAndroidDarkColor(color);
			button.FillColor = Utils.getAndroidColor(color);
			button.IsTextStroked = false;
		}

		public static void SetArrowButton(ButtonStroked button, TetrisColor color, int difference)
		{
			SetIconButton(button, color, UtilsUI.ArrowFont, button.MeasuredWidth + difference);
		}

		public static void SetArrowButtonWithHeight(ButtonStroked button, TetrisColor color)
		{
			button.LayoutParameters.Width = MenuButtonHeight*2/3;
			button.LayoutParameters.Height = MenuButtonHeight*2/3;
			SetIconButton(button, color, UtilsUI.ArrowFont, button.LayoutParameters.Height);
		}

		public static void SetIconButton(ButtonStroked button, TetrisColor color)
		{
			SetIconButton(button, color, UtilsUI.TextFont, button.MeasuredWidth);
		}

		public static void SetIconButtonWithHeight(ButtonStroked button, TetrisColor color)
		{
			button.LayoutParameters.Width = MenuButtonHeight*2/3;
			button.LayoutParameters.Height = MenuButtonHeight*2/3;
			SetIconButton(button, color, UtilsUI.TextFont, button.LayoutParameters.Height);
		}
	}
}

