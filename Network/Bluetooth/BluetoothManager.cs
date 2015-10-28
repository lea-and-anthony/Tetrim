﻿/*
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

using Android.Bluetooth;
using Android.OS;
using Java.Util;
using System.Runtime.CompilerServices;
using Android.Util;

namespace Tetrim
{
	public class BluetoothManager
	{
		//--------------------------------------------------------------
		// CONSTANTS
		//--------------------------------------------------------------
		// Constants that indicate the current connection state
		public enum State
		{
			None = 0,			// we're doing nothing
			Listen = 1,		// now listening for incoming connections
			Connecting = 2,	// now initiating an outgoing connection
			Connected = 3		// now connected to a remote device
		};

		// Message types sent from here Handler
		public enum MessageType
		{
			StateChange = 1,
			Read = 2,
			Write = 3,
			DeviceName = 4,
			Alert = 5
    		};

		// Key name for identification message
		public const string DeviceName = "device_name";

		// Name for the SDP record when creating server socket
		private const string Name = "BluetoothTetrim";

		// Unique UUID for this application
		public static UUID MyUUID = UUID.FromString("443f071d-32b6-431c-8087-8cdf758a711c");

		// Debugging
		public const string Tag = "Tetrim-Bluetooth";

		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		private BluetoothAdapter _bluetoothAdapter;
		private ConnectThread _connectThread;
		private ConnectedThread _connectedThread;
		private Handler _handler;
		private AcceptThread _acceptThread;
		private State _state;
		private readonly object _locker = new object ();

		//--------------------------------------------------------------
		// PROPERTIES
		//--------------------------------------------------------------
		public BluetoothAdapter BluetoothAdapter
		{
			get
			{
				return _bluetoothAdapter;
			}
		}

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public BluetoothManager()
		{
			_handler = new MyHandler ();
			_bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
			_state = State.None;
		}

		//--------------------------------------------------------------
		// THREAD METHODES
		//--------------------------------------------------------------

		// Start the service of receiving. Specifically start AcceptThread to begin a
		// session in listening(server) mode. Called by the Activity onResume()
		public void Start()
		{
			lock (_locker)
			{
				#if DEBUG
				Log.Debug (Tag, "Start");
				#endif

				// Cancel all the threads
				stopThreads();

				// Start the thread to listen on a BluetoothServerSocket
				if (_acceptThread == null)
				{
					_acceptThread = new AcceptThread (this);
					_acceptThread.Start ();
				}

				setState (State.Listen);
			}
		}

		/// Start the ConnectThread to initiate a connection to a remote device.
		public void Connect(BluetoothDevice device)
		{
			lock (_locker)
			{
				#if DEBUG
				Log.Debug (Tag, "Connecting to : " + device);
				#endif

				// Cancel any thread attempting to make a connection
				if (_state == State.Connecting)
				{
					if (_connectThread != null) {
						_connectThread.Cancel ();
						_connectThread = null;
					}
				}

				// Cancel any thread currently running a connection
				if (_connectedThread != null)
				{
					_connectedThread.Cancel ();
					_connectedThread = null;
				}

				// Start the thread to connect with the given device
				_connectThread = new ConnectThread (device, this);
				_connectThread.Start ();

				setState (State.Connecting);
			}
		}

		/// Start the ConnectedThread to begin managing a Bluetooth connection
		public void Connected(BluetoothSocket socket, BluetoothDevice device)
		{
			lock (_locker)
			{
				#if DEBUG
				Log.Debug (Tag, "Connected");
				#endif

				// Cancel all the threads
				stopThreads();

				// Start the thread to manage the connection and perform transmissions
				_connectedThread = new ConnectedThread (socket, this);
				_connectedThread.Start ();

				// Send the name of the connected device back to the UI Activity
				Message message = _handler.ObtainMessage ((int)MessageType.DeviceName);
				Bundle bundle = new Bundle ();
				bundle.PutString (DeviceName, device.Name);
				message.Data = bundle;
				_handler.SendMessage (message);

				setState (State.Connected);
			}
		}

		/// Stop all threads.
		public void Stop()
		{
			lock (_locker)
			{
				#if DEBUG
				Log.Debug (Tag, "Stop");
				#endif

				// Cancel all the threads
				stopThreads();

				setState (State.None);
			}
		}

		//--------------------------------------------------------------
		// PUBLIC METHODES
		//--------------------------------------------------------------
		/// Return the current connection state.
		public State GetState ()
		{
			lock (_locker)
			{
				return _state;
			}
   		}

		/// Write to the ConnectedThread in an unsynchronized manner
		public void Write (byte[] @out)
		{
			// Create temporary object
			ConnectedThread connectedThread;

			// Synchronize a copy of the ConnectedThread
			lock (_locker)
			{
				if (_state != State.Connected)
					return;
				connectedThread = _connectedThread;
			}

			if (connectedThread != null)
			{
				// Perform the write unsynchronized
				connectedThread.Write(@out);
			}
		}

		/// Indicate that the connection was lost and notify the UI Activity.
		public void ConnectionLost()
		{
			setState(State.Listen);

			// Send a message to display an error alert
			_handler.ObtainMessage((int) MessageType.Alert, Resource.String.ConnectionLostTitle,
				Resource.String.ConnectionLost, null).SendToTarget ();
		}

		/// Indicate that the connection attempt failed and notify the UI Activity.
		public void ConnectionFailed()
		{
			setState(State.Listen);

			// Send a message to display an error alert
			_handler.ObtainMessage((int) MessageType.Alert, Resource.String.ConnectionImpossibleTitle,
				Resource.String.ConnectionImpossible, null).SendToTarget ();
		}

		// Retrieve the message from the handler
		public Message ObtainMessage(int what, int arg1, int arg2, Java.Lang.Object obj)
		{
			return _handler.ObtainMessage(what, arg1, arg2, obj);
		}

		// Set the ConnectThread to null
		public void ResetConnectThread()
		{
			_connectThread = null;
		}

		//--------------------------------------------------------------
		// PRIVATE METHODES
		//--------------------------------------------------------------
		/// Set the current state of the chat connection.
		private void setState(State state)
		{
			lock (_locker)
			{
				#if DEBUG
				Log.Debug (Tag, "SetState() " + _state + " -> " + state);
				#endif

				_state = state;

				// Give the new state to the Handler so the UI Activity can update
				_handler.ObtainMessage ((int)MessageType.StateChange, (int)state, -1).SendToTarget ();
			}   
		}

		private void stopThreads()
		{
			// Cancel the thread that completed the connection
			if (_connectThread != null)
			{
				_connectThread.Cancel ();
				_connectThread = null;
			}

			// Cancel any thread currently running a connection
			if (_connectedThread != null)
			{
				_connectedThread.Cancel ();
				_connectedThread = null;
			}

			// Cancel the accept thread because we only want to connect to one device
			if (_acceptThread != null)
			{
				_acceptThread.Cancel ();
				_acceptThread = null;
			}
		}
	}
}
