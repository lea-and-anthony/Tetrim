using System;
using System.Reflection;
using System.ComponentModel;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Tetrim
{
	// TODO: check if necessary to add this setting:
	//ConfigurationChanges=Android.Content.PM.ConfigChanges.KeyboardHidden | Android.Content.PM.ConfigChanges.Orientation)]	
	[Activity(Label = "@string/reconnect_activity", Icon = "@drawable/icon", Theme = "@android:style/Theme.Dialog", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
	public class ReconnectActivity : Activity
	{
		//--------------------------------------------------------------
		// CONSTANTS
		//--------------------------------------------------------------
		private const string Tag = "Tetrim-ReconnectActivity";

		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		public static byte[] _messageFail { get; set; }
		private string _deviceAddress = string.Empty;
		private AlertDialog.Builder _builder = null;
		private AlertDialog _alertDialog = null;
		private bool _connectingOccured = false;
		private Network.StartState _state = Network.StartState.NONE;

		//--------------------------------------------------------------
		// EVENT CATCHING METHODES
		//--------------------------------------------------------------
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Set result CANCELED in case the user backs out
			SetResult (Result.Canceled);

			// We retrieve the old device we were connected to so we can try to reconnect to it later
			// (this value will be reinitialized during enabling)
			_deviceAddress = Network.Instance.CommunicationWay._deviceAddress;

			// We hook on the event before trying to re-enable the bluetooth
			Network.Instance.StateConnectedEvent += OnConnected;
			Network.Instance.StateConnectingEvent += OnConnecting;
			Network.Instance.StateNoneEvent += OnFail;
			Network.Instance.RestartMessage += OnRestartReceived;
			Network.Instance.WriteEvent += WriteMessageEventReceived;

			// We restart the bluetooth manager and then we try to reconnect
			restartBluetooth();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			// We unsubscribe from the events
			Network.Instance.StateConnectedEvent -= OnConnected;
			Network.Instance.StateConnectingEvent -= OnConnecting;
			Network.Instance.StateNoneEvent -= OnFail;
			Network.Instance.RestartMessage -= OnRestartReceived;
			Network.Instance.WriteEvent += WriteMessageEventReceived;
		}

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
		{
			#if DEBUG
			Log.Debug(Tag, "onActivityResult " + resultCode);
			#endif
			if(Network.Instance.ResultBluetoothActivation(requestCode, resultCode, this))
			{
				tryConnection();
			}
			else
			{
				Finish();
			}
		}

		//--------------------------------------------------------------
		// PRIVATES METHODES
		//--------------------------------------------------------------
		private int OnConnecting()
		{
			_connectingOccured = true;
			return 0;
		}

		private int OnConnected()
		{
			#if DEBUG
			Log.Debug(Tag, "OnConnected");
			#endif

			if(_messageFail != null)
			{
				Network.Instance.CommunicationWay.Write(_messageFail);
			}

			if(_alertDialog != null)
			{
				_alertDialog.Dismiss();
				_alertDialog = null;
			}

			// Now we display a pop-up asking if we want to continue the game
			AlertDialog.Builder builder = new AlertDialog.Builder(this);
			builder.SetTitle(Resource.String.connection_back_title);
			builder.SetMessage(Resource.String.connection_back);
			builder.SetCancelable(false);
			builder.SetPositiveButton(Android.Resource.String.Yes, delegate{sendRestart();});
			builder.SetNegativeButton(Android.Resource.String.No, delegate{Finish();});

			AlertDialog alert = builder.Create();
			alert.Show();

			return 0;
		}

		private int OnFail()
		{
			#if DEBUG
			Log.Debug(Tag, "Fail");
			#endif

			if(_connectingOccured)
			{
				// Reset result and asking if we retry or finish this Activity
				SetResult(Result.Canceled, null);

				if(_builder == null)
				{
					_builder = new AlertDialog.Builder(this);
					_builder.SetTitle(Resource.String.retry_connection_title);
					_builder.SetMessage(Resource.String.retry_connection);
					_builder.SetCancelable(false);
					_builder.SetPositiveButton(Android.Resource.String.Yes, delegate{_alertDialog = null; restartBluetooth();});
					_builder.SetNegativeButton(Android.Resource.String.No, delegate{_alertDialog = null; Finish();});
				}

				if(_alertDialog == null)
				{
					_alertDialog = _builder.Create();
					_alertDialog.Show();
				}
				_connectingOccured = false;
			}

			return 0;
		}

		private int OnRestartReceived()
		{
			if(_state != Network.StartState.WAITING_FOR_OPPONENT)
			{
				_state = Network.StartState.OPPONENT_READY;
			}
			else
			{
				// Set result and finish this Activity
				SetResult(Result.Ok, null);
				Finish();
			}
			return 0;
		}

		public int WriteMessageEventReceived(byte[] writeBuf)
		{
			if(writeBuf[0] == Constants.IdMessageRestart && _state == Network.StartState.OPPONENT_READY)
			{
				// Set result and finish this Activity
				SetResult(Result.Ok, null);
				Finish();
			}
			return 0;
		}

		private void sendRestart()
		{
			byte[] message = {Constants.IdMessageRestart};
			// We notify the opponent that we are ready
			Network.Instance.CommunicationWay.Write(message);
			if(_state == Network.StartState.NONE)
			{
				_state = Network.StartState.WAITING_FOR_OPPONENT;
			}
		}

		private void restartBluetooth()
		{
			Network.Instance.DisableBluetooth();

			// This function terminates the activity if there isn't any bluetooth available
			Network.ResultEnabling result = Network.Instance.TryEnablingBluetooth(this);
			if(result == Network.ResultEnabling.Enabled)
				tryConnection();
		}

		private void tryConnection()
		{
			if(Network.Instance.WaitingForConnection() && _deviceAddress != string.Empty)
			{
				// Attempt to connect to the previous device
				BluetoothDevice device = BluetoothAdapter.DefaultAdapter.GetRemoteDevice(_deviceAddress);
				Network.Instance.CommunicationWay.Connect(device);
			}
			else
			{
				OnFail();
			}
		}
	}
}

