﻿using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Tetrim
{
	[Activity(MainLauncher = true, ScreenOrientation = ScreenOrientation.Portrait)]
	public class MenuActivity : Activity, ViewTreeObserver.IOnGlobalLayoutListener
	{
		//--------------------------------------------------------------
		// CONSTANTS
		//--------------------------------------------------------------
		private const string Tag = "Tetrim-MenuActivity";

		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		private TextView _userNameText;

		//--------------------------------------------------------------
		// EVENT CATCHING METHODES
		//--------------------------------------------------------------
		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			// Set our view from the home layout resource
			SetContentView(Resource.Layout.Menu);

			#if DEBUG
			Log.Debug(Tag, "onCreate()");
			#endif

			ISharedPreferences sharedPreferencesUser = ApplicationContext.GetSharedPreferences(User.UserFileNameKey, FileCreationMode.Private);
			User.GiveContext(ref sharedPreferencesUser);

			// Retrieve the user's name
			if(!User.Instance.IsUserStored)
			{
				Intent intent = UtilsDialog.CreateUserNameDialogNoCancel(this);
				StartActivity(intent);
			}

			// Retrieve the fonts
			UtilsUI.TextFont = Typeface.CreateFromAsset(Assets,"Foo.ttf");
			UtilsUI.TitleFont = Typeface.CreateFromAsset(Assets,"Blox.ttf");
			UtilsUI.ArrowFont = Typeface.CreateFromAsset(Assets,"Arrows.otf");
			Utils.SetDefaultFont();
			// TODO : test if it works

			// Set the title
			UtilsUI.SetTitleTextView(FindViewById<TextView>(Resource.Id.titleT), TetrisColor.Red);
			UtilsUI.SetTitleTextView(FindViewById<TextView>(Resource.Id.titleE), TetrisColor.Orange);
			UtilsUI.SetTitleTextView(FindViewById<TextView>(Resource.Id.titleT2), TetrisColor.Yellow);
			UtilsUI.SetTitleTextView(FindViewById<TextView>(Resource.Id.titleR), TetrisColor.Green);
			UtilsUI.SetTitleTextView(FindViewById<TextView>(Resource.Id.titleI), TetrisColor.Cyan);
			UtilsUI.SetTitleTextView(FindViewById<TextView>(Resource.Id.titleM), TetrisColor.Pink);

			// Set the user name
			_userNameText = FindViewById<TextView>(Resource.Id.userNameText);
			_userNameText.SetTypeface(UtilsUI.TextFont, TypefaceStyle.Normal);
			_userNameText.Text = Resources.GetString(Resource.String.welcomeUser, User.Instance.UserName);

			// Single player button
			ButtonStroked singlePlayerButton = FindViewById<ButtonStroked>(Resource.Id.singlePlayerButton);
			UtilsUI.SetMenuButton(singlePlayerButton, TetrisColor.Red);
			singlePlayerButton.Click += delegate {
				startGame(this, Utils.RequestCode.RequestGameOnePlayer);
			};
			// Two players button
			ButtonStroked twoPlayersButton = FindViewById<ButtonStroked>(Resource.Id.twoPlayersButton);
			UtilsUI.SetMenuButton(twoPlayersButton, TetrisColor.Cyan);
			twoPlayersButton.Click += delegate {
				// Start the bluetooth connection
				var bluetoothConnectionActivity = new Intent(this, typeof(BluetoothConnectionActivity));
				StartActivity(bluetoothConnectionActivity);
			};

			// Settings button
			ButtonStroked settingsButton = FindViewById<ButtonStroked>(Resource.Id.settingsButton);
			UtilsUI.SetMenuButton(settingsButton, TetrisColor.Green);
			settingsButton.Click += delegate {  
				var settingsActivity = new Intent(this, typeof(SettingsActivity));
				StartActivity(settingsActivity);
			};

			// Exit button
			ButtonStroked exitButton = FindViewById<ButtonStroked>(Resource.Id.exitButton);
			UtilsUI.SetMenuButton(exitButton, TetrisColor.Yellow);
			exitButton.Click += delegate {
				System.Diagnostics.Process.GetCurrentProcess().CloseMainWindow();
			};

			// Test if the view is created so we can resize the buttons
			LinearLayout menuContainer = FindViewById<LinearLayout>(Resource.Id.menuContainer);
			if(menuContainer.ViewTreeObserver.IsAlive)
			{
				menuContainer.ViewTreeObserver.AddOnGlobalLayoutListener(this);
			}
		}

		protected override void OnResume ()
		{
			base.OnResume();
			UpdateUserNameInUI();
		}

		public void OnGlobalLayout()
		{
			// The view is completely loaded now, so getMeasuredWidth() won't return 0
			ButtonStroked settingsButton = FindViewById<ButtonStroked>(Resource.Id.settingsButton);
			UtilsUI.MenuButtonHeight = settingsButton.Height;

			// Destroy the onGlobalLayout afterwards, otherwise it keeps changing
			// the sizes non-stop, even though it's already done
			LinearLayout menuContainer = FindViewById<LinearLayout>(Resource.Id.menuContainer);
			menuContainer.ViewTreeObserver.RemoveGlobalOnLayoutListener(this);
		}
			
		//--------------------------------------------------------------
		// PUBLIC METHODES
		//--------------------------------------------------------------
		public static void startGame(Activity activity, Utils.RequestCode code)
		{
			Intent intent = null;
			if(Network.Instance.Connected)
			{
				intent = new Intent(activity, typeof(GameMultiActivity));
			}
			else
			{
				intent = new Intent(activity, typeof(GameSingleActivity));
			}
			activity.StartActivityForResult(intent, (int) code);
		}

		//--------------------------------------------------------------
		// PUBLIC METHODES
		//--------------------------------------------------------------
		private void UpdateUserNameInUI()
		{
			if(_userNameText != null)
			{
				_userNameText.Text = Resources.GetString(Resource.String.welcomeUser, User.Instance.UserName);
			}
		}
	}
}

