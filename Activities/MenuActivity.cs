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
			Typeface funnyFont = Typeface.CreateFromAsset(Assets,"Foo.ttf");
			Typeface bloxFont = Typeface.CreateFromAsset(Assets,"Blox.ttf");

			// Set the title text view
			SetTextView(bloxFont, Resource.Id.titleT, TetrisColor.Red);
			SetTextView(bloxFont, Resource.Id.titleE, TetrisColor.Orange);
			SetTextView(bloxFont, Resource.Id.titleT2, TetrisColor.Yellow);
			SetTextView(bloxFont, Resource.Id.titleR, TetrisColor.Green);
			SetTextView(bloxFont, Resource.Id.titleI, TetrisColor.Cyan);
			SetTextView(bloxFont, Resource.Id.titleM, TetrisColor.Pink);

			// Single player button
			ButtonStroked singlePlayerButton = FindViewById<ButtonStroked>(Resource.Id.singlePlayerButton);
			SetButton(singlePlayerButton, funnyFont, TetrisColor.Red);
			singlePlayerButton.Click += delegate {
				Network.Instance.DisableBluetooth();
				startGame(this);
			};

			// Two players button
			ButtonStroked twoPlayersButton = FindViewById<ButtonStroked>(Resource.Id.twoPlayersButton);
			SetButton(twoPlayersButton, funnyFont, TetrisColor.Cyan);
			twoPlayersButton.Click += delegate {
				// Start the bluetooth connection
				var bluetoothConnectionActivity = new Intent(this, typeof(BluetoothConnectionActivity));
				StartActivity(bluetoothConnectionActivity);
				//StartActivityForResult(deviceListActivity, (int) RequestCode.RequestConnectDevice);
				// TODO : add another connection mode
			};

			// Settings button
			ButtonStroked settingsButton = FindViewById<ButtonStroked>(Resource.Id.settingsButton);
			SetButton(settingsButton, funnyFont, TetrisColor.Green);
			settingsButton.Click += delegate {
				// TODO : handle Settings
			};

			// Exit button
			ButtonStroked exitButton = FindViewById<ButtonStroked>(Resource.Id.exitButton);
			SetButton(exitButton, funnyFont, TetrisColor.Yellow);
			exitButton.Click += delegate {
				// TODO : handle Exit
			};
		}

		protected void SetTextView(Typeface font, int id, TetrisColor color)
		{
			TextView titleTextView = FindViewById<TextView> (id);
			titleTextView.SetTypeface(font, TypefaceStyle.Normal);
			titleTextView.SetTextColor(Utils.getAndroidColor(color));
		}

		protected void SetButton(ButtonStroked button, Typeface font, TetrisColor color)
		{
			button.SetTypeface(font, TypefaceStyle.Normal);
			button.StrokeBorderWidth = 40;
			button.StrokeTextWidth = 20;
			button.RadiusIn = 30;
			button.RadiusOut = 20;
			button.DarkColor = Utils.getAndroidDarkColor(color);
			button.LightColor = Utils.getAndroidColor(color);
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

