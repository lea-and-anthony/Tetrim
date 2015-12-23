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
		public byte[] _messageFail { get; set; }
		private BluetoothDevice _device = null;

		//--------------------------------------------------------------
		// EVENT CATCHING METHODES
		//--------------------------------------------------------------
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			_messageFail = null;

			TextView message = new TextView(this);
			message.Text = Resources.GetString(Resource.String.connection_lost);

			// Set result CANCELED incase the user backs out
			SetResult (Result.Canceled);

			// We retrieve the old device we were connected to so we can try to reconnect to it later
			// (this value will be reinitialized during enabling)
			_device = Network.Instance.CommunicationWay._device;

			// We restart the bluetooth manager and then we try to reconnect
			restartBluetooth();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			// We unsubscribe from the events
			FieldInfo[] test = typeof(Network).GetFields(BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
			foreach(FieldInfo f in test)
			{
				Log.Debug(Tag, "field: " + f.Name);
			}

			FieldInfo f1 = typeof(Network).GetField("StateConnectedEvent", BindingFlags.Instance);
			object obj = f1.GetValue(Network.Instance);
			PropertyInfo pi = Network.Instance.GetType().GetProperty("StateConnectedEvent", BindingFlags.Instance);
			EventHandlerList list = (EventHandlerList)pi.GetValue(Network.Instance, null);
			list.RemoveHandler(obj, list[obj]);

			f1 = typeof(Network).GetField("EVENT_StateNoneEvent", BindingFlags.Instance);
			obj = f1.GetValue(Network.Instance);
			pi = Network.Instance.GetType().GetProperty("StateNoneEvent", BindingFlags.Instance);
			list = (EventHandlerList)pi.GetValue(Network.Instance, null);
			list.RemoveHandler(obj, list[obj]);
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
				OnFail();
			}
		}

		//--------------------------------------------------------------
		// PRIVATES METHODES
		//--------------------------------------------------------------
		private int OnConnected()
		{
			#if DEBUG
			Log.Debug(Tag, "OnConnected");
			#endif

			if(_messageFail != null)
			{
				Network.Instance.CommunicationWay.Write(_messageFail);
			}

			// Set result and finish this Activity
			SetResult(Result.Ok, null);
			Finish();

			return 0;
		}

		private int OnFail()
		{
			#if DEBUG
			Log.Debug(Tag, "Fail");
			#endif

			// Reset result and asking if we retry or finish this Activity
			SetResult(Result.Canceled, null);

			String message = String.Format(Resources.GetString(Resource.String.game_request), Network.Instance.CommunicationWay._device.Name);
			AlertDialog.Builder builder = new AlertDialog.Builder(this);
			builder.SetTitle(Resource.String.retry_connection_title);
			builder.SetMessage(Resource.String.retry_connection);
			builder.SetCancelable(false);
			builder.SetPositiveButton(Android.Resource.String.Yes, delegate{restartBluetooth();});
			builder.SetNegativeButton(Android.Resource.String.No, delegate{Finish();});
			AlertDialog alert = builder.Create();
			alert.Show();

			return 0;
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
			Network.Instance.StateConnectedEvent += OnConnected;
			Network.Instance.StateNoneEvent += OnFail;

			if(Network.Instance.WaitingForConnection() && _device != null)
			{
				// Attempt to connect to the previous device
				Network.Instance.CommunicationWay.Connect(_device);
			}
			else
			{
				OnFail();
			}
		}
	}
}

