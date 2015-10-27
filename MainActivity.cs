using System;
using System.Timers;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Graphics;
using Android.Util;
using Android.Bluetooth;

namespace Tetris
{
	public class MyView : View
	{
		public GridView m_gridView;

		public MyView(Context context, IAttributeSet attrs) : base(context, attrs)
		{
			m_gridView = null;
		}

		protected override void OnDraw(Canvas canvas)
		{
			base.OnDraw(canvas);

			if(m_gridView != null)
				m_gridView.Draw(canvas);
		}
	}


	[Activity(Label = "Tetris", MainLauncher = true, Icon = "@drawable/icon", Theme = "@android:style/Theme.NoTitleBar.Fullscreen")]
	public class MainActivity : Activity
	{
		// Name of the connected device
		public string connectedDeviceName = null;

		private BluetoothManager bluetooth = null;
		private const string TAG = "Tetris";
		private const bool Debug = true;
		private enum RequestCode
		{
			REQUEST_CONNECT_DEVICE = 1,
			REQUEST_ENABLE_BT = 2
		};


		public Game m_game { get; private set; }
		public GameView m_gameView { get; private set; }
		public bool buttonStartPressed = false;
		public bool opponentReady = false;

		/*------------------------------------ EVENT REPONDING METHODES ------------------------------------*/
		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			// Set our view from the "accueil" layout resource
			SetContentView(Resource.Layout.Accueil);

			if(Debug)
				Log.Debug(TAG, "onCreate()");

			Button button = FindViewById<Button>(Resource.Id.button1);

			button.Click += delegate {
				// Launch the DeviceListActivity to display the list of bluetooth device and select one
				var serverIntent = new Intent(this, typeof(DeviceListActivity));
				StartActivityForResult(serverIntent, (int) RequestCode.REQUEST_CONNECT_DEVICE);
			};

			Button button2 = FindViewById<Button>(Resource.Id.button2);

			button2.Click += delegate {
				enableBluetooth();
			};

			Button button3 = FindViewById<Button>(Resource.Id.button3);

			button3.Click += delegate {
				// Check that we're actually connected before trying anything
				if(bluetooth == null || bluetooth.GetState() != BluetoothManager.State.CONNECTED)
				{
					AlertDialog.Builder builder1 = new AlertDialog.Builder(this);
					builder1.SetTitle(Resource.String.not_connected_title);
					builder1.SetMessage(Resource.String.not_connected);
					builder1.SetCancelable(true);
					builder1.SetPositiveButton("Yes", delegate{bluetooth = null;startGame();});
					builder1.SetNegativeButton("No", delegate{if(bluetooth == null) enableBluetooth();});
					AlertDialog alert11 = builder1.Create();
					alert11.Show();
					//Toast.MakeText (this, Resource.String.not_connected, ToastLength.Short).Show ();
				}
				else
				{
					byte[] message = {Constants.IdMessageStart, Constants.NumVersion1, Constants.NumVersion2};
					// We notify the opponent that we are ready
					bluetooth.Write(message);

					if(opponentReady)
						startGame();// We launch the game (change view and everything)
					else
						buttonStartPressed = true;
				}
			};
		}

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
		{
			if(Debug)
				Log.Debug(TAG, "onActivityResult " + resultCode);

			switch(requestCode)
			{
			case (int) RequestCode.REQUEST_CONNECT_DEVICE:
				// When DeviceListActivity returns with a device to connect
				if(resultCode == Result.Ok && bluetooth != null)
				{
					// Get the device MAC address
					var address = data.Extras.GetString(DeviceListActivity.EXTRA_DEVICE_ADDRESS);
					// Get the BLuetoothDevice object
					BluetoothDevice device = BluetoothAdapter.DefaultAdapter.GetRemoteDevice(address);
					// Attempt to connect to the device
					bluetooth.Connect(device);
				}
				break;
			case (int) RequestCode.REQUEST_ENABLE_BT:
				// When the request to enable Bluetooth returns
				if(resultCode == Result.Ok)
				{
					// Bluetooth is now enabled
					bluetooth = new BluetoothManager(this);
					bluetooth.Start();
				}
				else
				{
					// User did not enable Bluetooth or an error occured
					Log.Debug(TAG, "Bluetooth not enabled");
					showAlert(Resource.String.BTNotEnabledTitle, Resource.String.BTNotEnabled);
				}
				break;
			}
		}

		protected override void OnResume ()
		{
			base.OnResume ();

			// Performing this check in onResume() covers the case in which Bluetooth was
			// not enabled when the button was hit, so we were paused to enable it...
			// onResume() will be called when ACTION_REQUEST_ENABLE activity returns.
			if (bluetooth != null)
			{
				// Only if the state is STATE_NONE, do we know that we haven't started already
				if (bluetooth.GetState() == BluetoothManager.State.NONE)
				{
					// Start the Bluetooth chat services
					bluetooth.Start();
				}
			}
		}

		protected override void OnDestroy ()
		{
			base.OnDestroy ();

			// Stop the Bluetooth Manager
			if (bluetooth != null)
				bluetooth.Stop ();

			if (Debug)
				Log.Error (TAG, "--- ON DESTROY ---");
		}

		private void OnTimerElapsed(object source, ElapsedEventArgs e)
		{
			bool newNextPiece = m_game.m_player1.m_grid.m_isNextPieceModified;
			byte[] messageBuffer = new byte[Constants.SizeMessagePiece - 1];
			messageBuffer = m_game.m_player1.m_grid.m_fallingPiece.getMessage(messageBuffer, 0);

			bool isSamePiece = m_game.m_player1.m_grid.MovePieceDown(m_game.m_player1);
			if(!isSamePiece)
				m_gameView.m_player1View.Update();

			TextView player1name = FindViewById<TextView> (Resource.Id.player1name);
			TextView player1score = FindViewById<TextView> (Resource.Id.player1score);
			TextView player1level = FindViewById<TextView> (Resource.Id.player1level);
			TextView player1rows = FindViewById<TextView> (Resource.Id.player1rows);

			RunOnUiThread(() => m_gameView.m_player1View.Draw(player1name, player1score, player1level, player1rows));
			
			if (m_game.m_player1.m_grid.isGameOver())
			{
				RunOnUiThread(() => showAlert (Resource.String.game_over, Resource.String.game_over));
			}

			//  Network
			// We send the message to the other player
			if(bluetooth != null && bluetooth.GetState() == BluetoothManager.State.CONNECTED)
			{
				// If it is the same piece we only send the position of the piece
				if(isSamePiece)
					bluetooth.Write(m_game.m_player1.getMessagePiece());
				// If it is a new piece, we send the old piece and the new one
				else
				{
					byte[] message = new byte[Constants.SizeMessagePiecePut];
					message[0] = Constants.IdMessagePiecePut;
					for(int i = 0; i < Constants.SizeMessagePiece - 1; i++)
					{
						message[i+1] = messageBuffer[i];
					}
					message = m_game.m_player1.m_grid.getMessagePiece(message, Constants.SizeMessagePiece);

					// We say if we use the piece sent by the opponent or not (if he didn't send one)
					if(newNextPiece)
						message[Constants.SizeMessagePiecePut-1] = 1;
					else
						message[Constants.SizeMessagePiecePut-1] = 0;

					bluetooth.Write(message);
				}
			}

			// Display of the current model
			FindViewById(Resource.Id.PlayerGridView).PostInvalidate();

			// Launch of the next timer
			Timer gameTimer = (Timer) source;
			gameTimer.Interval = getTimerLapse();
			gameTimer.Start();
		}



		/*------------------------------------ GENRALS METHODES ------------------------------------*/
		protected void startGame()
		{
			KeyguardManager keyguardManager = (KeyguardManager)GetSystemService(Activity.KeyguardService);
			KeyguardManager.KeyguardLock screenLock = keyguardManager.NewKeyguardLock(KeyguardService);
			screenLock.DisableKeyguard();

			// Set our view from the "main" layout resource
			SetContentView(Resource.Layout.Main);

			// Creation of the model
			m_game = new Game();

			// Creation of the view
			m_gameView = new GameView(m_game);
			MyView view = FindViewById<MyView>(Resource.Id.PlayerGridView);
			view.m_gridView = m_gameView.m_player1View._gridView;
			// If it is a 2 player game
			if(bluetooth != null && bluetooth.GetState() == BluetoothManager.State.CONNECTED)
			{
				MyView view2 = FindViewById<MyView>(Resource.Id.OpponentGridView);
				view2.m_gridView = m_gameView.m_player2View._gridView;

				ViewProposedPiece viewProposed = FindViewById<ViewProposedPiece>(Resource.Id.ProposedPiecesView);
				viewProposed.SetPlayer(m_game.m_player1);
				viewProposed.SetBluetooth(bluetooth);
			}

			// Launch the main timer of the application
			int time = getTimerLapse();
			Timer gameTimer = new Timer(time);
			gameTimer.Elapsed += new ElapsedEventHandler(OnTimerElapsed);
			gameTimer.Interval = time;
			gameTimer.Start();


			// Linkage of the button with the methods
			FindViewById<Button>(Resource.Id.buttonMoveLeft).Click += delegate {
				m_game.MoveLeft();
				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
			};

			FindViewById<Button>(Resource.Id.buttonMoveRight).Click += delegate {
				m_game.MoveRight();
				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
			};

			// Linkage of the button with the methods
			FindViewById<Button>(Resource.Id.buttonTurnLeft).Click += delegate {
				m_game.TurnLeft();
				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
			};

			FindViewById<Button>(Resource.Id.buttonTurnRight).Click += delegate {
				m_game.TurnRight();
				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
			};

			FindViewById<Button>(Resource.Id.buttonMoveDown).Click += delegate {
				m_game.MoveDown();
				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
			};

			FindViewById<Button>(Resource.Id.buttonMoveFoot).Click += delegate {
				m_game.MoveBottom();
				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
			};
		}

		protected bool enableBluetooth()
		{
			if(Debug)
				Log.Debug(TAG, "enableBluetooth()");

			// If the bluetooth is already enable and set
			if (bluetooth != null)
				return true;

			// Get local Bluetooth adapter
			BluetoothAdapter bluetoothAdapter;
			bluetoothAdapter = BluetoothAdapter.DefaultAdapter;

			// If the adapter is null, then Bluetooth is not supported
			if(bluetoothAdapter == null)
			{
				if(Debug)
					Log.Debug(TAG, "display of the alert");

				showAlert(Resource.String.BTNotAvailableTitle, Resource.String.BTNotAvailable);
				return false;
			}
			else
			{
				// If the bluetooth is not enable, we try to activate it
				if(!bluetoothAdapter.IsEnabled)
				{
					if(Debug)
						Log.Debug(TAG, "intent to activate bluetooth");
					Intent enableIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
					StartActivityForResult(enableIntent,(int) RequestCode.REQUEST_ENABLE_BT);
				}
				else
				{
					if(Debug)
						Log.Debug(TAG, "creation of BluetoothManager");

					bluetooth = new BluetoothManager(this);
					bluetooth.Start();
				}
			}
			return true;
		}

		public void InterpretMessage(byte[] message)
		{
			// If it is a message for the game, i.e. it is the position of the piece or the position of
			// the piece we have to add to the grid or the grid of the opponent
			if(message[0] == Constants.IdMessageGrid || message[0] == Constants.IdMessagePiece || 
				message[0] == Constants.IdMessagePiecePut)
			{
				// Interpret the message
				int retour = m_game.m_player2.interpretMessage(message);
				// Update of the opponent grid (the display will be done with the other grid)
				//if(retour == 2)
				m_gameView.m_player2View.Update();
				
				// Display of the model of the opponent
				FindViewById(Resource.Id.OpponentGridView).PostInvalidate();
				TextView player2name = FindViewById<TextView> (Resource.Id.player2name);
				TextView player2score = FindViewById<TextView> (Resource.Id.player2score);
				TextView player2level = FindViewById<TextView> (Resource.Id.player2level);
				TextView player2rows = FindViewById<TextView> (Resource.Id.player2rows);
				m_gameView.m_player2View.Draw(player2name, player2score, player2level, player2rows);

				// We actualise the proposed piece if the opponent used the selected one
				if(message[0] == Constants.IdMessagePiecePut && message[Constants.SizeMessagePiecePut - 1] == 1)
					FindViewById<ViewProposedPiece>(Resource.Id.ProposedPiecesView).ChangeProposedPiece();
			}
			// If it is the message of the next piece for us
			else if(message[0] == Constants.IdMessageNextPiece)
			{
				m_game.m_player1.interpretMessage(message);
			}
			// It is a message for the main activity
			else if(message[0] == Constants.IdMessageStart)
			{
				// We have recieve a demand to start the game
				// We verify that the two player have the same version of the application
				if(message[1] == Constants.NumVersion1 && message[2] == Constants.NumVersion2)
				{
					// The 2 players have the same version, we can launch the game if we are ready
					if(buttonStartPressed)
						startGame();// We launch the game (change view and everything)
					else
						opponentReady = true;
				}
			}
		}

		public static int getTimerLapse()
		{
			return 1000;
		}

		public void showAlert(int idTitle, int idMessage)
		{
			AlertDialog.Builder builder = new AlertDialog.Builder(this);
			builder.SetTitle(idTitle);
			builder.SetMessage(idMessage);
			builder.SetCancelable(false);
			builder.SetNeutralButton("OK", delegate {});
			AlertDialog alert = builder.Create();
			alert.Show();
		}
	}
}


