/*
* File inspired by BluetoothChatService from the BluetoothChat project
* 
* License of the project:
* Copyright (C) 2009 The Android Open Source Project
*
* Licensed under the Apache License, Version 2.0 (the "License");
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

using System.IO;
using Android.Bluetooth;
using Android.Widget;
using Android.OS;
using Java.Lang;
using Java.Util;
using System.Runtime.CompilerServices;
using Android.Util;

namespace Tetris
{
	public class BluetoothManager
	{
		// Member fields
		protected BluetoothAdapter bluetoothAdapter;
		protected ConnectThread connectThread;
		private ConnectedThread connectedThread;
		protected Handler _handler;
		private AcceptThread acceptThread;
		protected State _state;

		// Constants that indicate the current connection state
		public enum State
		{
			NONE = 0,			// we're doing nothing
			LISTEN = 1,		// now listening for incoming connections
			CONNECTING = 2,	// now initiating an outgoing connection
			CONNECTED = 3		// now connected to a remote device
		};

		// Message types sent from here Handler
		public enum MessageType
		{
			STATE_CHANGE = 1,
			READ = 2,
			WRITE = 3,
			DEVICE_NAME = 4,
			ALERT = 5
		};

		// Unique UUID for this application
		private static UUID MY_UUID = UUID.FromString("443f071d-32b6-431c-8087-8cdf758a711c");

		//Key name for identification message
		public const string DEVICE_NAME = "device_name";

		// Name for the SDP record when creating server socket
		private const string NAME = "BluetoothTetris";


		// Debugging
		private const string TAG = "Tetris-Bluetooth";


		public BluetoothManager()
		{
			_handler = new MyHandler ();
			bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
			_state = State.NONE;
		}

		/// Return the current connection state.
		[MethodImpl(MethodImplOptions.Synchronized)]
		public State GetState ()
		{
			return _state;
		}

		/// Set the current state of the chat connection.
		[MethodImpl(MethodImplOptions.Synchronized)]
		private void SetState(State state)
		{
			#if DEBUG
				Log.Debug(TAG, "setState() " + _state + " -> " + state);
			#endif

			_state = state;

			// Give the new state to the Handler so the UI Activity can update
			_handler.ObtainMessage((int) MessageType.STATE_CHANGE, (int) state, -1).SendToTarget();
		}

		// Start the service of receiving. Specifically start AcceptThread to begin a
		// session in listening(server) mode. Called by the Activity onResume()
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Start()
		{
			#if DEBUG
				Log.Debug(TAG, "start");
			#endif

			// Cancel any thread attempting to make a connection
			if(connectThread != null)
			{
				connectThread.Cancel();
				connectThread = null;
			}

			// Cancel any thread currently running a connection
			if(connectedThread != null)
			{
				connectedThread.Cancel();
				connectedThread = null;
			}

			// Start the thread to listen on a BluetoothServerSocket
			if(acceptThread == null)
			{
				acceptThread = new AcceptThread(this);
				acceptThread.Start();
			}

			SetState(State.LISTEN);
		}

		/// Start the ConnectThread to initiate a connection to a remote device.
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Connect(BluetoothDevice device)
		{
			#if DEBUG
				Log.Debug(TAG, "connect to: " + device);
			#endif

			// Cancel any thread attempting to make a connection
			if(_state == State.CONNECTING)
			{
				if(connectThread != null)
				{
					connectThread.Cancel();
					connectThread = null;
				}
			}

			// Cancel any thread currently running a connection
			if(connectedThread != null)
			{
				connectedThread.Cancel();
				connectedThread = null;
			}

			// Start the thread to connect with the given device
			connectThread = new ConnectThread(device, this);
			connectThread.Start();

			SetState(State.CONNECTING);
		}

		/// Start the ConnectedThread to begin managing a Bluetooth connection
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Connected(BluetoothSocket socket, BluetoothDevice device)
		{
			#if DEBUG
				Log.Debug(TAG, "connected");
			#endif

			// Cancel the thread that completed the connection
			if(connectThread != null)
			{
				connectThread.Cancel();
				connectThread = null;
			}

			// Cancel any thread currently running a connection
			if(connectedThread != null)
			{
				connectedThread.Cancel();
				connectedThread = null;
			}

			// Cancel the accept thread because we only want to connect to one device
			if(acceptThread != null)
			{
				acceptThread.Cancel();
				acceptThread = null;
			}

			// Start the thread to manage the connection and perform transmissions
			connectedThread = new ConnectedThread(socket, this);
			connectedThread.Start();

			// Send the name of the connected device back to the UI Activity
			var msg = _handler.ObtainMessage((int) MessageType.DEVICE_NAME);
			Bundle bundle = new Bundle();
			bundle.PutString(DEVICE_NAME, device.Name);
			msg.Data = bundle;
			_handler.SendMessage(msg);

			SetState(State.CONNECTED);
		}

		/// Stop all threads.
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Stop()
		{
			#if DEBUG
				Log.Debug(TAG, "stop");
			#endif

			if(connectThread != null)
			{
				connectThread.Cancel();
				connectThread = null;
			}

			if(connectedThread != null)
			{
				connectedThread.Cancel();
				connectedThread = null;
			}

			if(acceptThread != null)
			{
				acceptThread.Cancel();
				acceptThread = null;
			}

			SetState(State.NONE);
		}

		/// Write to the ConnectedThread in an unsynchronized manner
		public void Write (byte[] @out)
		{
			// Create temporary object
			ConnectedThread r;
			// Synchronize a copy of the ConnectedThread
			lock(this)
			{
				if(_state != State.CONNECTED)
					return;
				r = connectedThread;
			}
			// Perform the write unsynchronized
			r.Write(@out);
		}

		/// Indicate that the connection attempt failed and notify the UI Activity.
		private void ConnectionFailed()
		{
			SetState(State.LISTEN);

			// Send a message to display an error alert
			_handler.ObtainMessage((int) MessageType.ALERT, Resource.String.ConnectionImpossibleTitle,
				Resource.String.ConnectionImpossible, null).SendToTarget ();
		}

		/// Indicate that the connection was lost and notify the UI Activity.
		public void ConnectionLost()
		{
			SetState(State.LISTEN);

			// Send a message to display an error alert
			_handler.ObtainMessage((int) MessageType.ALERT, Resource.String.ConnectionLostTitle,
				Resource.String.ConnectionLost, null).SendToTarget ();
		}

		/// This thread runs while attempting to make an outgoing connection
		/// with a device. It runs straight through; the connection either
		/// succeeds or fails.
		protected class ConnectThread : Thread
		{
			private BluetoothSocket mmSocket;
			private BluetoothDevice mmDevice;
			private BluetoothManager _service;

			public ConnectThread(BluetoothDevice device, BluetoothManager service)
			{
				mmDevice = device;
				_service = service;
				BluetoothSocket tmp = null;

				// Get a BluetoothSocket for a connection with the
				// given BluetoothDevice
				try
				{
					tmp = device.CreateRfcommSocketToServiceRecord(MY_UUID);
				}
				catch(Java.IO.IOException e)
				{
					Log.Error(TAG, "create() failed", e);
				}
				mmSocket = tmp;
			}

			public override void Run()
			{
				Log.Info(TAG, "BEGIN mConnectThread");
				Name = "ConnectThread";

				// Always cancel discovery because it will slow down a connection
				_service.bluetoothAdapter.CancelDiscovery();

				// Make a connection to the BluetoothSocket
				try
				{
					// This is a blocking call and will only return on a
					// successful connection or an exception
					mmSocket.Connect();
				}
				catch(Java.IO.IOException)
				{
					_service.ConnectionFailed();
					// Close the socket
					try
					{
						mmSocket.Close();
					}
					catch(Java.IO.IOException e2)
					{
						Log.Error(TAG, "unable to close() socket during connection failure", e2);
					}

					// Start the service over to restart listening mode
					_service.Start();
					return;
				}

				// Reset the ConnectThread because we're done
				lock(this)
				{
					_service.connectThread = null;
				}

				// Start the connected thread
				_service.Connected(mmSocket, mmDevice);
			}

			public void Cancel()
			{
				try
				{
					mmSocket.Close();
				}
				catch(Java.IO.IOException e)
				{
					Log.Error(TAG, "close() of connect socket failed", e);
				}
			}
		}


		/// This thread runs during a connection with a remote device.
		/// It handles all incoming and outgoing transmissions.
		private class ConnectedThread : Thread
		{
			private BluetoothSocket mmSocket;
			private Stream mmInStream;
			private Stream mmOutStream;
			private BluetoothManager _service;

			public ConnectedThread(BluetoothSocket socket, BluetoothManager service)
			{
				Log.Debug(TAG, "create ConnectedThread: ");
				mmSocket = socket;
				_service = service;
				Stream tmpIn = null;
				Stream tmpOut = null;

				// Get the BluetoothSocket input and output streams
				try
				{
					tmpIn = socket.InputStream;
					tmpOut = socket.OutputStream;
				}
				catch(Java.IO.IOException e)
				{
					Log.Error(TAG, "temp sockets not created", e);
				}

				mmInStream = tmpIn;
				mmOutStream = tmpOut;
			}

			public override void Run()
			{
				Log.Info(TAG, "BEGIN mConnectedThread");
				byte[] buffer = new byte[Constants.SizeMaxBluetoothMessage];
				int bytes;

				// Keep listening to the InputStream while connected
				while(true)
				{
					try
					{
						// Read from the InputStream
						bytes = mmInStream.Read(buffer, 0, buffer.Length);

						// Send the obtained bytes to the UI Activity
						_service._handler.ObtainMessage((int) MessageType.READ, bytes, -1, buffer)
							.SendToTarget();
					}
					catch(Java.IO.IOException e)
					{
						Log.Error(TAG, "disconnected", e);
						_service.ConnectionLost();
						break;
					}
				}
			}

			/// Write to the connected OutStream.
			public void Write(byte[] buffer)
			{
				try
				{
					mmOutStream.Write(buffer, 0, buffer.Length);

					// Share the sent message back to the UI Activity
					_service._handler.ObtainMessage((int) MessageType.WRITE, -1, -1, buffer)
						.SendToTarget();
				}
				catch(Java.IO.IOException e)
				{
					Log.Error(TAG, "Exception during write", e);
				}
			}

			public void Cancel()
			{
				try
				{
					mmSocket.Close();
				}
				catch(Java.IO.IOException e)
				{
					Log.Error(TAG, "close() of connect socket failed", e);
				}
			}
		}

		/// This thread runs while listening for incoming connections. It behaves
		/// like a server-side client. It runs until a connection is accepted
		///(or until cancelled).
		private class AcceptThread : Thread
		{
			// The local server socket
			private BluetoothServerSocket mmServerSocket;
			private BluetoothManager _service;

			public AcceptThread(BluetoothManager service)
			{
				_service = service;
				BluetoothServerSocket tmp = null;

				// Create a new listening server socket
				try
				{
					tmp = _service.bluetoothAdapter.ListenUsingRfcommWithServiceRecord(NAME, MY_UUID);
				}
				catch(Java.IO.IOException e)
				{
					Log.Error(TAG, "listen() failed", e);
				}
				mmServerSocket = tmp;
			}

			public override void Run()
			{
				#if DEBUG
					Log.Debug(TAG, "BEGIN mAcceptThread " + this.ToString());
				#endif

				Name = "AcceptThread";
				BluetoothSocket socket = null;

				// Listen to the server socket if we're not connected
				while(_service._state != BluetoothManager.State.CONNECTED)
				{
					try
					{
						// This is a blocking call and will only return on a
						// successful connection or an exception
						socket = mmServerSocket.Accept();
					}
					catch(Java.IO.IOException e)
					{
						Log.Error(TAG, "accept() failed", e);
						break;
					}

					// If a connection was accepted
					if(socket != null)
					{
						lock(this)
						{
							switch(_service._state)
							{
							case BluetoothManager.State.LISTEN:
							case BluetoothManager.State.CONNECTING:
								// Situation normal. Start the connected thread.
								_service.Connected(socket, socket.RemoteDevice);
								break;
							case BluetoothManager.State.NONE:
							case BluetoothManager.State.CONNECTED:
								// Either not ready or already connected. Terminate new socket.
								try
								{
									socket.Close();
								}
								catch(Java.IO.IOException e)
								{
									Log.Error(TAG, "Could not close unwanted socket", e);
								}
								break;
							}
						}
					}
				}

				#if DEBUG
					Log.Info(TAG, "END mAcceptThread");
				#endif
			}

			public void Cancel()
			{
				#if DEBUG
					Log.Debug(TAG, "cancel " + this.ToString());
				#endif

				try
				{
					mmServerSocket.Close();
				}
				catch(Java.IO.IOException e)
				{
					Log.Error(TAG, "close() of server failed", e);
				}
			}
		}




		// The Handler that gets information back from the BluetoothChatService
		private class MyHandler : Handler
		{
			public MyHandler() {}

			public override void HandleMessage (Message msg)
			{
				switch (msg.What)
				{
				case (int) MessageType.STATE_CHANGE:
					switch(msg.Arg1)
					{
					case (int) State.CONNECTED:
						Network.Instance.NotifyStateConnected();
						break;
					case (int) State.CONNECTING:
						Network.Instance.NotifyStateConnecting();
						break;
					case (int) State.LISTEN:
					case (int) State.NONE:
						Network.Instance.NotifyStateNone();
						break;
					}
					break;
				case (int) MessageType.WRITE:
					byte[] writeBuf = (byte[])msg.Obj;
					Network.Instance.NotifyWriteMessage(writeBuf);
					break;
				case (int) MessageType.READ:
					byte[] readBuf = (byte[])msg.Obj;
					Network.Instance.NotifyReadMessage(readBuf);
					break;
				case (int) MessageType.DEVICE_NAME:
					string deviceName = msg.Data.GetString (DEVICE_NAME);
					Network.Instance.NotifyDeviceName(deviceName);
					break;
				case (int) MessageType.ALERT:
					// Display an error message
					Log.Warn(TAG, "MessageType.ALERT");
					break;
				}
			}
		}
	}
}

