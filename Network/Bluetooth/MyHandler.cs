using System;

using Android.OS;
using Android.Util;

namespace Tetrim
{
	// The Handler that gets information back from the BluetoothChatService
	public class MyHandler : Handler
	{
		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public MyHandler() {}

		//--------------------------------------------------------------
		// PUBLIC METHODES
		//--------------------------------------------------------------
		public override void HandleMessage (Message message)
		{
			switch (message.What)
			{
			case (int) BluetoothManager.MessageType.StateChange:
				switch(message.Arg1)
				{
				case (int) BluetoothManager.State.Connected:
					Network.Instance.NotifyStateConnected();
					break;
				case (int) BluetoothManager.State.Connecting:
					Network.Instance.NotifyStateConnecting();
					break;
				case (int) BluetoothManager.State.Listen:
				case (int) BluetoothManager.State.None:
					Network.Instance.NotifyStateNone();
					break;
				}
				break;
			case (int) BluetoothManager.MessageType.Write:
				byte[] writeBuf = (byte[])message.Obj;
				Network.Instance.NotifyWriteMessage(writeBuf);
				break;
			case (int) BluetoothManager.MessageType.Read:
				byte[] readBuf = (byte[])message.Obj;
				Network.Instance.NotifyReadMessage(readBuf);
				break;
			case (int) BluetoothManager.MessageType.DeviceName:
				string deviceName = message.Data.GetString (BluetoothManager.DeviceName);
				Network.Instance.NotifyDeviceName(deviceName);
				break;
			case (int) BluetoothManager.MessageType.Alert:
				// Display an error message
				Log.Warn(BluetoothManager.Tag, "MessageType.ALERT");
				break;
			}
		}
	}
}

