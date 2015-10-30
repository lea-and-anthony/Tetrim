/*
* Copyright(C) 2009 The Android Open Source Project
*
* Licensed under the Apache License, Version 2.0(the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Tetrim
{
	// This Activity appears as a dialog. It lists any paired devices and
	// devices detected in the area after discovery. When a device is chosen
	// by the user, the MAC address of the device is sent back to the parent
	// Activity in the result Intent.
	[Activity(Label = "Tetrim", Icon = "@drawable/icon", Theme = "@android:style/Theme.NoTitleBar.Fullscreen")]		
	public class BluetoothConnectionActivity : Activity
	{
		//--------------------------------------------------------------
		// CONSTANTS
		//--------------------------------------------------------------
		// Debugging
		private const string Tag = "DeviceListActivity";

		// Return Intent extra
		public const string ExtraDeviceAddress = "device_address";

		private enum RequestCode
		{
			RequestEnableBluetooth = 1
		};

		private enum StartState
		{
			NONE,
			WAITING_FOR_OPPONENT,
			OPPONENT_READY
		};

		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		private BluetoothAdapter _bluetoothAdapter;
		private static ArrayAdapter<string> _pairedDevicesArrayAdapter;
		private static ArrayAdapter<string> _newDevicesArrayAdapter;
		private Receiver _receiver;
		private StartState _state = StartState.NONE;
		private bool _isConnectionInitiator = false;
		private String _connectedDeviceName = String.Empty;

		//--------------------------------------------------------------
		// METHODES OVERRIDE
		//--------------------------------------------------------------
		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			// Setup the window
			RequestWindowFeature(WindowFeatures.IndeterminateProgress);
			SetContentView(Resource.Layout.BluetoothConnection);

			// Set result CANCELED incase the user backs out
			SetResult(Result.Canceled);

			// Initialize the button to perform device discovery			
			var scanButton = FindViewById<Button>(Resource.Id.button_scan);
			scanButton.Click +=(sender, e) => {
				startDiscovery();
			};

			// Initialize array adapters. One for already paired devices and
			// one for newly discovered devices
			_pairedDevicesArrayAdapter = new ArrayAdapter<string>(this, Resource.Layout.device_name);
			_newDevicesArrayAdapter = new ArrayAdapter<string>(this, Resource.Layout.device_name);

			// Find and set up the ListView for paired devices
			var pairedListView = FindViewById<ListView>(Resource.Id.paired_devices);
			pairedListView.Adapter = _pairedDevicesArrayAdapter;
			pairedListView.ItemClick += deviceListClick;

			// Find and set up the ListView for newly discovered devices
			var newDevicesListView = FindViewById<ListView>(Resource.Id.new_devices);
			newDevicesListView.Adapter = _newDevicesArrayAdapter;
			newDevicesListView.ItemClick += deviceListClick;

			// Register for broadcasts when a device is discovered
			_receiver = new Receiver(this, ref _newDevicesArrayAdapter);
			var filter = new IntentFilter(BluetoothDevice.ActionFound);
			RegisterReceiver(_receiver, filter);

			// Register for broadcasts when discovery has finished
			filter = new IntentFilter(BluetoothAdapter.ActionDiscoveryFinished);
			RegisterReceiver(_receiver, filter);

			// Get the local Bluetooth adapter
			_bluetoothAdapter = BluetoothAdapter.DefaultAdapter;

			// Get a set of currently paired devices
			var pairedDevices = _bluetoothAdapter.BondedDevices;

			// If there are paired devices, add each one to the ArrayAdapter
			if(pairedDevices.Count > 0)
			{
				FindViewById<View>(Resource.Id.title_paired_devices).Visibility = ViewStates.Visible;
				foreach(var device in pairedDevices)
				{
					_pairedDevicesArrayAdapter.Add(device.Name + "\n" + device.Address);
				}
			}
			else
			{
				String noDevices = Resources.GetText(Resource.String.none_paired);
				_pairedDevicesArrayAdapter.Add(noDevices);	
			}

			enableBluetooth();

			// Hook on the Network event
			Network.Instance.DeviceNameEvent += DeviceNameEventReceived;
			Network.Instance.ReadEvent += ReadMessageEventReceived;
			Network.Instance.StateConnectedEvent += StateConnectedEventReceived;
			Network.Instance.StateConnectingEvent += StateConnectingEventReceived;
			Network.Instance.StateNoneEvent += StateNoneEventReceived;
			Network.Instance.WriteEvent += WriteMessageEventReceived;
			Network.Instance.StartMessage += StartGameMessageReceived;
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
			if (Network.Instance.Enable())
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

			switch(requestCode)
			{
			case (int) RequestCode.RequestEnableBluetooth:
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
					#if DEBUG
					Log.Debug(Tag, "Bluetooth not enabled");
					#endif
					Utils.ShowAlert(Resource.String.BTNotEnabledTitle, Resource.String.BTNotEnabled, this);
				}
				break;
			}
		}

		//--------------------------------------------------------------
		// EVENT METHODES
		//--------------------------------------------------------------
		public int WriteMessageEventReceived(byte[] writeBuf)
		{
			#if DEBUG
			Log.Debug(Tag, "MessageType = write");
			#endif

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
				RunOnUiThread(() => {
					AlertDialog.Builder builder1 = new AlertDialog.Builder(this);
					builder1.SetTitle("Game Request");
					builder1.SetMessage("Do you want to play ?");
					builder1.SetCancelable(false);
					builder1.SetPositiveButton("Yes", delegate{SendStartGameMessage();});
					builder1.SetNegativeButton("No", delegate{Network.Instance.CommunicationWay.Stop();});
					AlertDialog alert11 = builder1.Create();
					alert11.Show();
				});
			}

			return 0;
		}

		public int StateNoneEventReceived()
		{
			#if DEBUG
			Log.Debug(Tag, "State = none");
			#endif

			return 0;
		}

		public int DeviceNameEventReceived(string deviceName)
		{
			_connectedDeviceName = deviceName;

			#if DEBUG
			Log.Debug(Tag, "MessageType = read, " + deviceName);
			#endif

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
					MenuActivity.startGame(this);// We launch the game (change view and everything)
				else
					_state = StartState.OPPONENT_READY;
			}
			return 0;
		}

		public int SendStartGameMessage()
		{
			byte[] message = {Constants.IdMessageStart, Constants.NumVersion1, Constants.NumVersion2};
			// We notify the opponent that we are ready
			Network.Instance.CommunicationWay.Write(message);

			if(_state == StartState.OPPONENT_READY)
				MenuActivity.startGame(this);// We launch the game (change view and everything)
			else
				_state = StartState.WAITING_FOR_OPPONENT;

			return 0;
		}

		public void FinishDiscovery()
		{
			// Indicate the end of discovery
			var scanButton = FindViewById<Button>(Resource.Id.button_scan);
			scanButton.Text = Resources.GetString(Resource.String.select_device);
			scanButton.Clickable = true;
		}

		//--------------------------------------------------------------
		// PRIVATE METHODES
		//--------------------------------------------------------------
		private bool enableBluetooth()
		{
			#if DEBUG
			Log.Debug(Tag, "enableBluetooth()");
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
				Log.Debug(Tag, "display of the alert");
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
					Log.Debug(Tag, "intent to activate bluetooth");
					#endif

					Intent enableIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
					StartActivityForResult(enableIntent,(int) RequestCode.RequestEnableBluetooth);
				}
				else
				{
					#if DEBUG
					Log.Debug(Tag, "creation of BluetoothManager");
					#endif

					Network.Instance.EnableBluetooth();
					Network.Instance.CommunicationWay.Start();
				}
			}
			return true;
		}

		// Start device discover with the BluetoothAdapter
		private void startDiscovery()
		{
			#if DEBUG
			Log.Debug(Tag, "doDiscovery()");
			#endif

			// Indicate scanning
			var scanButton = FindViewById<Button>(Resource.Id.button_scan);
			scanButton.Text = Resources.GetString(Resource.String.scanning);
			scanButton.Clickable = false;

			// Turn on sub-title for new devices
			FindViewById<View>(Resource.Id.title_new_devices).Visibility = ViewStates.Visible;	

			// If we're already discovering, stop it
			if(_bluetoothAdapter.IsDiscovering)
			{
				_bluetoothAdapter.CancelDiscovery();
			}

			// Request discover from BluetoothAdapter
			_bluetoothAdapter.StartDiscovery();
		}

		// The on-click listener for all devices in the ListViews
		private void deviceListClick(object sender, AdapterView.ItemClickEventArgs e)
		{
			// Cancel discovery because it's costly and we're about to connect
			_bluetoothAdapter.CancelDiscovery();

			// Get the device MAC address, which is the last 17 chars in the View
			var info =(e.View as TextView).Text.ToString();
			var address = info.Substring(info.Length - 17);

			if(Network.Instance.Enable())
			{
				// Get the BLuetoothDevice object
				BluetoothDevice device = BluetoothAdapter.DefaultAdapter.GetRemoteDevice(address);
				// Attempt to connect to the device
				Network.Instance.CommunicationWay.Connect(device);
			}
		}
	}
}

