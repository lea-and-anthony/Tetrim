using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;

namespace Tetrim
{
	[Activity(Theme = "@style/Theme.TetrimDialogTheme", ScreenOrientation = ScreenOrientation.Portrait)]
	public class ReconnectActivity : DialogActivity
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
		private bool _connectingOccured = false;
		private bool isDialogDisplayed = false;
		private Network.StartState _state = Network.StartState.NONE;
		private readonly object _locker = new object (); // locker on _state because this variable can be modified in several threads

		//--------------------------------------------------------------
		// EVENT CATCHING METHODS
		//--------------------------------------------------------------
		protected override void OnCreate (Bundle bundle)
		{
			Builder = new DialogBuilder(this);
			Builder.PositiveText = null;
			Builder.PositiveAction = null;
			Builder.NegativeText = null;
			Builder.NegativeAction = null;
			Builder.Message = Resources.GetString(Resource.String.reconnect_activity);
			Builder.Title = null;

			base.OnCreate (bundle);

			_root.SetMinimumWidth(0);
			_root.SetMinimumHeight(0);
			CloseAllDialog -= Finish;

			// Set result CANCELED in case the user backs out
			SetResult (Android.App.Result.Canceled);

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

			if(_messageFail != null && Network.Instance.Connected)
			{
				Network.Instance.CommunicationWay.Write(_messageFail);
			}

			// We unsubscribe from the events
			Network.Instance.StateConnectedEvent -= OnConnected;
			Network.Instance.StateConnectingEvent -= OnConnecting;
			Network.Instance.StateNoneEvent -= OnFail;
			Network.Instance.RestartMessage -= OnRestartReceived;
			Network.Instance.WriteEvent -= WriteMessageEventReceived;
		}

		protected override void OnActivityResult(int requestCode, Android.App.Result resultCode, Intent data)
		{
			#if DEBUG
			Log.Debug(Tag, "onActivityResult " + resultCode);
			#endif
			if(Network.Instance.ResultBluetoothActivation(requestCode, resultCode, this))
			{
				tryConnection();
			}
			else if(requestCode == (int) Utils.RequestCode.RequestEnableBluetooth)
			{
				Finish();
			}
		}

		//--------------------------------------------------------------
		// PRIVATES METHODS
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

			if(_deviceAddress != Network.Instance.CommunicationWay._deviceAddress)
			{
				// We are connected to the wrong device so we stop the current connection
				Network.Instance.CommunicationWay.Start();
				return 1;
			}

			DialogActivity.CloseAll();

			// Now we display a pop-up asking if we want to continue the game
			Intent intent = DialogActivity.CreateYesNoDialog(this, Resource.String.connection_back, -1,
				delegate {sendRestart();}, delegate {Finish();});
			StartActivity(intent);

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
				SetResult(Android.App.Result.Canceled, null);

				if(!isDialogDisplayed)
				{
					isDialogDisplayed = true;
					Intent intent = DialogActivity.CreateYesNoDialog(this, Resource.String.retry_connection, -1,
						delegate{isDialogDisplayed = false; restartBluetooth();}, delegate{isDialogDisplayed = false; Finish();});
					StartActivity(intent);
				}
				_connectingOccured = false;
			}
			_state = Network.StartState.NONE;

			return 0;
		}

		private int OnRestartReceived()
		{
			lock (_locker)
			{
				if(_state != Network.StartState.WAITING_FOR_OPPONENT)
				{
					_state = Network.StartState.OPPONENT_READY;
				}
				else
				{
					// Set result and finish this Activity
					SetResult(Android.App.Result.Ok, null);
					Finish();
				}
			}
			return 0;
		}

		public int WriteMessageEventReceived(byte[] writeBuf)
		{
			if(writeBuf[0] == Constants.IdMessageRestart)
			{
				lock (_locker)
				{
					if(_state == Network.StartState.OPPONENT_READY)
					{
						// Set result and finish this Activity
						SetResult(Android.App.Result.Ok, null);
						Finish();
					}
					else if(_state == Network.StartState.NONE)
					{
						_state = Network.StartState.WAITING_FOR_OPPONENT;
					}
				}
			}
			return 0;
		}

		private void sendRestart()
		{
			byte[] message = {Constants.IdMessageRestart};
			// We notify the opponent that we are ready
			Network.Instance.CommunicationWay.Write(message);
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
			if(Network.Instance.WaitingForConnection && _deviceAddress != string.Empty)
			{
				if(Network.Instance.RoleInLastConnection == Network.ConnectionRole.Master)
				{
					// Attempt to connect to the previous device (only one device should try this)
					BluetoothDevice device = BluetoothAdapter.DefaultAdapter.GetRemoteDevice(_deviceAddress);
					Network.Instance.CommunicationWay.Connect(device);
				}
			}
			else
			{
				OnFail();
			}
		}
	}
}

