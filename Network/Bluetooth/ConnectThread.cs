using System;

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
			Log.Info(BluetoothManager.Tag, "BEGIN ConnectThread");
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
			catch(Java.IO.IOException)
			{
				_service.ConnectionFailed();

				// Close the socket
				try
				{
					_socket.Close();
				}
				catch(Java.IO.IOException e2)
				{
					Log.Error(BluetoothManager.Tag, "Unable to Close() socket during connection failure", e2);
				}

				// Start the service over to restart listening mode
				_service.Start();
				return;
			}

			// Reset the ConnectThread because we're done
			lock (locker)
			{
				_service.ResetConnectThread();
			}

			// Start the connected thread
			_service.Connected(_socket, _device);
		}

		public void Cancel()
		{
			try
			{
				_socket.Close();
			}
			catch(Java.IO.IOException e)
			{
				Log.Error(BluetoothManager.Tag, "Close() of connect socket failed", e);
			}
		}
	}
}

