﻿using System;
using System.Collections.Generic;

using Android.App;
using Android.Graphics;
using Android.Content;
using Android.Widget;
using Android.OS;
using Android.Util;
using Android.Bluetooth;

namespace Tetrim
{
	[Activity(Label = "Tetrim", Icon = "@drawable/icon", Theme = "@android:style/Theme.NoTitleBar.Fullscreen", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]		
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

			Utils.SetTextFont(FindViewById<TextView>(Resource.Id.settingsTitle));

			ButtonStroked backButton = FindViewById<ButtonStroked>(Resource.Id.backButton);
			Utils.SetIconButtonWithHeight(backButton, TetrisColor.Orange);
			backButton.Click += delegate {
				Finish();
			};

			ButtonStroked deleteFriendsButton = FindViewById<ButtonStroked>(Resource.Id.deleteFriendsButton);
			Utils.SetMenuButtonWithHeight(deleteFriendsButton, TetrisColor.Red);
			deleteFriendsButton.Click += delegate {
				Intent intent = Utils.CreateMakeSureDialog(this, Resources,
					Resources.GetString(Resource.String.askSureDeleteFriends),
					delegate {User.Instance.ClearFriends();});
				StartActivity(intent);
			};

			ButtonStroked changeNameButton = FindViewById<ButtonStroked>(Resource.Id.changeNameButton);
			Utils.SetMenuButtonWithHeight(changeNameButton, TetrisColor.Cyan);
			changeNameButton.Click += delegate {
				Intent intent = Utils.CreateUserNameDialog(this, Resources);
				StartActivity(intent);
			};

			ButtonStroked deleteHighScoresButton = FindViewById<ButtonStroked>(Resource.Id.deleteHighScoresButton);
			Utils.SetMenuButtonWithHeight(deleteHighScoresButton, TetrisColor.Yellow);
			deleteHighScoresButton.Click += delegate {
				Intent intent = Utils.CreateMakeSureDialog(this, Resources,
					Resources.GetString(Resource.String.askSureDeleteHighScore),
					delegate {User.Instance.ClearHighScore();});
				StartActivity(intent);
			};
		}
	}
}
