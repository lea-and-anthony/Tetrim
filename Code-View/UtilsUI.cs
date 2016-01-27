using System;

using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Tetrim
{
	public static class UtilsUI
	{
		//--------------------------------------------------------------
		// CONSTANTS
		//--------------------------------------------------------------
		public static Color Player1Background = Color.Black;
		public static Color Player2Background = Color.Black;
		public static int MenuButtonHeight;

		//--------------------------------------------------------------
		// FONTS
		//--------------------------------------------------------------
		public static Typeface TextFont;
		public static Typeface TitleFont;
		public static Typeface ArrowFont;

		public static ButtonUI MenuButtonUI;
		public static ButtonUI IconButtonUI;
		public static ButtonUI ArrowButtonUI;
		public static ButtonUI DialogButtonUI;
		public static ButtonUI DeviceButtonUI;
		public static ButtonUI DeviceMenuButtonUI;

		//--------------------------------------------------------------
		// BUTTON CONFIGURATION
		//--------------------------------------------------------------
		public static ButtonUI CreateMenuButtonSettings()
		{
			ButtonUI menuButtonUI = new ButtonUI();
			menuButtonUI.StrokeBorderWidth = 15;
			menuButtonUI.StrokeTextWidth = 7;
			menuButtonUI.RadiusIn = 10;
			menuButtonUI.RadiusOut = 7;
			menuButtonUI.StrokeShade = Utils.ColorShade.Dark;
			menuButtonUI.FillShade = Utils.ColorShade.Normal;
			return menuButtonUI;
		}

		public static ButtonUI CreateIconButtonSettings(Typeface font)
		{
			ButtonUI iconButtonUI = new ButtonUI();
			iconButtonUI.IsSquared = true;
			iconButtonUI.StrokeBorderWidth = 7;
			iconButtonUI.StrokeTextWidth = 5;
			iconButtonUI.RadiusIn = 7;
			iconButtonUI.RadiusOut = 5;
			iconButtonUI.StrokeShade = Utils.ColorShade.Dark;
			iconButtonUI.FillShade = Utils.ColorShade.Normal;
			iconButtonUI.IsTextStroked = false;
			iconButtonUI.Typeface = font;
			return iconButtonUI;
		}

		public static ButtonUI CreateIconButtonSettings()
		{
			return CreateIconButtonSettings(TextFont);
		}

		public static ButtonUI CreateArrowButtonSettings()
		{
			return CreateIconButtonSettings(ArrowFont);
		}

		public static ButtonUI CreateDialogButtonSettings()
		{
			ButtonUI dialogButtonUI = new ButtonUI();
			dialogButtonUI.StrokeShade = Utils.ColorShade.Dark;
			dialogButtonUI.FillShade = Utils.ColorShade.Normal;
			dialogButtonUI.SetTextSize(ComplexUnitType.Dip, 20);
			return dialogButtonUI;
		}

		public static ButtonUI CreateDeviceButtonSettings()
		{
			ButtonUI deviceButtonUI = new ButtonUI();
			deviceButtonUI.StrokeShade = Utils.ColorShade.Normal;
			deviceButtonUI.FillShade = Utils.ColorShade.Dark;
			deviceButtonUI.Gravity = GravityFlags.Left;
			deviceButtonUI.StrokeBorderWidth = 7;
			deviceButtonUI.StrokeTextWidth = 5;
			deviceButtonUI.RadiusIn = 7;
			deviceButtonUI.RadiusOut = 5;
			deviceButtonUI.IsTextStroked = false;
			deviceButtonUI.Shape = ButtonUI.ButtonShape.BottomTop;
			deviceButtonUI.Padding = 20;
			return deviceButtonUI;
		}
			
		public static ButtonUI CreateDeviceMenuButtonSettings()
		{
			ButtonUI deviceMenuButtonUI = new ButtonUI();
			deviceMenuButtonUI.StrokeShade = Utils.ColorShade.Dark;
			deviceMenuButtonUI.FillShade = Utils.ColorShade.Normal;
			return deviceMenuButtonUI;
		}

		//--------------------------------------------------------------
		// BUTTONS INITIALIZATION
		//--------------------------------------------------------------
		public static void SetMenuButton(ButtonStroked button, TetrisColor color)
		{
			button.Settings = MenuButtonUI.Clone();
			button.Settings.StrokeColor = Utils.getAndroidColor(color, button.Settings.StrokeShade);
			button.Settings.FillColor = Utils.getAndroidColor(color, button.Settings.FillShade);
		}

		public static void SetMenuButtonWithHeight(ButtonStroked button, TetrisColor color)
		{
			SetMenuButton(button, color);
			button.LayoutParameters.Width = ViewGroup.LayoutParams.MatchParent;
			button.LayoutParameters.Height = MenuButtonHeight;
		}

		private static void SetBaseIconButton(ButtonStroked button, TetrisColor color, int height)
		{
			SetBaseIconButton(button, color, height, height);
		}

		private static void SetBaseIconButton(ButtonStroked button, TetrisColor color, int height, int textsize)
		{
			button.Text = button.Tag.ToString();
			button.SetMaxHeight(height);
			button.SetMinimumHeight(height);
			button.Settings.SetTextSize(ComplexUnitType.Px, textsize);
			button.Settings.StrokeColor = Utils.getAndroidColor(color, button.Settings.StrokeShade);
			button.Settings.FillColor = Utils.getAndroidColor(color, button.Settings.FillShade);
		}

		public static void SetArrowButton(ButtonStroked button, TetrisColor color, int difference)
		{
			button.Settings = ArrowButtonUI.Clone();
			SetBaseIconButton(button, color, button.MeasuredWidth + difference);
		}

		public static void SetArrowButtonWithHeight(ButtonStroked button, TetrisColor color)
		{
			button.Settings = ArrowButtonUI.Clone();
			button.LayoutParameters.Width = MenuButtonHeight*2/3;
			button.LayoutParameters.Height = MenuButtonHeight*2/3;
			SetBaseIconButton(button, color, button.LayoutParameters.Height);
		}

		public static void SetIconButton(ButtonStroked button, TetrisColor color, int difference)
		{
			button.Settings = IconButtonUI.Clone();
			SetBaseIconButton(button, color, button.MeasuredWidth + difference, (button.MeasuredWidth + difference)/2);
		}

		public static void SetIconButtonWithHeight(ButtonStroked button, TetrisColor color)
		{
			button.Settings = IconButtonUI.Clone();
			button.LayoutParameters.Width = MenuButtonHeight*2/3;
			button.LayoutParameters.Height = MenuButtonHeight*2/3;
			SetBaseIconButton(button, color, button.LayoutParameters.Height, button.LayoutParameters.Height/2);
		}

		public static void SetDialogButton(DialogActivity activity, ButtonStroked button, EditText field, TetrisColor color, string text, EventHandler action, bool answer)
		{
			if(!String.IsNullOrEmpty(text))
			{
				button.Settings = DialogButtonUI.Clone();
				button.Settings.StrokeColor = Utils.getAndroidColor(color, button.Settings.StrokeShade);
				button.Settings.FillColor = Utils.getAndroidColor(color, button.Settings.FillShade);
				button.Text = text;
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
			if(device == null)
			{
				return CreateDeviceButton(activity, device, color, minHeight, activity.Resources.GetString(defaultText));
			}

			return CreateDeviceButton(activity, device, color, minHeight, device.Name);
		}

		public static ButtonStroked CreateDeviceButton(BluetoothConnectionActivity activity, BluetoothDevice device, TetrisColor color, int minHeight, string text)
		{
			ButtonStroked button = new ButtonStroked(activity);
			button.Settings = DeviceButtonUI.Clone();
			button.Settings.StrokeColor = Utils.getAndroidColor(color, button.Settings.StrokeShade);
			button.Settings.FillColor = Utils.getAndroidColor(color, button.Settings.FillShade);
			button.SetMinimumHeight(minHeight);
			int padding = Utils.GetPixelsFromDP(activity, button.Settings.Padding);
			button.SetPadding(padding, padding, padding, padding);
			button.Text = text;
			if(device != null)
			{
				button.Tag = device.Address;
				button.Click += delegate {
					activity.DeviceListClick(button);
				};
			}
			else
			{
				button.Enabled = false;
			}
			return button;
		}

		public static void SetDeviceMenuButton(Activity activity, ref ButtonStroked button, int id, TetrisColor color)
		{
			button = activity.FindViewById<ButtonStroked>(id);
			button.Settings = DeviceMenuButtonUI.Clone();
			button.Settings.StrokeColor = Utils.getAndroidColor(color, button.Settings.StrokeShade);
			button.Settings.FillColor = Utils.getAndroidColor(color, button.Settings.FillShade);
		}

		//--------------------------------------------------------------
		// LAYOUT INITIALIZATION
		//--------------------------------------------------------------
		public static LinearLayout.LayoutParams CreateDeviceLayoutParams(Activity activity, int marginPixel)
		{
			LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, 1);
			return lp;
		}

		public static void SetDeviceMenuLayout(Activity activity, ref LinearLayout layout, int nbDevices)
		{
			layout = new LinearLayout(activity);
			layout.WeightSum = nbDevices;
			layout.Orientation = Orientation.Vertical;
		}

		//--------------------------------------------------------------
		// TEXT INITIALIZATION
		//--------------------------------------------------------------
		public static void SetTitleTextView(TextView titleTextView, TetrisColor color)
		{
			titleTextView.SetTypeface(UtilsUI.TitleFont, TypefaceStyle.Normal);
			titleTextView.SetTextColor(Utils.getAndroidColor(color));
		}

		public static void SetGamePlayerStatText(Activity activity, int id, bool me, bool isTitle)
		{
			TextView textView = activity.FindViewById<TextView>(id);
			textView.SetBackgroundColor(Utils.getAndroidColor(me ? TetrisColor.Cyan : TetrisColor.Red));
			textView.SetTextColor(!isTitle ? (me ? UtilsUI.Player1Background : UtilsUI.Player2Background)
				: Utils.getAndroidDarkColor(me ? TetrisColor.Cyan : TetrisColor.Red));
		}

		public static void SetGamePlayerStatText(Activity activity, int id, bool me, bool isTitle, TetrisColor color)
		{
			TextView textView = activity.FindViewById<TextView>(id);
			textView.SetBackgroundColor(Utils.getAndroidColor(color));
			textView.SetTextColor(!isTitle ? (me ? UtilsUI.Player1Background : UtilsUI.Player2Background)
				: Utils.getAndroidDarkColor(color));
		}

		public static void SetGamePlayerNameText(Activity activity, int id, bool me)
		{
			TextView textView = activity.FindViewById<TextView>(id);
			textView.SetTextColor(Utils.getAndroidColor(me ? TetrisColor.Cyan : TetrisColor.Red));
		}

		public static void SetGamePlayerNameText(Activity activity, int id, TetrisColor color)
		{
			TextView textView = activity.FindViewById<TextView>(id);
			textView.SetTextColor(Utils.getAndroidColor(color));
		}
	}
}

