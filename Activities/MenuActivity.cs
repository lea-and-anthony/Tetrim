using System;
using System.Timers;

using Android.App;
using Android.Content;
using Android.Widget;
using Android.OS;
using Android.Util;
using Android.Bluetooth;

namespace Tetris
{
	[Activity(Label = "Tetris", MainLauncher = true, Icon = "@drawable/icon", Theme = "@android:style/Theme.NoTitleBar.Fullscreen")]
	public class MenuActivity : Activity
	{
		//--------------------------------------------------------------
		// TYPES
		//--------------------------------------------------------------
		private enum RequestCode
		{
			REQUEST_CONNECT_DEVICE = 1,
			REQUEST_ENABLE_BT = 2
		};

		private enum StartState
		{
			NONE,
			WAITING_FOR_OPPONENT,
			OPPONENT_READY
		};

		//--------------------------------------------------------------
		// CONSTANTS
		//--------------------------------------------------------------
		private const string TAG = "Tetris-MenuActivity";


		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		private String _connectedDeviceName = String.Empty;

		private TextView _textView1 = null;
		private TextView _textView2 = null;

		private StartState _state = StartState.NONE;

		//--------------------------------------------------------------
		// EVENT REPONDING METHODES
		//--------------------------------------------------------------
		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			// Set our view from the "accueil" layout resource
			SetContentView(Resource.Layout.Accueil);

			#if DEBUG
			Log.Debug(TAG, "onCreate()");
			#endif

			// Display the list of the available bluetooth devices
			Button remoteDeviceButton = FindViewById<Button>(Resource.Id.button1);
			remoteDeviceButton.Click += delegate {
				// Launch the DeviceListActivity to display the list of bluetooth device and select one
				var serverIntent = new Intent(this, typeof(DeviceListActivity));
				StartActivityForResult(serverIntent, (int) RequestCode.REQUEST_CONNECT_DEVICE);
			};

			// Creation of the bluetoothManager
			Button enableBluetoothButton= FindViewById<Button>(Resource.Id.button2);
			enableBluetoothButton.Click += delegate {
				enableBluetooth();
			};

			// Start Game
			Button startGameButton = FindViewById<Button>(Resource.Id.button3);
			startGameButton.Click += delegate {
				// Check that we're actually connected before trying anything
				if(!Network.Instance.Connected())
				{
					AlertDialog.Builder builder1 = new AlertDialog.Builder(this);
					builder1.SetTitle(Resource.String.not_connected_title);
					builder1.SetMessage(Resource.String.not_connected);
					builder1.SetCancelable(true);
					builder1.SetPositiveButton("Yes", delegate{Network.Instance.DisableBluetooth();startGame();});
					builder1.SetNegativeButton("No", delegate{if(!Network.Instance.Enable()) enableBluetooth();});
					AlertDialog alert11 = builder1.Create();
					alert11.Show();
				}
				else
				{
					byte[] message = {Constants.IdMessageStart, Constants.NumVersion1, Constants.NumVersion2};
					// We notify the opponent that we are ready
					Network.Instance.CommunicationWay.Write(message);

					if(_state == StartState.OPPONENT_READY)
						startGame();// We launch the game (change view and everything)
					else
						_state = StartState.WAITING_FOR_OPPONENT;
				}
			};

			_textView1 = FindViewById<TextView> (Resource.Id.textView1);
			_textView2 = FindViewById<TextView> (Resource.Id.textView2);

			// Hook on the Network event
			Network.Instance.DeviceNameEvent += DeviceNameEventReceived;
			Network.Instance.ReadEvent += ReadMessageEventReceived;
			Network.Instance.StateConnectedEvent += StateConnectedEventReceived;
			Network.Instance.StateConnectingEvent += StateConnectingEventReceived;
			Network.Instance.StateNoneEvent += StateNoneEventReceived;
			Network.Instance.WriteEvent += WriteMessageEventReceived;
			Network.Instance.StartMessage += StartGameMessage;
		}

		protected override void OnResume ()
		{
			base.OnResume ();

			// Performing this check in onResume() covers the case in which Bluetooth was
			// not enabled when the button was hit, so we were paused to enable it...
			// onResume() will be called when ACTION_REQUEST_ENABLE activity returns.
			// Only if the state is STATE_NONE, do we know that we haven't started already
			if (Network.Instance.WaitingForConnection())
			{
				// Start the Bluetooth for the game
				Network.Instance.CommunicationWay.Start();
			}
		}

		protected override void OnDestroy ()
		{
			base.OnDestroy ();

			// Stop the Bluetooth Manager
			if (Network.Instance.Enable())
				Network.Instance.CommunicationWay.Stop ();

			#if DEBUG
			Log.Error (TAG, "--- ON DESTROY ---");
			#endif
		}

		/*
		 * Called by the system when it completed its task.
		 * For example it is called when the system managed to enable the bluetooth or when
		 * the user has selected a remote device for the bluetooth connection.
		 */
		protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
		{
			#if DEBUG
			Log.Debug(TAG, "onActivityResult " + resultCode);
			#endif

			switch(requestCode)
			{
			case (int) RequestCode.REQUEST_CONNECT_DEVICE:
				// When DeviceListActivity returns with a device to connect
				if(resultCode == Result.Ok && Network.Instance.Enable())
				{
					// Get the device MAC address
					var address = data.Extras.GetString(DeviceListActivity.EXTRA_DEVICE_ADDRESS);
					// Get the BLuetoothDevice object
					BluetoothDevice device = BluetoothAdapter.DefaultAdapter.GetRemoteDevice(address);
					// Attempt to connect to the device
					Network.Instance.CommunicationWay.Connect(device);
				}
				break;
			case (int) RequestCode.REQUEST_ENABLE_BT:
				// When the request to enable Bluetooth returns
				if(resultCode == Result.Ok)
				{
					// Bluetooth is now enabled
					Network.Instance.EnableBluetooth();
					Network.Instance.CommunicationWay.Start();
				}
				else
				{
					// User did not enable Bluetooth or an error occured
					Log.Debug(TAG, "Bluetooth not enabled");
					Utils.ShowAlert(Resource.String.BTNotEnabledTitle, Resource.String.BTNotEnabled, this);
				}
				break;
			}
		}

		//--------------------------------------------------------------
		// PUBLIC METHODES
		//--------------------------------------------------------------
		public int WriteMessageEventReceived(byte[] writeBuf)
		{
			if(_textView1 != null)
				_textView1.SetText("MessageType = write", TextView.BufferType.Normal);

			return 0;
		}

		public int ReadMessageEventReceived(byte[] readBuf)
		{
			var message = new Java.Lang.String (readBuf);

			if(_textView1 != null)
				_textView1.SetText("MessageType = read", TextView.BufferType.Normal);
			if(_textView2 != null)
				_textView2.SetText(message, TextView.BufferType.Normal);

			return 0;
		}

		public int StateConnectingEventReceived()
		{
			if(_textView1 != null)
				_textView1.SetText ("State = connecting", TextView.BufferType.Normal);

			return 0;
		}

		public int StateConnectedEventReceived()
		{
			if(_textView1 != null)
				_textView1.SetText ("State = connected", TextView.BufferType.Normal);

			return 0;
		}

		public int StateNoneEventReceived()
		{
			if(_textView1 != null)
				_textView1.SetText ("State = none", TextView.BufferType.Normal);

			return 0;
		}

		public int DeviceNameEventReceived(string deviceName)
		{
			_connectedDeviceName = deviceName;
			if(_textView1 != null)
				_textView1.SetText("MessageType = read", TextView.BufferType.Normal);
			if(_textView2 != null)
				_textView2.SetText(deviceName, TextView.BufferType.Normal);

			return 0;
		}

		public int StartGameMessage(byte[] message)
		{
			// We have recieve a demand to start the game
			// We verify that the two player have the same version of the application
			if(message[1] == Constants.NumVersion1 && message[2] == Constants.NumVersion2)
			{
				// The 2 players have the same version, we can launch the game if we are ready
				if(_state == StartState.WAITING_FOR_OPPONENT)
					startGame();// We launch the game (change view and everything)
				else
					_state = StartState.OPPONENT_READY;
			}
			return 0;
		}



		//--------------------------------------------------------------
		// PRIVATES METHODES
		//--------------------------------------------------------------
		private bool enableBluetooth()
		{
			#if DEBUG
			Log.Debug(TAG, "enableBluetooth()");
			#endif

			// If the bluetooth is already enable and set
			if (Network.Instance.Enable())
				return true;

			// Get local Bluetooth adapter
			BluetoothAdapter bluetoothAdapter;
			bluetoothAdapter = BluetoothAdapter.DefaultAdapter;

			// If the adapter is null, then Bluetooth is not supported
			if(bluetoothAdapter == null)
			{
				#if DEBUG
				Log.Debug(TAG, "display of the alert");
				#endif

				Utils.ShowAlert(Resource.String.BTNotAvailableTitle, Resource.String.BTNotAvailable, this);
				return false;
			}
			else
			{
				// If the bluetooth is not enable, we try to activate it
				if(!bluetoothAdapter.IsEnabled)
				{
					#if DEBUG
					Log.Debug(TAG, "intent to activate bluetooth");
					#endif

					Intent enableIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
					StartActivityForResult(enableIntent,(int) RequestCode.REQUEST_ENABLE_BT);
				}
				else
				{
					#if DEBUG
					Log.Debug(TAG, "creation of BluetoothManager");
					#endif

					Network.Instance.EnableBluetooth();
					Network.Instance.CommunicationWay.Start();
				}
			}
			return true;
		}

		/*
		 * Start the MainActivity for the game
		 */
		private void startGame()
		{
			Intent intent = new Intent(this, typeof(MainActivity));
			StartActivity(intent);
		}
	}
}

