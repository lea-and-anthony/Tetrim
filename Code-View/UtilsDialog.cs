using System;

using Android.App;
using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Tetrim
{
	public static class UtilsDialog
	{
		// Event triggered when the pop up (displayed by CreateBluetoothDialogNoCancel) is closed
		public delegate void PopUpEndDelegate();
		public static event PopUpEndDelegate PopUpEndEvent;
		public static Intent CreateBluetoothDialogNoCancel(Activity activity, int messageId)
		{
			return DialogActivity.CreateYesNoDialog(activity, messageId, -1, Resource.String.ok, -1,
				delegate {if(PopUpEndEvent != null){PopUpEndEvent.Invoke();PopUpEndEvent = null;}}, null);
		}

		public static Intent CreatePauseGameDialog(GameActivity activity)
		{
			return CreatePauseGameDialog(activity, null);
		}

		public static Intent CreatePauseGameDialog(GameActivity activity, string name)
		{
			bool isMe = string.IsNullOrEmpty(name);
			string title, nameText, message, posText, negText;
			EventHandler posAction, negAction;
			if(isMe)
			{
				nameText = activity.Resources.GetString(Resource.String.you);
				posText = activity.Resources.GetString(Resource.String.resume);
				posAction = delegate {activity.ResumeGame();};
				negText = activity.Resources.GetString(Resource.String.menu);
				negAction = delegate {activity.Finish();};
			}
			else
			{
				nameText = name;
				/*posText = activity.Resources.GetString(Resource.String.menu);
				posAction = delegate {activity.Finish();};
				negText = string.Empty;
				negAction = null;*/
				posText = string.Empty;
				posAction = null;
				negText = activity.Resources.GetString(Resource.String.menu);
				negAction = delegate {activity.Finish();};
			}
			title = activity.Resources.GetString(Resource.String.pause);
			message = string.Format(activity.Resources.GetString(Resource.String.pauseBy, nameText));
			return DialogActivity.CreateYesNoDialog(activity, title, message, posText, negText,	posAction, negAction);
		}

		public static Intent CreateUserNameDialog(Activity activity)
		{
			return DialogActivity.CreateTextPromptDialog(activity, Resource.String.askName, Resource.String.askName, Resource.String.ok, Resource.String.cancel,
				delegate {User.Instance.SetName(DialogBuilder.ReturnText);}, null);
		}

		public static Intent CreateUserNameDialogNoCancel(Activity activity)
		{
			return DialogActivity.CreateTextPromptDialog(activity, Resource.String.askName, Resource.String.askName, Resource.String.ok, -1,
				delegate {User.Instance.SetName(DialogBuilder.ReturnText);}, null);
		}

		public static Intent CreateGameOverDialogSingle(GameActivity activity, int score)
		{
			// Title saying GAME OVER
			TextView titleText = new TextView(activity);
			titleText.SetTextSize(ComplexUnitType.Dip, 30);
			titleText.Text = activity.Resources.GetString(Resource.String.gameOver);
			titleText.Gravity = GravityFlags.CenterHorizontal;
			titleText.SetTextColor(Utils.getAndroidColor(TetrisColor.Red));

			// Score display
			TextView scoreText = new TextView(activity);
			scoreText.SetTextSize(ComplexUnitType.Dip, 20);

			// Highscore display
			TextView highScoreText = new TextView(activity);
			highScoreText.SetTextSize(ComplexUnitType.Dip, 20);

			if(score > User.Instance.HighScore)
			{
				// New Highscore ! in green
				scoreText.SetTextColor(Utils.getAndroidColor(TetrisColor.Green));
				scoreText.Text = activity.Resources.GetString(Resource.String.newHighScore);
				// #highscore# in yellow
				highScoreText.SetTextColor(Utils.getAndroidColor(TetrisColor.Green));
				highScoreText.Text = activity.Resources.GetString(Resource.String.playerScore, score.ToString());
			}
			else
			{
				// Your score : #score# in red
				scoreText.SetTextColor(Utils.getAndroidColor(TetrisColor.Yellow));
				scoreText.Text = activity.Resources.GetString(Resource.String.playerScore, score.ToString());
				// You highscore : #highscore# in blue
				highScoreText.SetTextColor(Utils.getAndroidColor(TetrisColor.Yellow));
				highScoreText.Text = activity.Resources.GetString(Resource.String.playerHighScore, User.Instance.HighScore.ToString());
			}

			return DialogActivity.CreateCustomDialog(activity, new[]{titleText, scoreText, highScoreText}, Resource.String.playAgain, Resource.String.menu,
				delegate {activity.NewGame();}, delegate {activity.Finish();});
		}

		public static Intent CreateGameOverDialogMulti(GameActivity activity, bool hasWon)
		{
			// Create the content
			TextView titleText = new TextView(activity);
			titleText.SetTextSize(ComplexUnitType.Dip, 30);
			titleText.Text = activity.Resources.GetString(Resource.String.gameOver);
			titleText.Gravity = GravityFlags.CenterHorizontal;
			titleText.SetTextColor(Utils.getAndroidColor(TetrisColor.Yellow));
			TextView text = new TextView(activity);
			text.SetTextSize(ComplexUnitType.Dip, 30);
			text.Text = activity.Resources.GetString(hasWon ? Resource.String.playerWin : Resource.String.playerLoose);
			text.Gravity = GravityFlags.CenterHorizontal;
			text.SetTextColor(Utils.getAndroidColor(hasWon ? TetrisColor.Green : TetrisColor.Red));

			return DialogActivity.CreateCustomDialog(activity, new[]{titleText, text}, Resource.String.playAgain, Resource.String.menu,
				delegate {activity.NewGame();}, delegate {activity.Finish();});
   		}
	}
}

