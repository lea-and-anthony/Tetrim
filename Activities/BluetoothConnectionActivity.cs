/*
* Inspired from 2009 The Android Open Source Project
* Bluetooth chat
*/

using System;
using System.Collections.Generic;

using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Tetrim
{
	[Activity(ScreenOrientation = ScreenOrientation.Portrait)]		
	public class BluetoothConnectionActivity : Activity, ViewTreeObserver.IOnGlobalLayoutListener
	{
		//--------------------------------------------------------------
		// CONSTANTS
		//--------------------------------------------------------------
		// Debugging
		private const string Tag = "BluetoothConnectionActivity";

		// Return Intent extra
		public const string ExtraDeviceAddress = "device_address";

		private const int NbDevicesDisplayed = 6;
		private const TetrisColor FriendsDeviceColor = TetrisColor.Magenta;
		private const TetrisColor PairedDeviceColor = TetrisColor.Green;
		private const TetrisColor NewDeviceColor = TetrisColor.Blue;

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

			// Initialize the buttons
			UtilsUI.SetDeviceMenuButton(this, ref _friendsDevicesButton, Resource.Id.friendsDevices, FriendsDeviceColor);
			_friendsDevicesButton.Click += (sender, e) => SwitchMenu (Menu.FRIENDS);
			UtilsUI.SetDeviceMenuButton(this, ref _pairedDevicesButton, Resource.Id.pairedDevices, PairedDeviceColor);
			_pairedDevicesButton.Click += (sender, e) => SwitchMenu (Menu.PAIRED);
			UtilsUI.SetDeviceMenuButton(this, ref _newDevicesButton, Resource.Id.newDevices, NewDeviceColor);
			_newDevicesButton.Click += (sender, e) => {
				SwitchMenu(Menu.NEW);
				startDiscovery();
			};

			// Create the layouts
			_devicesLayout = FindViewById<ScrollView>(Resource.Id.devicesLayout);
			UtilsUI.SetDeviceMenuLayout(this, ref _friendsDevicesLayout, NbDevicesDisplayed);
			UtilsUI.SetDeviceMenuLayout(this, ref _pairedDevicesLayout, NbDevicesDisplayed);
			UtilsUI.SetDeviceMenuLayout(this, ref _newDevicesLayout, NbDevicesDisplayed);
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
			if (Network.Instance.Enabled)
				Network.Instance.Stop ();

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
				if(resultCode == Result.FirstUser)
				{
					// We want to restart a game
					MenuActivity.startTwoPlayerGame(this, _opponentName); // We launch the game (change view and everything)
				}
				else
				{
					// We end this activity after the game so we come back on the menu screen
					Finish();
				}
			}
			// The request code is tested in ResultBluetoothActivation
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
			ButtonStroked newDeviceButton = UtilsUI.CreateDeviceButton(this, device, NewDeviceColor, (int)(_devicesLayout.Height*1f/NbDevicesDisplayed), Resource.String.none_found);
			LinearLayout.LayoutParams lp = UtilsUI.CreateDeviceLayoutParams(this, 5);
			_newDevicesLayout.AddView(newDeviceButton, _newDevicesLayout.ChildCount - 1, lp);
		}

		public void AddFriendDevice(BluetoothDevice device, string name)
		{
			if(device != null)
			{
				_friendsDevices.Add(device);
			}
			ButtonStroked friendsDeviceButton = UtilsUI.CreateDeviceButton(this, device, FriendsDeviceColor, (int)(_devicesLayout.Height*1f/NbDevicesDisplayed), name);
			LinearLayout.LayoutParams lp = UtilsUI.CreateDeviceLayoutParams(this, 5);
			_friendsDevicesLayout.AddView(friendsDeviceButton, lp);
		}

		public void AddPairedDevice(BluetoothDevice device)
		{
			if(device != null)
			{
				_pairedDevices.Add(device);
			}
			ButtonStroked pairedDeviceButton = UtilsUI.CreateDeviceButton(this, device, PairedDeviceColor, (int)(_devicesLayout.Height*1f/NbDevicesDisplayed), Resource.String.none_paired);
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
				break;
			case Menu.PAIRED:
				DisableMenuCategory(_friendsDevicesLayout, _friendsDevicesButton);
				DisableMenuCategory(_newDevicesLayout, _newDevicesButton);
				_newDevicesLayout.RemoveAllViews();
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
				MenuActivity.startTwoPlayerGame(this, _opponentName);
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

			// Send the name of the user anyway
			SendNameMessage();

			if(_isConnectionInitiator)
			{
				// Send a request to start a game
				SendStartGameMessage();
			}
			else
			{
				// Dislay a pop-up telling that there is a connection which was made with someone
				// The pop-up is closed as soon as we received a StartGameMessage
				displayWaitingDialog(Resource.String.connection_in_progress);
			}

			return 0;
		}

		public int StateNoneEventReceived()
		{
			#if DEBUG
			Log.Debug(Tag, "State = none");
			#endif

			// To avoid infinite loop because this function can throw a StateNoneEvent
			Network.Instance.StateNoneEvent -= StateNoneEventReceived;

			if(DialogActivity.CloseAllDialog != null)
			{
				// if the connection fail we remove the pop-up and restart the bluetooth
				DialogActivity.CloseAllDialog.Invoke();
			}

			Network.Instance.CommunicationWay.Start();

			if(Network.Instance.CommunicationWay.State == BluetoothManager.StateEnum.None)
			{
				// if the bluetooth is still disabled, we need to stop
				Finish();
			}

			_state = Network.StartState.NONE;
			_isConnectionInitiator = false;

			Network.Instance.StateNoneEvent += StateNoneEventReceived;

			return 0;
   		}

		public int StartGameMessageReceived(byte[] message)
		{
			// Remove the pop-up in all the case
			if(DialogActivity.CloseAllDialog != null)
			{
				DialogActivity.CloseAllDialog.Invoke();
			}

			// We have recieve a demand to start the game
			// We verify that the two player have the same version of the application
			if(message[1] == Constants.NumVersion)
			{
				// The 2 players have the same version, we can launch the game if we are ready
				if(_state == Network.StartState.WAITING_FOR_OPPONENT)
				{
					User.Instance.AddFriend(Network.Instance.CommunicationWay._deviceAddress, _opponentName);
					MenuActivity.startTwoPlayerGame(this, _opponentName);
				}
				else
				{
					// if we are not ready, display a pop-up asking if we want to play with this person
					_state = Network.StartState.OPPONENT_READY;

					// Display a pop-up asking if we want to play now that we are connected and the opponent is ready
					Intent dialog = DialogActivity.CreateYesNoDialog(this, Resources.GetString(Resource.String.game_request_title),
						String.Format(Resources.GetString(Resource.String.game_request), _opponentName),
						delegate{SendStartGameMessage();}, delegate{CancelConnection();});
					StartActivity(dialog);
				}
			}
			else
			{
				Intent dialog = UtilsDialog.CreateBluetoothDialogNoCancel(this, Resource.String.wrong_version);
				StartActivity(dialog);
				Network.Instance.CommunicationWay.Start(); // We restart the connection
			}
			return 0;
		}

		public void SendStartGameMessage()
		{
			byte[] message = {Constants.IdMessageStart, Constants.NumVersion};
			// We notify the opponent that we are ready
			Network.Instance.CommunicationWay.Write(message);

			if(_state != Network.StartState.OPPONENT_READY)
			{
				_state = Network.StartState.WAITING_FOR_OPPONENT;
				displayWaitingDialog(Resource.String.waiting_for_opponent);
			}
		}

		public void SendNameMessage()
		{
			byte[] message = new byte[Constants.SizeMessage[Constants.IdMessageName]];
			message[0] = Constants.IdMessageName;

			char[] smallName = User.Instance.UserName.ToCharArray();
			char[] name = new char[Constants.MaxLengthName]; // the array is already initialized with the default value which is '\0'
			Buffer.BlockCopy(smallName, 0, name, 0, sizeof(char)*smallName.Length);
			for(int i = 0; i < name.Length; i++)
			{
				Utils.AddByteArrayToOverArray(ref message, BitConverter.GetBytes(name[i]), 1 + i*sizeof(char));
			}

			Network.Instance.CommunicationWay.Write(message);
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
			if(_newDevices.Count == 0)
			{
				AddNewDevice(null);
			}
		}

		public void CancelConnection()
		{
			#if DEBUG
			Log.Debug(Tag, "CancelConnection()");
			#endif

			if(DialogActivity.CloseAllDialog != null)
			{
				DialogActivity.CloseAllDialog.Invoke();
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

			if(Network.Instance.Enabled)
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
				string name;
				foreach(BluetoothDevice device in pairedDevices)
				{
					if(User.Instance.Friends.TryGetValue(device.Address, out name))
					{
						AddFriendDevice(device, name);
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
				AddFriendDevice(null, Resources.GetString(Resource.String.none_friend));
			}
		}

		private void displayWaitingDialog(int idMessage)
		{
			if(DialogActivity.CloseAllDialog != null)
			{
				DialogActivity.CloseAllDialog.Invoke();
			}

			Intent dialog = DialogActivity.CreateYesNoDialog(this, idMessage, -1,
				Resource.String.cancel, -1, delegate{CancelConnection();}, null);
			StartActivity(dialog);
		}

		// Start device discover with the BluetoothAdapter
		private void startDiscovery()
		{
			#if DEBUG
			Log.Debug(Tag, "doDiscovery()");
			#endif

			_progressBar = new ProgressBar(this);
			_progressBar.Indeterminate = true;
			int padding = Utils.GetPixelsFromDP(this, 15);
			_progressBar.SetPadding(padding, padding, padding, padding);
			_newDevicesLayout.SetGravity(GravityFlags.CenterHorizontal);
			_newDevicesLayout.AddView(_progressBar, (int)(_devicesLayout.Height*1f/NbDevicesDisplayed), (int)(_devicesLayout.Height*1f/NbDevicesDisplayed));

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

