using Android.Bluetooth;
using Android.Content;

namespace Tetrim
{
	public class Receiver : BroadcastReceiver
	{ 
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		private BluetoothConnectionActivity _activity;

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public Receiver(BluetoothConnectionActivity activity)
		{
			_activity = activity;
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
					_activity.AddNewDevice(device);
				}
				// When discovery is finished, change the Activity title
			}
			else if(action == BluetoothAdapter.ActionDiscoveryFinished)
			{
				_activity.FinishDiscovery();
			}
		} 
	}
}

