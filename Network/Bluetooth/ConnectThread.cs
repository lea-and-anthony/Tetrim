using Java.Lang;

using Android.Bluetooth;
using Android.Util;

namespace Tetrim
{
	/// This thread runs while attempting to make an outgoing connection
	/// with a device. It runs straight through; the connection either
	/// succeeds or fails.
	public class ConnectThread : Thread
	{
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		private BluetoothSocket _socket;
		private BluetoothDevice _device;
		private BluetoothManager _service;
		private bool _continue = true;
		private bool _success = false;
		private bool _end = false;
		private object locker = new object ();

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public ConnectThread(BluetoothDevice device, BluetoothManager service)
		{
			_device = device;
			_service = service;
			BluetoothSocket tmp = null;

			// Get a BluetoothSocket for a connection with the
			// given BluetoothDevice
			try
			{
				tmp = device.CreateRfcommSocketToServiceRecord(BluetoothManager.MyUUID);
			}
			catch(Java.IO.IOException e)
			{
				Log.Error(BluetoothManager.Tag, "create() failed", e);
			}
			_socket = tmp;
		}

		//--------------------------------------------------------------
		// PUBLIC METHODES
		//--------------------------------------------------------------
		public override void Run()
		{
			#if DEBUG
			Log.Debug(BluetoothManager.Tag, "BEGIN ConnectThread");
			#endif

			if(_socket != null)
			{
				Name = "ConnectThread";

				// Always cancel discovery because it will slow down a connection
				_service.BluetoothAdapter.CancelDiscovery();

				// Make a connection to the BluetoothSocket
				try
				{
					// This is a blocking call and will only return on a
					// successful connection or an exception
					_socket.Connect();
				}
				catch(Java.IO.IOException e)
				{
					if(_continue)
					{
						_service.ConnectionFailed();

						Log.Error(BluetoothManager.Tag, "Unable to connect. Message = " + e.Message);
						// Close the socket
						try
						{
							if(_socket != null)
							{
								_socket.Close();
								_socket = null;
							}
						}
						catch(Java.IO.IOException e2)
						{
							Log.Error(BluetoothManager.Tag, "Unable to Close() socket during connection failure", e2);
						}
					}
					_end = true;
					return;
				}

				if(_continue)
				{
					// Start the connected thread
					_success = true;
					_service.Connected(_socket, _device, true);
				}
			}
			else
			{
				Log.Error(BluetoothManager.Tag, "ERROR: Could not start the Connect thread because _socket = null");
			}

			_end = true;

			#if DEBUG
			Log.Debug(BluetoothManager.Tag, "END ConnectThread");
			#endif
		}

		public void Cancel()
		{
			_continue = false;
			if(_socket != null && !_success) // if it is a success we are trying to abort this thread so no need to wait
			{
				try
				{
					_socket.Close();
					_socket = null;
				}
				catch(Java.IO.IOException e)
				{
					Log.Error(BluetoothManager.Tag, "Close() of connect socket failed", e);
				}

				// Wait for the end of the thread
				while(!_end)
				{
					Thread.Sleep(10);
				}
			}
		}
	}
}

