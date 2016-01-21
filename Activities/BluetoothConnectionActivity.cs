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
	[Activity(Label = "Tetrim", Icon = "@drawable/icon", Theme = "@android:style/Theme.NoTitleBar.Fullscreen", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]		
	public class BluetoothConnectionActivity : Activity, ViewTreeObserver.IOnGlobalLayoutListener
	{
		//--------------------------------------------------------------
		// CONSTANTS
		//--------------------------------------------------------------
		// Debugging
		private const string Tag = "BluetoothConnectionActivity";

		// Return Intent extra
		public const string ExtraDeviceAddress = "device_address";

		private int NbDevices = 6;
		private TetrisColor FriendsDeviceColor = TetrisColor.Pink;
		private TetrisColor PairedDeviceColor = TetrisColor.Green;
		private TetrisColor NewDeviceColor = TetrisColor.Blue;

		private enum Menu
		{
			FRIENDS,
			PAIRED,
			NEW
		};

		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		private BluetoothAdapter _bluetoothAdapter = null;
		private static List<BluetoothDevice> _friendsDevices = new List<BluetoothDevice>();
		private static List<BluetoothDevice> _pairedDevices = new List<BluetoothDevice>();
		private static List<BluetoothDevice> _newDevices = new List<BluetoothDevice>();
		private Receiver _receiver;
		private Network.StartState _state = Network.StartState.NONE;
		private bool _isConnectionInitiator = false;
		private string _opponentName = String.Empty;
		private Intent _currentDialog = null;
		private Menu _menuSelected = Menu.PAIRED;
		private ScrollView _devicesLayout;
		private LinearLayout _friendsDevicesLayout, _pairedDevicesLayout, _newDevicesLayout;
		private ButtonStroked _friendsDevicesButton, _pairedDevicesButton, _newDevicesButton;
		private ProgressBar _progressBar;

		//--------------------------------------------------------------
		// EVENT CATCHING OVERRIDE
		//--------------------------------------------------------------
		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			// Setup the window
			RequestWindowFeature(WindowFeatures.IndeterminateProgress);
			SetContentView(Resource.Layout.BluetoothConnection);

			// Set result CANCELED incase the user backs out
			SetResult(Result.Canceled);

			Typeface niceFont = Typeface.CreateFromAsset(Assets,"Foo.ttf");
			TextView selectDeviceText = FindViewById<TextView>(Resource.Id.selectDeviceText);
			selectDeviceText.SetTypeface(niceFont, TypefaceStyle.Normal);

			// Initialize the buttons
			UtilsUI.SetDeviceMenuButton(this, ref _friendsDevicesButton, Resource.Id.friendsDevices, FriendsDeviceColor);
			_friendsDevicesButton.Click +=(sender, e) => {
				SwitchMenu(Menu.FRIENDS);
			};
			UtilsUI.SetDeviceMenuButton(this, ref _pairedDevicesButton, Resource.Id.pairedDevices, PairedDeviceColor);
			_pairedDevicesButton.Click +=(sender, e) => {
				SwitchMenu(Menu.PAIRED);
			};
			UtilsUI.SetDeviceMenuButton(this, ref _newDevicesButton, Resource.Id.newDevices, NewDeviceColor);
			_newDevicesButton.Click +=(sender, e) => {
				SwitchMenu(Menu.NEW);
				startDiscovery();
			};

			// Create the layouts
			_devicesLayout = FindViewById<ScrollView>(Resource.Id.devicesLayout);
			UtilsUI.SetDeviceMenuLayout(this, ref _friendsDevicesLayout, NbDevices);
			UtilsUI.SetDeviceMenuLayout(this, ref _pairedDevicesLayout, NbDevices);
			UtilsUI.SetDeviceMenuLayout(this, ref _newDevicesLayout, NbDevices);
			SwitchMenu(Menu.FRIENDS);

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
			Network.Instance.DeviceNameEvent += NameReceived;
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

		protected override void OnResume ()
		{
			base.OnResume ();
			if (Network.Instance.WaitingForStart())
			{
				// Performing this check in onResume() covers the case in which Bluetooth was
				// not enabled when the button was hit, so we were paused to enable it...
				// onResume() will be called when ACTION_REQUEST_ENABLE activity returns.
				// Only if the state is STATE_NONE, do we know that we haven't started already

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
			if(requestCode == (int) Utils.RequestCode.RequestGameTwoPlayer)
			{
				// We end this activity after the game so we come back on the menu screen
				Finish();
			}
			if(Network.Instance.ResultBluetoothActivation(requestCode, resultCode, this))
			{
				actualizeView();
			}
		}

		//--------------------------------------------------------------
		// DEVICES
		//--------------------------------------------------------------
		public void AddNewDevice(BluetoothDevice device)
		{
			if(device != null)
			{
				_newDevices.Add(device);
			}
			ButtonStroked newDeviceButton = UtilsUI.CreateDeviceButton(this, device, NewDeviceColor, (int)(_devicesLayout.Height*1f/NbDevices), Resource.String.none_found);
			LinearLayout.LayoutParams lp = UtilsUI.CreateDeviceLayoutParams(this, 5);
			_newDevicesLayout.AddView(newDeviceButton, _newDevicesLayout.ChildCount - 2, lp);
		}

		public void AddFriendDevice(BluetoothDevice device)
		{
			if(device != null)
			{
				_friendsDevices.Add(device);
			}
			ButtonStroked friendsDeviceButton = UtilsUI.CreateDeviceButton(this, device, FriendsDeviceColor, (int)(_devicesLayout.Height*1f/NbDevices), Resource.String.none_friend);
			LinearLayout.LayoutParams lp = UtilsUI.CreateDeviceLayoutParams(this, 5);
			_friendsDevicesLayout.AddView(friendsDeviceButton, lp);
		}

		public void AddPairedDevice(BluetoothDevice device)
		{
			if(device != null)
			{
				_pairedDevices.Add(device);
			}
			ButtonStroked pairedDeviceButton = UtilsUI.CreateDeviceButton(this, device, PairedDeviceColor, (int)(_devicesLayout.Height*1f/NbDevices), Resource.String.none_paired);
			LinearLayout.LayoutParams lp = UtilsUI.CreateDeviceLayoutParams(this, 5);
			_pairedDevicesLayout.AddView(pairedDeviceButton, lp);
   		}

		//--------------------------------------------------------------
		// MENU BEHAVIOUR
		//--------------------------------------------------------------
		private void SwitchMenu(Menu chosenMenu)
		{
			this._menuSelected = chosenMenu;
			switch(this._menuSelected)
			{
			case Menu.FRIENDS:
				DisableMenuCategory(_pairedDevicesLayout, _pairedDevicesButton);
				DisableMenuCategory(_newDevicesLayout, _newDevicesButton);
				_newDevicesLayout.RemoveAllViews();
				EnableMenuCategory(_friendsDevicesLayout, _friendsDevicesButton);
				//_devicesLayout.SetBackgroundColor(Utils.getAndroidDarkColor(FriendsDeviceColor));
				break;
			case Menu.PAIRED:
				DisableMenuCategory(_friendsDevicesLayout, _friendsDevicesButton);
				DisableMenuCategory(_newDevicesLayout, _newDevicesButton);
				_newDevicesLayout.RemoveAllViews();
				EnableMenuCategory(_pairedDevicesLayout, _pairedDevicesButton);
				//_devicesLayout.SetBackgroundColor(Utils.getAndroidDarkColor(PairedDeviceColor));
				break;
			case Menu.NEW:
				DisableMenuCategory(_friendsDevicesLayout, _friendsDevicesButton);
				DisableMenuCategory(_pairedDevicesLayout, _pairedDevicesButton);
				EnableMenuCategory(_newDevicesLayout, _newDevicesButton);
				//_devicesLayout.SetBackgroundColor(Utils.getAndroidDarkColor(NewDeviceColor));
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

		//--------------------------------------------------------------
		// NETWORK METHODES
		//--------------------------------------------------------------
		public int WriteMessageEventReceived(byte[] writeBuf)
		{
			#if DEBUG
			Log.Debug(Tag, "MessageType = write");
			#endif

			if(writeBuf[0] == Constants.IdMessageStart && _state == Network.StartState.OPPONENT_READY)
			{
				User.Instance.AddFriend(Network.Instance.CommunicationWay._deviceAddress, _opponentName);
				MenuActivity.startGame(this, Utils.RequestCode.RequestGameTwoPlayer);// We launch the game (change view and everything)
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

			// No need to continue the discovery now that we are connected
			if(_bluetoothAdapter.IsDiscovering)
			{
				_bluetoothAdapter.CancelDiscovery();
			}

			if(_isConnectionInitiator)
			{
				// Send a request to start a game
				SendStartGameMessage();
			}
			else
			{
				if(_currentDialog != null)
				{
					DialogActivity.CloseAllDialog.Invoke();
					_currentDialog = null;
				}

				// Display a pop-up asking if we want to play now that we are connected
				_currentDialog = DialogActivity.CreateYesNoDialog(this, Resources.GetString(Resource.String.game_request_title),
					String.Format(Resources.GetString(Resource.String.game_request), _opponentName),
					delegate{_currentDialog = null; SendStartGameMessage();}, delegate{_currentDialog = null; CancelConnection();});
				StartActivity(_currentDialog);
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
				// if the connection fail we remove the pop-up and restart the bluetooth
				DialogActivity.CloseAllDialog.Invoke();
				_currentDialog = null;
				Network.Instance.CommunicationWay.Start();
			}

			_state = Network.StartState.NONE;
			_isConnectionInitiator = false;

			return 0;
   		}

		public int StartGameMessageReceived(byte[] message)
		{
			// We have recieve a demand to start the game
			// We verify that the two player have the same version of the application
			if(message[1] == Constants.NumVersion)
			{
				// The 2 players have the same version, we can launch the game if we are ready
				if(_state == Network.StartState.WAITING_FOR_OPPONENT)
				{
					if(_currentDialog != null)
					{
						DialogActivity.CloseAllDialog.Invoke();
						_currentDialog = null;
					}

					User.Instance.AddFriend(Network.Instance.CommunicationWay._deviceAddress, _opponentName);
					MenuActivity.startGame(this, Utils.RequestCode.RequestGameTwoPlayer); // We launch the game (change view and everything)
				}
				else
					_state = Network.StartState.OPPONENT_READY;
			}
			else
			{
				_currentDialog = UtilsDialog.CreateBluetoothDialogNoCancel(this, Resource.String.wrong_version);
				StartActivity(_currentDialog);
				Network.Instance.CommunicationWay.Start(); // We restart the connection
			}
			return 0;
		}

		public int SendStartGameMessage()
		{
			byte[] message = {Constants.IdMessageStart, Constants.NumVersion};
			// We notify the opponent that we are ready
			Network.Instance.CommunicationWay.Write(message);

			if(_state != Network.StartState.OPPONENT_READY)
			{
				_state = Network.StartState.WAITING_FOR_OPPONENT;
				displayWaitingDialog(Resource.String.waiting_for_opponent);
			}

			return 0;
		}

		public int NameReceived(string name)
		{
			_opponentName = name;
			return 0;
		}

		public void FinishDiscovery()
		{
			// Remove the progress bar
			_newDevicesLayout.RemoveView(_progressBar);

			// Display if no device found
			AddNewDevice(null);
		}

		public void CancelConnection()
		{
			#if DEBUG
			Log.Debug(Tag, "CancelConnection()");
			#endif

			if(_currentDialog != null)
			{
				DialogActivity.CloseAllDialog.Invoke();
				_currentDialog = null;
			}

			_state = Network.StartState.NONE;
			Network.Instance.CommunicationWay.Start();
		}

		// The on-click listener for all devices in the ListViews
		public void DeviceListClick(ButtonStroked sender)
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
					if(User.Instance.Friends.ContainsKey(device.Address))
					{
						AddFriendDevice(device);
						_friendsDevices.Add(device);
					}
					AddPairedDevice(device);
				}
			}
			else
			{
				AddPairedDevice(null);
			}

			if(_friendsDevices.Count == 0)
			{
				AddFriendDevice(null);
			}
		}

		private void displayWaitingDialog(int idMessage)
		{
			if(_currentDialog != null)
			{
				DialogActivity.CloseAllDialog.Invoke();
				_currentDialog = null;
			}

			_currentDialog = DialogActivity.CreateYesNoDialog(this, -1, idMessage,
				Resource.String.cancel, -1, delegate{CancelConnection();}, null);
			StartActivity(_currentDialog);
		}

		// Start device discover with the BluetoothAdapter
		private void startDiscovery()
		{
			#if DEBUG
			Log.Debug(Tag, "doDiscovery()");
			#endif

			_progressBar = new ProgressBar(BaseContext);
			_progressBar.Indeterminate = true;
			int padding = Utils.GetPixelsFromDP(BaseContext, 15);
			_progressBar.SetPadding(padding, padding, padding, padding);
			_newDevicesLayout.AddView(_progressBar, LinearLayout.LayoutParams.MatchParent, (int)(_devicesLayout.Height*1f/NbDevices));

			// If we're already discovering, stop it
			if(_bluetoothAdapter.IsDiscovering)
			{
				_bluetoothAdapter.CancelDiscovery();
			}

			// Request discover from BluetoothAdapter
			_bluetoothAdapter.StartDiscovery();
   		}
	}
}

