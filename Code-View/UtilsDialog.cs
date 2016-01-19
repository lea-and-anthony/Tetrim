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
	public static class UtilsDialog
	{
		// Event triggered when the pop up (displayed by CreateBluetoothDialogNoCancel) is closed
		public delegate void PopUpEndDelegate();
		public static event PopUpEndDelegate PopUpEndEvent;

		public static Intent CreateBluetoothDialogNoCancel(Activity activity, Android.Content.Res.Resources resources, int messageId)
		{
			// TODO : not cancelable
			DialogBuilder builder = new DialogBuilder(activity.BaseContext);
			builder.Message = resources.GetString(messageId);
			builder.ContentType = DialogBuilder.DialogContentType.TextView;
			builder.RequestCode = DialogBuilder.DialogRequestCode.PosOrNeg;
			builder.PositiveText = resources.GetString(Resource.String.ok);
			builder.PositiveAction += delegate {
				if(PopUpEndEvent != null)
				{
					PopUpEndEvent.Invoke();
					PopUpEndEvent = null; // Unset the event after invoking (we won't need it twice)
				}
			};
			DialogActivity.Builder = builder;
			return new Intent(activity, typeof(DialogActivity));
		}

		public static Intent CreateYesNoDialogNoCancel(Activity activity, Android.Content.Res.Resources resources,
			int titleId, int messageId, EventHandler posAction, EventHandler negAction)
		{
			// TODO : not cancelable
			DialogBuilder builder = new DialogBuilder(activity.BaseContext);
			if(titleId != -1)
			{
				builder.Title = resources.GetString(titleId);
			}
			builder.Message = resources.GetString(messageId);
			builder.ContentType = DialogBuilder.DialogContentType.TextView;
			builder.RequestCode = DialogBuilder.DialogRequestCode.PosOrNeg;
			builder.PositiveText = resources.GetString(Resource.String.yesDialog);
			builder.PositiveAction += posAction;
			builder.NegativeText = resources.GetString(Resource.String.noDialog);
			builder.NegativeAction += negAction;
			DialogActivity.Builder = builder;
			return new Intent(activity, typeof(DialogActivity));
		}

		public static Intent CreateUserNameDialogNoCancel(Activity activity, Android.Content.Res.Resources resources)
		{
			DialogBuilder builder = new DialogBuilder(activity.BaseContext);
			builder.Title = resources.GetString(Resource.String.askName);
			builder.Message = resources.GetString(Resource.String.askName);
			builder.ContentType = DialogBuilder.DialogContentType.EditText;
			builder.RequestCode = DialogBuilder.DialogRequestCode.Text;
			builder.PositiveText = resources.GetString(Resource.String.ok);
			builder.PositiveAction += delegate {
				User.Instance.SetName(builder.ReturnText);
			};
			DialogActivity.Builder = builder;
			return new Intent(activity, typeof(DialogActivity));
		}

		public static Intent CreateUserNameDialog(Activity activity, Android.Content.Res.Resources resources)
		{
			DialogBuilder builder = new DialogBuilder(activity.BaseContext);
			builder.Title = resources.GetString(Resource.String.askName);
			builder.Message = resources.GetString(Resource.String.askName);
			builder.ContentType = DialogBuilder.DialogContentType.EditText;
			builder.RequestCode = DialogBuilder.DialogRequestCode.Text;
			builder.NegativeText = resources.GetString(Resource.String.cancel);
			builder.PositiveText = resources.GetString(Resource.String.ok);
			builder.PositiveAction += delegate {
				User.Instance.SetName(builder.ReturnText);
			};
			DialogActivity.Builder = builder;
			return new Intent(activity, typeof(DialogActivity));
		}

		public static Intent CreateMakeSureDialog(Activity activity, Android.Content.Res.Resources resources, string message, EventHandler posAction)
		{
			DialogBuilder builder = new DialogBuilder(activity.BaseContext);
			builder.ContentType = DialogBuilder.DialogContentType.TextView;
			builder.RequestCode = DialogBuilder.DialogRequestCode.PosOrNeg;
			builder.Message = message;
			builder.NegativeText = resources.GetString(Resource.String.noDialog);
			builder.PositiveText = resources.GetString(Resource.String.yesDialog);
			builder.PositiveAction += posAction;
			DialogActivity.Builder = builder;
			return new Intent(activity, typeof(DialogActivity));
		}

		public static Intent CreateGameOverDialogSingle(Activity activity, Android.Content.Res.Resources resources, int score)
		{
			// Create the content
			TextView titleText = new TextView(activity.BaseContext);
			titleText.SetTextSize(ComplexUnitType.Dip, 30);
			UtilsUI.SetTextFont(titleText);
			titleText.Text = resources.GetString(Resource.String.gameOver);
			titleText.Gravity = GravityFlags.CenterHorizontal;
			titleText.SetTextColor(Utils.getAndroidColor(TetrisColor.Red));
			TextView scoreText = new TextView(activity.BaseContext);
			scoreText.SetTextSize(ComplexUnitType.Dip, 20);
			UtilsUI.SetTextFont(scoreText);
			TextView highScoreText = new TextView(activity.BaseContext);
			highScoreText.SetTextSize(ComplexUnitType.Dip, 20);
			UtilsUI.SetTextFont(highScoreText);
			if(score > User.Instance.HighScore)
			{
				// New Highscore ! in green
				scoreText.SetTextColor(Utils.getAndroidColor(TetrisColor.Green));
				scoreText.Text = resources.GetString(Resource.String.newHighScore);
				// #highscore# in yellow
				highScoreText.SetTextColor(Utils.getAndroidColor(TetrisColor.Green));
				highScoreText.Text = resources.GetString(Resource.String.playerScore, score.ToString());
			}
			else
			{
				// Your score : #score# in red
				scoreText.SetTextColor(Utils.getAndroidColor(TetrisColor.Yellow));
				scoreText.Text = resources.GetString(Resource.String.playerScore, score.ToString());
				// You highscore : #highscore# in blue
				highScoreText.SetTextColor(Utils.getAndroidColor(TetrisColor.Yellow));
				highScoreText.Text = resources.GetString(Resource.String.playerHighScore, User.Instance.HighScore.ToString());
			}

			// Create the builder
			DialogBuilder builder = new DialogBuilder(activity.BaseContext);
			builder.Content.Add(titleText);
			builder.Content.Add(scoreText);
			builder.Content.Add(highScoreText);
			builder.ContentType = DialogBuilder.DialogContentType.None;
			builder.RequestCode = DialogBuilder.DialogRequestCode.PosOrNeg;
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
			DialogActivity.Builder = builder;
			return new Intent(activity, typeof(DialogActivity));
		}

		public static Intent CreateGameOverDialogMulti(Activity activity, Android.Content.Res.Resources resources, bool hasWon)
		{
			// Create the content
			TextView titleText = new TextView(activity.BaseContext);
			titleText.SetTextSize(ComplexUnitType.Dip, 30);
			UtilsUI.SetTextFont(titleText);
			titleText.Text = resources.GetString(Resource.String.gameOver);
			titleText.Gravity = GravityFlags.CenterHorizontal;
			titleText.SetTextColor(Utils.getAndroidColor(TetrisColor.Yellow));
			TextView text = new TextView(activity.BaseContext);
			text.SetTextSize(ComplexUnitType.Dip, 30);
			UtilsUI.SetTextFont(text);
			text.Text = resources.GetString(hasWon ? Resource.String.playerWin : Resource.String.playerLoose);
			text.SetTextColor(Utils.getAndroidColor(hasWon ? TetrisColor.Green : TetrisColor.Red));

			// Create the builder
			DialogBuilder builder = new DialogBuilder(activity.BaseContext);
			builder.Content.Add(titleText);
			builder.Content.Add(text);
			builder.ContentType = DialogBuilder.DialogContentType.None;
			builder.RequestCode = DialogBuilder.DialogRequestCode.PosOrNeg;
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
			DialogActivity.Builder = builder;
			return new Intent(activity, typeof(DialogActivity));
		}
	}
}

