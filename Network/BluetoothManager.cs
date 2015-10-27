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

using System;
using System.IO;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Views;
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
		protected MainActivity _activity;

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
		private const string TAG = "Tetris";
		private const bool Debug = true;


		public BluetoothManager(MainActivity activity)
		{
			_activity = activity;
			_handler = new MyHandler (activity);
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
			if(Debug)
				Log.Debug(TAG, "setState() " + _state + " -> " + state);

			_state = state;

			// Give the new state to the Handler so the UI Activity can update
			_handler.ObtainMessage((int) MessageType.STATE_CHANGE, (int) state, -1).SendToTarget();
		}

		// Start the service of receiving. Specifically start AcceptThread to begin a
		// session in listening(server) mode. Called by the Activity onResume()
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Start()
		{	
			if(Debug)
				Log.Debug(TAG, "start");

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
			if(Debug)
				Log.Debug(TAG, "connect to: " + device);

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
			if(Debug)
				Log.Debug(TAG, "connected");

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

		/// <summary>
		/// Stop all threads.
		/// </summary>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Stop()
		{
			if(Debug)
				Log.Debug(TAG, "stop");

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
				if(Debug)
					Log.Debug(TAG, "BEGIN mAcceptThread " + this.ToString());

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

				if(Debug)
					Log.Info(TAG, "END mAcceptThread");
			}

			public void Cancel()
			{
				if(Debug)
					Log.Debug(TAG, "cancel " + this.ToString());

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
			MainActivity appBluetooth;

			public MyHandler(MainActivity app)
			{
				appBluetooth = app;	
			}

			public override void HandleMessage (Message msg)
			{
				TextView textView1 = appBluetooth.FindViewById<TextView> (Resource.Id.textView1);
				TextView textView2 = appBluetooth.FindViewById<TextView> (Resource.Id.textView2);
				switch (msg.What)
				{
				case (int) MessageType.STATE_CHANGE:
					if (Debug)
						Log.Info (TAG, "STATE_CHANGE: " + msg.Arg1);
					switch(msg.Arg1)
					{
					case (int) State.CONNECTED:
						/*appBluetooth.title.SetText (Resource.String.title_connected_to);
						appBluetooth.title.Append (bluetoothChat.connectedDeviceName);*/
						if(textView1 != null)
							textView1.SetText ("State = connected", TextView.BufferType.Normal);
						break;
					case (int) State.CONNECTING:
						/*appBluetooth.title.SetText (Resource.String.title_connecting);*/
						if(textView1 != null)
							textView1.SetText ("State = connecting", TextView.BufferType.Normal);
						break;
					case (int) State.LISTEN:
					case (int) State.NONE:
						if(textView1 != null)
							textView1.SetText ("State = none", TextView.BufferType.Normal);
						/*appBluetooth.title.SetText (Resource.String.title_not_connected);*/
						break;
					}
					break;
				case (int) MessageType.WRITE:
					// construct a string from the buffer
					/*byte[] writeBuf = (byte[])msg.Obj;
					var writeMessage = new Java.Lang.String (writeBuf);
					appBluetooth.conversationArrayAdapter.Add ("Me: " + writeMessage);*/
					if(textView1 != null)
						textView1.SetText("MessageType = write", TextView.BufferType.Normal);
					break;
				case (int) MessageType.READ:
					byte[] readBuf = (byte[])msg.Obj;
					// construct a string from the valid bytes in the buffer
					var readMessage = new Java.Lang.String (readBuf, 0, msg.Arg1);
					//appBluetooth.conversationArrayAdapter.Add (bluetoothChat.connectedDeviceName + ":  " + readMessage);
					if(textView1 != null)
						textView1.SetText("MessageType = read", TextView.BufferType.Normal);
					if(textView2 != null)
						textView2.SetText(readMessage, TextView.BufferType.Normal);

					// We notify the main activity of this message 
					appBluetooth.InterpretMessage(readBuf);
					break;
				case (int) MessageType.DEVICE_NAME:
					appBluetooth.connectedDeviceName = msg.Data.GetString (DEVICE_NAME);
					if(textView1 != null)
						textView1.SetText("MessageType = read", TextView.BufferType.Normal);
					if(textView2 != null)
						textView2.SetText(appBluetooth.connectedDeviceName, TextView.BufferType.Normal);
					//Toast.MakeText (Application.Context, "Connected to " + bluetoothChat.connectedDeviceName, ToastLength.Short).Show ();
					break;
				case (int) MessageType.ALERT:
					// Display an error message
					appBluetooth.showAlert(msg.Arg1, msg.Arg2);
					break;
				}
			}
		}
	}
}

