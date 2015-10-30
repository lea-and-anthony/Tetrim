using System;
using System.Timers;

using Android.App;
using Android.Graphics;
using Android.Content;
using Android.Widget;
using Android.OS;
using Android.Util;
using Android.Bluetooth;

namespace Tetrim
{
	[Activity(Label = "Tetrim", MainLauncher = true, Icon = "@drawable/icon", Theme = "@android:style/Theme.NoTitleBar.Fullscreen")]
	public class MenuActivity : Activity
	{
		//--------------------------------------------------------------
		// CONSTANTS
		//--------------------------------------------------------------
		private const string Tag = "Tetrim-MenuActivity";

		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------

		//--------------------------------------------------------------
		// EVENT CATCHING METHODES
		//--------------------------------------------------------------
		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			// Set our view from the home layout resource
			SetContentView(Resource.Layout.Home);

			#if DEBUG
			Log.Debug(Tag, "onCreate()");
			#endif

			// Retrieve the fonts
			Typeface arcadeFont = Typeface.CreateFromAsset(Assets,"Karmatic_Arcade.ttf");
			Typeface bloxFont = Typeface.CreateFromAsset(Assets,"Blox.ttf");

			// Set the title text view
			TextView titleTextView = FindViewById<TextView> (Resource.Id.textViewTitle);
			titleTextView.SetTypeface(bloxFont, TypefaceStyle.Normal);

			// Single player button
			Button singlePlayerButton = FindViewById<Button>(Resource.Id.singlePlayerButton);
			singlePlayerButton.SetTypeface(arcadeFont, TypefaceStyle.Normal);
			singlePlayerButton.Click += delegate {
				Network.Instance.DisableBluetooth();
				startGame(this);
			};

			// Two players button
			Button twoPlayersButton = FindViewById<Button>(Resource.Id.twoPlayersButton);
			twoPlayersButton.SetTypeface(arcadeFont, TypefaceStyle.Normal);
			twoPlayersButton.Click += delegate {
				// Start the bluetooth connection
				var bluetoothConnectionActivity = new Intent(this, typeof(BluetoothConnectionActivity));
				StartActivity(bluetoothConnectionActivity);
				//StartActivityForResult(deviceListActivity, (int) RequestCode.RequestConnectDevice);
				// TODO : add another connection mode
			};

			// Settings button
			Button settingsButton = FindViewById<Button>(Resource.Id.settingsButton);
			settingsButton.SetTypeface(arcadeFont, TypefaceStyle.Normal);
			settingsButton.Click += delegate {
				// TODO : handle Settings
			};

			// Exit button
			Button exitButton = FindViewById<Button>(Resource.Id.exitButton);
			exitButton.SetTypeface(arcadeFont, TypefaceStyle.Normal);
			exitButton.Click += delegate {
				// TODO : handle Exit
			};
		}

		//--------------------------------------------------------------
		// PUBLIC METHODES
		//--------------------------------------------------------------
		public static void startGame(Activity activity)
		{
			Intent intent = new Intent(activity, typeof(MainActivity));
			activity.StartActivity(intent);
		}
	}
}

