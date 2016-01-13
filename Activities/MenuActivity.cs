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
	[Activity(Label = "Tetrim", MainLauncher = true, Icon = "@drawable/icon", Theme = "@android:style/Theme.NoTitleBar.Fullscreen", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
	public class MenuActivity : Activity
	{
		//--------------------------------------------------------------
		// CONSTANTS
		//--------------------------------------------------------------
		private const string Tag = "Tetrim-MenuActivity";

		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		private TextView _userNameText;
		private CustomDialogBuilder.RequestCodes userNameDialogRequestCode = CustomDialogBuilder.RequestCodes.Text;
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

			ISharedPreferences sharedPreferencesUser = ApplicationContext.GetSharedPreferences(User.UserFileNameKey, FileCreationMode.Private);
			User.GiveContext(ref sharedPreferencesUser);

			if(!User.Instance.IsUserStored)
			{
				Intent intent = Utils.CreateUserNameDialog(this, Resources);
				StartActivityForResult(intent, (int)userNameDialogRequestCode);
			}

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

			_userNameText = FindViewById<TextView>(Resource.Id.userNameText);
			_userNameText.SetTypeface(funnyFont, TypefaceStyle.Normal);
			_userNameText.Text = Resources.GetString(Resource.String.welcomeUser, User.Instance.UserName);

			// Single player button
			ButtonStroked singlePlayerButton = FindViewById<ButtonStroked>(Resource.Id.singlePlayerButton);
			SetButton(singlePlayerButton, funnyFont, TetrisColor.Red);
			singlePlayerButton.Click += delegate {
				startGame(this, Utils.RequestCode.RequestGameOnePlayer);
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
		protected override void OnActivityResult (int requestCode, Result resultCode, Intent data)
		{
			if(resultCode==Result.Ok)
			{
				if(requestCode==(int)CustomDialogBuilder.RequestCodes.Text)
				{
					String userName = data.GetStringExtra(userNameDialogRequestCode.ToString());
					User.Instance.SetName(userName);
					if(_userNameText != null)
					{
						_userNameText.Text = Resources.GetString(Resource.String.welcomeUser, User.Instance.UserName);
					}
				}
			}
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
			button.StrokeBorderWidth = 15;
			button.StrokeTextWidth = 7;
			button.RadiusIn = 10;
			button.RadiusOut = 7;
			button.StrokeColor = Utils.getAndroidDarkColor(color);
			button.FillColor = Utils.getAndroidColor(color);
		}

		//--------------------------------------------------------------
		// PUBLIC METHODES
		//--------------------------------------------------------------
		public static void startGame(Activity activity, Utils.RequestCode code)
		{
			Intent intent = new Intent(activity, typeof(MainActivity));
			activity.StartActivityForResult(intent, (int) code);
		}
	}
}

