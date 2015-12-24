/*
* Inspired from 2009 The Android Open Source Project
* Bluetooth chat
*/

using System;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using System.Collections.Generic;

namespace Tetrim
{
	// This Activity appears as a dialog. It lists any paired devices and
	// devices detected in the area after discovery. When a device is chosen
	// by the user, the MAC address of the device is sent back to the parent
	// Activity in the result Intent.
	[Activity(Label = "Tetrim", Icon = "@drawable/icon", Theme = "@android:style/Theme.NoTitleBar.Fullscreen")]		
	public class BluetoothConnectionActivity : Activity, ViewTreeObserver.IOnGlobalLayoutListener
	{
		//--------------------------------------------------------------
		// CONSTANTS
		//--------------------------------------------------------------
		// Debugging
		private const string Tag = "DeviceListActivity";

		// Return Intent extra
		public const string ExtraDeviceAddress = "device_address";

		private int NbDevices = 6;
		private TetrisColor FriendsDeviceColor = TetrisColor.Pink;
		private TetrisColor PairedDeviceColor = TetrisColor.Green;
		private TetrisColor NewDeviceColor = TetrisColor.Blue;

		private enum StartState
		{
			NONE,
			WAITING_FOR_OPPONENT,
			OPPONENT_READY
		};

		private enum Menu
		{
			FRIENDS,
			PAIRED,
			NEW
		};

		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		private BluetoothAdapter _bluetoothAdapter;
		private static List<BluetoothDevice> _friendsDevices = new List<BluetoothDevice>();
		private static List<BluetoothDevice> _pairedDevices = new List<BluetoothDevice>();
		private static List<BluetoothDevice> _newDevices = new List<BluetoothDevice>();
		private Receiver _receiver;
		private StartState _state = StartState.NONE;
		private bool _isConnectionInitiator = false;
		private AlertDialog _currentDialog = null;
		private Menu _menuSelected = Menu.PAIRED;
		private ScrollView _devicesLayout;
		private LinearLayout _friendsDevicesLayout, _pairedDevicesLayout, _newDevicesLayout;
		private ButtonStroked _friendsDevicesButton, _pairedDevicesButton, _newDevicesButton;

		//--------------------------------------------------------------
		// METHODES OVERRIDE
		//--------------------------------------------------------------
		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			// Setup the window
			RequestWindowFeature(WindowFeatures.IndeterminateProgress);
			SetContentView(Resource.Layout.BluetoothDevices);

			// Set result CANCELED incase the user backs out
			SetResult(Result.Canceled);

			Typeface niceFont = Typeface.CreateFromAsset(Assets,"Foo.ttf");
			TextView selectDeviceText = FindViewById<TextView>(Resource.Id.selectDeviceText);
			selectDeviceText.SetTypeface(niceFont, TypefaceStyle.Normal);

			// Initialize the buttons
			SetupMenuButton(ref _friendsDevicesButton, Resource.Id.friendsDevices, FriendsDeviceColor);
			_friendsDevicesButton.Click +=(sender, e) => {
				SwitchMenu(Menu.FRIENDS);
			};
			SetupMenuButton(ref _pairedDevicesButton, Resource.Id.pairedDevices, PairedDeviceColor);
			_pairedDevicesButton.Click +=(sender, e) => {
				SwitchMenu(Menu.PAIRED);
			};
			SetupMenuButton(ref _newDevicesButton, Resource.Id.newDevices, NewDeviceColor);
			_newDevicesButton.Click +=(sender, e) => {
				SwitchMenu(Menu.NEW);
				startDiscovery();
			};

			// Create the layouts
			_devicesLayout = FindViewById<ScrollView>(Resource.Id.devicesLayout);
			SetupLayout(ref _friendsDevicesLayout, FriendsDeviceColor);
			SetupLayout(ref _pairedDevicesLayout, PairedDeviceColor);
			SetupLayout(ref _newDevicesLayout, NewDeviceColor);
			SwitchMenu(Menu.PAIRED);

			// Test if the view is created so we can resize the buttons
			if(_devicesLayout.ViewTreeObserver.IsAlive)
			{
				_devicesLayout.ViewTreeObserver.AddOnGlobalLayoutListener(this);
			}
		}

		public void OnGlobalLayout()
		{
			// The view is completely loaded now, so Height won't return 0

			// Register for broadcasts when a device is discovered
			_receiver = new Receiver(this);
			var filter = new IntentFilter(BluetoothDevice.ActionFound);
			RegisterReceiver(_receiver, filter);

			// Register for broadcasts when discovery has finished
			filter = new IntentFilter(BluetoothAdapter.ActionDiscoveryFinished);
			RegisterReceiver(_receiver, filter);

			if(Network.Instance.TryEnablingBluetooth(this) == Network.ResultEnabling.Enabled)
			{
				actualizeView();
			}

			// Hook on the Network event
			Network.Instance.EraseAllEvent();
			Network.Instance.ReadEvent += ReadMessageEventReceived;
			Network.Instance.StateConnectedEvent += StateConnectedEventReceived;
			Network.Instance.StateConnectingEvent += StateConnectingEventReceived;
			Network.Instance.StateNoneEvent += StateNoneEventReceived;
			Network.Instance.WriteEvent += WriteMessageEventReceived;
			Network.Instance.StartMessage += StartGameMessageReceived;

			// Destroy the onGlobalLayout afterwards, otherwise it keeps changing
			// the sizes non-stop, even though it's already done
			_devicesLayout.ViewTreeObserver.RemoveGlobalOnLayoutListener(this);
		}

		private void SetupMenuButton(ref ButtonStroked button, int id, TetrisColor color)
		{
			button = FindViewById<ButtonStroked>(id);
			button.StrokeColor = Utils.getAndroidDarkColor(color);
			button.FillColor = Utils.getAndroidColor(color);
		}

		private void SetupLayout(ref LinearLayout layout, TetrisColor color)
		{
			layout = new LinearLayout(this.BaseContext);
			layout.WeightSum = NbDevices;
			//layout.SetBackgroundColor(Utils.getAndroidDarkColor(color));
			layout.Orientation = Orientation.Vertical;
		}

		public void AddNewDevice(BluetoothDevice device)
		{
			_newDevices.Add(device);
			ButtonStroked newDeviceButton = CreateButton(device, NewDeviceColor);
			LinearLayout.LayoutParams lp = CreateLayoutParams();
			_newDevicesLayout.AddView(newDeviceButton, lp);
		}

		public void AddPairedDevice(BluetoothDevice device)
		{
			_pairedDevices.Add(device);
			ButtonStroked pairedDeviceButton = CreateButton(device, PairedDeviceColor);
			LinearLayout.LayoutParams lp = CreateLayoutParams();
			_pairedDevicesLayout.AddView(pairedDeviceButton, lp);
		}

		private ButtonStroked CreateButton(BluetoothDevice device, TetrisColor color)
		{
			ButtonStroked button = new ButtonStroked(this.BaseContext);
			button.SetMinimumHeight((int)(_devicesLayout.Height*1f/NbDevices));
			button.Tag = device.Address;
			button.Text = device.Name;
			button.StrokeColor = Utils.getAndroidColor(color);
			button.FillColor = Utils.getAndroidDarkColor(color);
			button.Gravity = GravityFlags.Left;
			int padding = Utils.GetPixelsFromDP(this.BaseContext, 20);
			button.SetPadding(padding, padding, padding, padding);
			button.StrokeBorderWidth = 20;
			button.StrokeTextWidth = 15;
			button.RadiusIn = 20;
			button.RadiusOut = 15;
			button.IsTextStroked = false;
			button.Shape = ButtonStroked.ButtonShape.BottomTop;
			button.Click += delegate {
				deviceListClick(button);
			};
			return button;
		}

		private LinearLayout.LayoutParams CreateLayoutParams()
		{
			LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, 0, 1);
			int margin = Utils.GetPixelsFromDP(this.BaseContext, 5);
			//lp.SetMargins(margin, margin, margin, margin);
			return lp;
		}

		private void SwitchMenu(Menu chosenMenu)
		{
			this._menuSelected = chosenMenu;
			switch(this._menuSelected)
			{
			case Menu.FRIENDS:
				DisableMenuCategory(_pairedDevicesLayout, _pairedDevicesButton);
				DisableMenuCategory(_newDevicesLayout, _newDevicesButton);
				EnableMenuCategory(_friendsDevicesLayout, _friendsDevicesButton);
				break;
			case Menu.PAIRED:
				DisableMenuCategory(_friendsDevicesLayout, _friendsDevicesButton);
				DisableMenuCategory(_newDevicesLayout, _newDevicesButton);
				EnableMenuCategory(_pairedDevicesLayout, _pairedDevicesButton);
				break;
			case Menu.NEW:
				DisableMenuCategory(_friendsDevicesLayout, _friendsDevicesButton);
				DisableMenuCategory(_pairedDevicesLayout, _pairedDevicesButton);
				EnableMenuCategory(_newDevicesLayout, _newDevicesButton);
				break;
			}
		}

		private void EnableMenuCategory(LinearLayout layout, ButtonStroked button)
		{
			_devicesLayout.AddView(layout, new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.WrapContent));
			button.Selected = true;
			button.Clickable = false;
		}

		private void DisableMenuCategory(LinearLayout layout, ButtonStroked button)
		{
			_devicesLayout.RemoveView(layout);
			button.Selected = false;
			button.Clickable = true;
		}

		protected override void OnResume ()
		{
			base.OnResume ();

			// Performing this check in onResume() covers the case in which Bluetooth was
			// not enabled when the button was hit, so we were paused to enable it...
			// onResume() will be called when ACTION_REQUEST_ENABLE activity returns.
			// Only if the state is STATE_NONE, do we know that we haven't started already
			if (Network.Instance.WaitingForStart())
			{
				// Start the Bluetooth for the game
				Network.Instance.CommunicationWay.Start();
			}
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			// Make sure we're not doing discovery anymore
			if(_bluetoothAdapter != null)
			{
				_bluetoothAdapter.CancelDiscovery();
			}

			// Unregister broadcast listeners
			UnregisterReceiver(_receiver);

			// Stop the Bluetooth Manager
			if (Network.Instance.Enabled())
				Network.Instance.CommunicationWay.Stop ();

			#if DEBUG
			Log.Error (Tag, "--- ON DESTROY ---");
			#endif
		}

		protected override void OnActivityResult (int requestCode, Result resultCode, Intent data)
		{
			#if DEBUG
			Log.Debug(Tag, "onActivityResult " + resultCode);
			#endif
			if(Network.Instance.ResultBluetoothActivation(requestCode, resultCode, this))
			{
				actualizeView();
			}
		}

		public void CancelConnection()
		{
			if(_currentDialog != null)
				_currentDialog.Dismiss();

			_currentDialog = null;
			_state = StartState.NONE;
			Network.Instance.CommunicationWay.Start();
		}

		//--------------------------------------------------------------
		// EVENT METHODES
		//--------------------------------------------------------------
		public int WriteMessageEventReceived(byte[] writeBuf)
		{
			#if DEBUG
			Log.Debug(Tag, "MessageType = write");
			#endif

			if(writeBuf[0] == Constants.IdMessageStart && _state == StartState.OPPONENT_READY)
			{
				MenuActivity.startGame(this);// We launch the game (change view and everything)
			}
				

			return 0;
		}

		public int ReadMessageEventReceived(byte[] readBuf)
		{
			var message = new Java.Lang.String (readBuf);

			#if DEBUG
			Log.Debug(Tag, "MessageType = read, " + message);
			#endif

			return 0;
		}

		public int StateConnectingEventReceived()
		{
			#if DEBUG
			Log.Debug(Tag, "State = connecting");
			#endif

			_isConnectionInitiator = true;
			return 0;
		}

		public int StateConnectedEventReceived()
		{
			#if DEBUG
			Log.Debug(Tag, "State = connected");
			#endif

			if(_isConnectionInitiator)
			{
				// Send a request to start a game
				SendStartGameMessage();
			}
			else
			{
				if(_currentDialog != null)
				{
					_currentDialog.Dismiss();
					_currentDialog = null;
				}

				// Display a pop-up asking if we want to play now that we are connected
				String message = String.Format(Resources.GetString(Resource.String.game_request), Network.Instance.CommunicationWay._device.Name);
				AlertDialog.Builder builder = new AlertDialog.Builder(this);
				builder.SetTitle(Resource.String.game_request_title);
				builder.SetMessage(message);
				builder.SetCancelable(false);
				builder.SetPositiveButton(Android.Resource.String.Yes, delegate{SendStartGameMessage(); _currentDialog = null;});
				builder.SetNegativeButton(Android.Resource.String.No, delegate{CancelConnection();});
				_currentDialog = builder.Create();
				_currentDialog.Show();
			}

			return 0;
		}

		public int StateNoneEventReceived()
		{
			#if DEBUG
			Log.Debug(Tag, "State = none");
			#endif

			if(_currentDialog != null)
			{
				// if the connection fail we remove the pop-up
				_currentDialog.Dismiss();
				_currentDialog = null;
			}

			_state = StartState.NONE;

			return 0;
   		}

		public int StartGameMessageReceived(byte[] message)
		{
			// We have recieve a demand to start the game
			// We verify that the two player have the same version of the application
			if(message[1] == Constants.NumVersion1 && message[2] == Constants.NumVersion2)
			{
				// The 2 players have the same version, we can launch the game if we are ready
				if(_state == StartState.WAITING_FOR_OPPONENT)
				{
					if(_currentDialog != null)
					{
						_currentDialog.Dismiss();
						_currentDialog = null;
					}
					MenuActivity.startGame(this);// We launch the game (change view and everything)
				}
				else
					_state = StartState.OPPONENT_READY;
			}
			else
			{
				Utils.ShowAlert(Resource.String.wrong_version_title, Resource.String.wrong_version, this);
				Network.Instance.CommunicationWay.Start(); // We restart the connection
			}
			return 0;
		}

		public int SendStartGameMessage()
		{
			byte[] message = {Constants.IdMessageStart, Constants.NumVersion1, Constants.NumVersion2};
			// We notify the opponent that we are ready
			Network.Instance.CommunicationWay.Write(message);

			if(_state != StartState.OPPONENT_READY)
			{
				_state = StartState.WAITING_FOR_OPPONENT;
				displayWaitingDialog(Resource.String.waiting_for_opponent);
			}

			return 0;
		}

		public void FinishDiscovery()
		{
			// TODO : Indicate the end of discovery

			// TODO : display if no device found
			// Resource.String.none_found
		}

		//--------------------------------------------------------------
		// PRIVATE METHODES
		//--------------------------------------------------------------
		private void actualizeView()
		{
			// Get the local Bluetooth adapter
			_bluetoothAdapter = BluetoothAdapter.DefaultAdapter;

			// Get a set of currently paired devices
			ICollection<BluetoothDevice> pairedDevices = _bluetoothAdapter.BondedDevices;

			// If there are paired devices, add each one to the ArrayAdapter
			if(pairedDevices.Count > 0)
			{
				_pairedDevices.Clear();
				foreach(BluetoothDevice device in pairedDevices)
				{
					AddPairedDevice(device);
				}
			}
			else
			{
				// TODO : display if no device found
				// Resource.String.none_paired
			}
		}

		private void displayWaitingDialog(int idMessage)
		{
			if(_currentDialog != null)
			{
				_currentDialog.Dismiss();
				_currentDialog = null;
			}
			AlertDialog.Builder builder = new AlertDialog.Builder(this);
			builder.SetMessage(idMessage);
			builder.SetCancelable(false);
			builder.SetNegativeButton(Android.Resource.String.Cancel, delegate{CancelConnection();});
			_currentDialog = builder.Create();
			_currentDialog.Show();
		}

		// Start device discover with the BluetoothAdapter
		private void startDiscovery()
		{
			#if DEBUG
			Log.Debug(Tag, "doDiscovery()");
			#endif

			// If we're already discovering, stop it
			if(_bluetoothAdapter.IsDiscovering)
			{
				_bluetoothAdapter.CancelDiscovery();
			}

			// Request discover from BluetoothAdapter
			_bluetoothAdapter.StartDiscovery();
		}

		// The on-click listener for all devices in the ListViews
		private void deviceListClick(ButtonStroked sender)
		{
			if(_bluetoothAdapter.IsDiscovering)
			{
				// Cancel discovery because it's costly and we're about to connect
				_bluetoothAdapter.CancelDiscovery();
			}

			// Get the device MAC address, which is the last 17 chars in the View
			//string info = (sender).Text;
			string address = (sender).Tag.ToString();

			if(Network.Instance.Enabled())
			{
				displayWaitingDialog(Resource.String.waiting_for_opponent);

				// Get the BLuetoothDevice object
				BluetoothDevice device = BluetoothAdapter.DefaultAdapter.GetRemoteDevice(address);
				// Attempt to connect to the device
				Network.Instance.CommunicationWay.Connect(device);
			}
		}
	}
}

