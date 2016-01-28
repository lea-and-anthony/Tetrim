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
			string message = string.Empty;
			TetrisColor messageColor = TetrisColor.Cyan;
			if(score > User.Instance.HighScore)
			{
				// New Highscore !
				// #highscore#
				message = activity.Resources.GetString(Resource.String.newHighScore) + "\n";
				message += activity.Resources.GetString(Resource.String.playerScore, score.ToString());
				messageColor = TetrisColor.Green;
			}
			else
			{
				// Your score : #score#
				// You highscore : #highscore#
				message = activity.Resources.GetString(Resource.String.playerScore, score.ToString()) + "\n";
				message += activity.Resources.GetString(Resource.String.playerHighScore, User.Instance.HighScore.ToString());
				messageColor = TetrisColor.Red;
			}
			return CreateGameOverDialog(activity, message, messageColor);
		}

		public static Intent CreateGameOverDialogMulti(GameActivity activity, bool hasWon)
		{
			string message = activity.Resources.GetString(hasWon ? Resource.String.playerWin : Resource.String.playerLoose);
			TetrisColor messageColor = hasWon ? TetrisColor.Green : TetrisColor.Red;
			return CreateGameOverDialog(activity, message, messageColor);
   		}

		private static Intent CreateGameOverDialog(GameActivity activity, string message, TetrisColor messageColor)
		{
			string title = activity.Resources.GetString(Resource.String.gameOver);
			string posText = activity.Resources.GetString(Resource.String.playAgain);
			string negText = activity.Resources.GetString(Resource.String.menu);
			return DialogActivity.CreateYesNoDialog(activity, title, message, posText, negText,
				delegate {activity.NewGame();}, delegate {activity.Finish();});
		}
	}
}

