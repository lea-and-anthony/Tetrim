using System;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Android.Runtime;
using Android.Util;

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

		public static Typeface TextFont;
		public static Typeface TitleFont;
		public static Typeface ArrowFont;
		public static int MenuButtonHeight;

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
			/*Color androidColor = Utils.getAndroidColor(color);
			androidColor.R *= 2;
			androidColor.G *= 2;
			androidColor.B *= 2;
			return androidColor;*/
			switch (color)
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
			return Android.Graphics.Color.White;
		}

		public static void SetTitleTextView(TextView titleTextView, TetrisColor color)
		{
			titleTextView.SetTypeface(Utils.TitleFont, TypefaceStyle.Normal);
			titleTextView.SetTextColor(Utils.getAndroidColor(color));
		}

		public static void SetTextFont(TextView titleTextView)
		{
			titleTextView.SetTypeface(Utils.TextFont, TypefaceStyle.Normal);
		}

		public static void SetMenuButton(ButtonStroked button, TetrisColor color)
		{
			button.SetTypeface(Utils.TextFont, TypefaceStyle.Normal);
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

		public static void SetArrowButton(ButtonStroked button, TetrisColor color)
		{
			SetIconButton(button, color, Utils.ArrowFont, button.MeasuredWidth);
		}

		public static void SetArrowButtonWithHeight(ButtonStroked button, TetrisColor color)
		{
			button.LayoutParameters.Width = MenuButtonHeight*2/3;
			button.LayoutParameters.Height = MenuButtonHeight*2/3;
			SetIconButton(button, color, Utils.ArrowFont, button.LayoutParameters.Height);
		}

		public static void SetIconButton(ButtonStroked button, TetrisColor color)
		{
			SetIconButton(button, color, Utils.TextFont, button.MeasuredWidth);
		}

		public static void SetIconButtonWithHeight(ButtonStroked button, TetrisColor color)
		{
			button.LayoutParameters.Width = MenuButtonHeight*2/3;
			button.LayoutParameters.Height = MenuButtonHeight*2/3;
			SetIconButton(button, color, Utils.TextFont, button.LayoutParameters.Height);
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

		public static Intent CreateUserNameDialogNoCancel(Activity activity, Android.Content.Res.Resources resources)
		{
			CustomDialogBuilder builder = new CustomDialogBuilder(activity.BaseContext);
			builder.Title = resources.GetString(Resource.String.askName);
			builder.Message = resources.GetString(Resource.String.askName);
			builder.ContentType = CustomDialogBuilder.DialogContentType.EditText;
			builder.RequestCode = CustomDialogBuilder.DialogRequestCode.Text;
			builder.PositiveText = resources.GetString(Resource.String.ok);
			builder.PositiveAction += delegate {
				User.Instance.SetName(builder.ReturnText);
			};
			CustomDialog.Builder = builder;
			return new Intent(activity, typeof(CustomDialog));
		}

		public static Intent CreateUserNameDialog(Activity activity, Android.Content.Res.Resources resources)
		{
			CustomDialogBuilder builder = new CustomDialogBuilder(activity.BaseContext);
			builder.Title = resources.GetString(Resource.String.askName);
			builder.Message = resources.GetString(Resource.String.askName);
			builder.ContentType = CustomDialogBuilder.DialogContentType.EditText;
			builder.RequestCode = CustomDialogBuilder.DialogRequestCode.Text;
			builder.NegativeText = resources.GetString(Resource.String.cancel);
			builder.PositiveText = resources.GetString(Resource.String.ok);
			builder.PositiveAction += delegate {
				User.Instance.SetName(builder.ReturnText);
			};
			CustomDialog.Builder = builder;
			return new Intent(activity, typeof(CustomDialog));
		}

		public static Intent CreateMakeSureDialog(Activity activity, Android.Content.Res.Resources resources, string message, EventHandler posAction)
		{
			CustomDialogBuilder builder = new CustomDialogBuilder(activity.BaseContext);
			builder.ContentType = CustomDialogBuilder.DialogContentType.TextView;
			builder.RequestCode = CustomDialogBuilder.DialogRequestCode.PosOrNeg;
			builder.Message = message;
			builder.NegativeText = resources.GetString(Resource.String.noDialog);
			builder.PositiveText = resources.GetString(Resource.String.yesDialog);
			builder.PositiveAction += posAction;
			CustomDialog.Builder = builder;
			return new Intent(activity, typeof(CustomDialog));
		}

		public static Intent CreateGameOverDialogSingle(Activity activity, Android.Content.Res.Resources resources, int score)
		{
			// Create the content
			TextView scoreText = new TextView(activity.BaseContext);
			scoreText.SetTextSize(ComplexUnitType.Dip, 20);
			scoreText.SetTypeface(Utils.TextFont);
			TextView highScoreText = new TextView(activity.BaseContext);
			highScoreText.SetTextSize(ComplexUnitType.Dip, 20);
			highScoreText.SetTypeface(Utils.TextFont);
			if(score > User.Instance.HighScore)
			{
				// New Highscore ! in green
				scoreText.SetTextColor(Utils.getAndroidColor(TetrisColor.Green));
				scoreText.Text = resources.GetString(Resource.String.newHighScore);
				// #highscore# in yellow
				highScoreText.SetTextColor(Utils.getAndroidColor(TetrisColor.Yellow));
				highScoreText.Text = score.ToString();
			}
			else
			{
				// Your score : #score# in red
				scoreText.SetTextColor(Utils.getAndroidColor(TetrisColor.Red));
				scoreText.Text = resources.GetString(Resource.String.playerScore, score.ToString());
				// You highscore : #highscore# in blue
				scoreText.SetTextColor(Utils.getAndroidColor(TetrisColor.Cyan));
				scoreText.Text = resources.GetString(Resource.String.playerScore, User.Instance.HighScore.ToString());
			}

			// Create the builder
			CustomDialogBuilder builder = new CustomDialogBuilder(activity.BaseContext);
			builder.ContentType = CustomDialogBuilder.DialogContentType.None;
			builder.RequestCode = CustomDialogBuilder.DialogRequestCode.PosOrNeg;
			builder.PositiveText = resources.GetString(Resource.String.playAgain);
			builder.PositiveAction += delegate {
				MenuActivity.startGame(activity, Utils.RequestCode.RequestGameOnePlayer);
				activity.Finish();
			};
			builder.NegativeText = resources.GetString(Resource.String.menu);
			builder.NegativeAction += delegate {
				activity.Finish();
			};

			// Create the dialog
			CustomDialog.Builder = builder;
			return new Intent(activity, typeof(CustomDialog));
		}
	}
}

