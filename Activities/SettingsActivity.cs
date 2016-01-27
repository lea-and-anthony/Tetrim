using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using Android.Views;

namespace Tetrim
{
	[Activity(ScreenOrientation = ScreenOrientation.Portrait)]		
	public class SettingsActivity : Activity
	{
		//--------------------------------------------------------------
		// CONSTANTS
		//--------------------------------------------------------------
		private const string Tag = "Tetrim-SettingsActivity";

		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------

		//--------------------------------------------------------------
		// EVENT CATCHING METHODES
		//--------------------------------------------------------------
		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);

			// Set our view from the home layout resource
			SetContentView(Resource.Layout.Settings);

			#if DEBUG
			Log.Debug(Tag, "onCreate()");
			#endif

			ButtonStroked backButton = FindViewById<ButtonStroked>(Resource.Id.backButton);
			UtilsUI.SetIconButtonWithHeight(backButton, TetrisColor.Orange);
			backButton.Click += delegate {
				Finish();
			};

			ButtonStroked deleteFriendsButton = FindViewById<ButtonStroked>(Resource.Id.deleteFriendsButton);
			UtilsUI.SetMenuButtonWithHeight(deleteFriendsButton, TetrisColor.Red);
			deleteFriendsButton.Click += delegate {
				Intent intent = DialogActivity.CreateYesNoDialog(this, Resource.String.askSureDeleteFriends, -1	,
					delegate {User.Instance.ClearFriends();}, null);
				StartActivity(intent);
			};

			ButtonStroked changeNameButton = FindViewById<ButtonStroked>(Resource.Id.changeNameButton);
			UtilsUI.SetMenuButtonWithHeight(changeNameButton, TetrisColor.Cyan);
			changeNameButton.Click += delegate {
				Intent intent = UtilsDialog.CreateUserNameDialog(this);
				StartActivity(intent);
			};

			ButtonStroked deleteHighScoresButton = FindViewById<ButtonStroked>(Resource.Id.deleteHighScoresButton);
			UtilsUI.SetMenuButtonWithHeight(deleteHighScoresButton, TetrisColor.Yellow);
			deleteHighScoresButton.Click += delegate {
				Intent intent = DialogActivity.CreateYesNoDialog(this, Resource.String.askSureDeleteHighScore, -1,
					delegate {User.Instance.ClearHighScore();}, null);
				StartActivity(intent);
			};
		}

		protected override void OnDestroy()
		{
			Utils.RemoveBitmapsOfButtonStroked(FindViewById<ViewGroup>(Resource.Id.rootSettings));
			base.OnDestroy();
		}
	}
}

