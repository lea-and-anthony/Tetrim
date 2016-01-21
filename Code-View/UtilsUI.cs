using System;

using Android.App;
using Android.Bluetooth;
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

		public static void SetDialogButton(DialogActivity activity, ButtonStroked button, EditText field, TetrisColor color, string text, EventHandler action, bool answer)
		{
			if(!String.IsNullOrEmpty(text))
			{
				button.StrokeColor = Utils.getAndroidDarkColor(color);
				button.FillColor = Utils.getAndroidColor(color);
				button.Text = text;
				button.TextSize = Utils.GetPixelsFromDP(activity.BaseContext, 20);
				button.Click += delegate {
					DialogBuilder.ReturnText = (DialogActivity.Builder.RequestCode == DialogBuilder.DialogRequestCode.Text ) ? field.Text : null;
				};
				button.Click += action;
				button.Click += delegate {
					Intent intent = new Intent();
					switch(DialogActivity.Builder.RequestCode)
					{
					case DialogBuilder.DialogRequestCode.PosOrNeg:
						intent.PutExtra(DialogActivity.Builder.RequestCode.ToString(), answer);
						activity.SetResult(Result.Ok, intent);
						activity.Finish();
						break;
					case DialogBuilder.DialogRequestCode.Text:
						if(answer)
						{
							if(!String.IsNullOrEmpty(field.Text))
							{
								intent.PutExtra(DialogActivity.Builder.RequestCode.ToString(), field.Text);
								activity.SetResult(Result.Ok, intent);
								activity.Finish();
							}
						}
						else
						{
							activity.SetResult(Result.Canceled);
							activity.Finish();
						}
						break;
					default:
						break;
					}
				};
			}
		}

		public static ButtonStroked CreateDeviceButton(BluetoothConnectionActivity activity, BluetoothDevice device, TetrisColor color, int minHeight, int defaultText)
		{
			ButtonStroked button = new ButtonStroked(activity.BaseContext);
			button.SetMinimumHeight(minHeight);
			button.StrokeColor = Utils.getAndroidColor(color);
			button.FillColor = Utils.getAndroidDarkColor(color);
			button.Gravity = GravityFlags.Left;
			int padding = Utils.GetPixelsFromDP(activity.BaseContext, 20);
			button.SetPadding(padding, padding, padding, padding);
			button.StrokeBorderWidth = 7;
			button.StrokeTextWidth = 5;
			button.RadiusIn = 7;
			button.RadiusOut = 5;
			button.IsTextStroked = false;
			button.Shape = ButtonStroked.ButtonShape.BottomTop;
			if(device != null)
			{
				button.Tag = device.Address;
				button.Text = device.Name;
				button.Click += delegate {
					activity.DeviceListClick(button);
				};
			}
			else
			{
				button.Text = activity.Resources.GetString(defaultText);
				button.Enabled = false;
			}
			return button;
		}

		public static void SetDeviceMenuButton(Activity activity, ref ButtonStroked button, int id, TetrisColor color)
		{
			button = activity.FindViewById<ButtonStroked>(id);
			button.StrokeColor = Utils.getAndroidDarkColor(color);
			button.FillColor = Utils.getAndroidColor(color);
		}

		public static void SetDeviceMenuLayout(Activity activity, ref LinearLayout layout, int nbDevices)
		{
			layout = new LinearLayout(activity.BaseContext);
			layout.WeightSum = nbDevices;
			layout.Orientation = Orientation.Vertical;
		}

		public static LinearLayout.LayoutParams CreateDeviceLayoutParams(Activity activity, int marginPixel)
		{
			LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, 0, 1);
			int margin = Utils.GetPixelsFromDP(activity.BaseContext, marginPixel);
			//lp.SetMargins(margin, margin, margin, margin);
			return lp;
		}


		public static void SetGamePlayerStatText(Activity activity, int id, bool me, bool isTitle)
		{
			TextView textView = activity.FindViewById<TextView>(id);
			UtilsUI.SetTextFont(textView);
			textView.SetBackgroundColor(Utils.getAndroidColor(me ? TetrisColor.Cyan : TetrisColor.Red));
			textView.SetTextColor(!isTitle ? (me ? UtilsUI.Player1Background : UtilsUI.Player2Background)
				: Utils.getAndroidDarkColor(me ? TetrisColor.Cyan : TetrisColor.Red));
		}

		public static void SetGamePlayerStatText(Activity activity, int id, bool me, bool isTitle, TetrisColor color)
		{
			TextView textView = activity.FindViewById<TextView>(id);
			UtilsUI.SetTextFont(textView);
			textView.SetBackgroundColor(Utils.getAndroidColor(color));
			textView.SetTextColor(!isTitle ? (me ? UtilsUI.Player1Background : UtilsUI.Player2Background)
				: Utils.getAndroidDarkColor(color));
		}

		public static void SetGamePlayerNameText(Activity activity, int id, bool me)
		{
			TextView textView = activity.FindViewById<TextView>(id);
			UtilsUI.SetTextFont(textView);
			textView.SetTextColor(Utils.getAndroidColor(me ? TetrisColor.Cyan : TetrisColor.Red));
		}

		public static void SetGamePlayerNameText(Activity activity, int id, TetrisColor color)
		{
			TextView textView = activity.FindViewById<TextView>(id);
			UtilsUI.SetTextFont(textView);
			textView.SetTextColor(Utils.getAndroidColor(color));
		}
	}
}

