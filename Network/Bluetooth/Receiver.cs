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
	public class Receiver : BroadcastReceiver
	{ 
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		private BluetoothConnectionActivity _activity;
		private ArrayAdapter<string> _newDevicesArrayAdapter;

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public Receiver(BluetoothConnectionActivity activity, ref ArrayAdapter<string> newDevicesArrayAdapter)
		{
			_activity = activity;
			_newDevicesArrayAdapter = newDevicesArrayAdapter;
		}

		//--------------------------------------------------------------
		// METHODES OVERRIDE
		//--------------------------------------------------------------
		public override void OnReceive(Context context, Intent intent)
		{ 
			string action = intent.Action;

			// When discovery finds a device
			if(action == BluetoothDevice.ActionFound)
			{
				// Get the BluetoothDevice object from the Intent
				BluetoothDevice device =(BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
				// If it's already paired, skip it, because it's been listed already
				if(device.BondState != Bond.Bonded)
				{
					_newDevicesArrayAdapter.Add(device.Name + "\n" + device.Address);
				}
				// When discovery is finished, change the Activity title
			}
			else if(action == BluetoothAdapter.ActionDiscoveryFinished)
			{
				if(_newDevicesArrayAdapter.Count == 0 && 
					_activity.FindViewById<View>(Resource.Id.title_new_devices).Visibility == ViewStates.Visible)
				{
					var noDevices = _activity.Resources.GetText(Resource.String.none_found).ToString();
					_newDevicesArrayAdapter.Add(noDevices);
				}
				_activity.FinishDiscovery();
			}
		} 
	}
}

