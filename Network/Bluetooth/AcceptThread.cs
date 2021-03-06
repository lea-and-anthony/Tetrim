﻿using Java.Lang;

using Android.Bluetooth;
using Android.Util;

namespace Tetrim
{
	/// This thread runs while listening for incoming connections. It behaves
	/// like a server-side client. It runs until a connection is accepted
	///(or until cancelled).
	public class AcceptThread : Thread
	{
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		// The local server socket
		private BluetoothServerSocket _serverSocket;
		private BluetoothManager _service;
		private bool _continue = true;
		private bool _success = false;
		private bool _end = false;
		private object locker = new object ();

		//--------------------------------------------------------------
		// PROPERTIES
		//--------------------------------------------------------------
		public bool CanStart
		{
			get
			{
				return _serverSocket != null;
			}
		}

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public AcceptThread(BluetoothManager service)
		{
			_service = service;
			BluetoothServerSocket temp = null;

			// Create a new listening server socket
			try
			{
				temp = _service.BluetoothAdapter.ListenUsingRfcommWithServiceRecord(Name, BluetoothManager.MyUUID);
			}
			catch(Java.IO.IOException e)
			{
				Log.Error(BluetoothManager.Tag, "Listen() failed", e);
			}
			_serverSocket = temp;
		}

		//--------------------------------------------------------------
		// PUBLIC METHODS
		//--------------------------------------------------------------
		public override void Run()
		{
			#if DEBUG
			Log.Debug(BluetoothManager.Tag, "BEGIN AcceptThread " + this.ToString());
			#endif

			if(!CanStart)
			{
				Log.Error(BluetoothManager.Tag, "ERROR: Could not start the accept thread because _serverSocket = null");
				_end = true;
				return;
			}

			Name = "AcceptThread";
			BluetoothSocket socket = null;

			// Listen to the server socket if we're not connected
			while(_service.State != BluetoothManager.StateEnum.Connected && _continue)
			{
				try
				{
					// This is a blocking call and will only return on a
					// successful connection or an exception
					socket = _serverSocket.Accept();
				}
				catch(Java.IO.IOException e)
				{
					Log.Error(BluetoothManager.Tag, "Accept() failed", e);
					continue;
				}

				// If a connection was accepted
				if(socket != null && _continue)
				{
					lock (locker)
					{
						switch (_service.State)
						{
						case BluetoothManager.StateEnum.Listen:
						case BluetoothManager.StateEnum.Connecting:
							// Situation normal. Start the connected thread.
							_success = true;
							_service.Connected (socket, socket.RemoteDevice, false);
							break;
						case BluetoothManager.StateEnum.None:
						case BluetoothManager.StateEnum.Connected:
							// Either not ready or already connected. Terminate new socket.
							try
							{
								socket.Close ();
							}
							catch (Java.IO.IOException e)
							{
								Log.Error (BluetoothManager.Tag, "Could not close unwanted socket", e);
							}
							break;
						}
					}
				}
			}
			_end = true;

			#if DEBUG
			Log.Info(BluetoothManager.Tag, "END AcceptThread");
			#endif
		}

		public void Cancel()
		{
			#if DEBUG
			Log.Debug(BluetoothManager.Tag, "Cancel " + this.ToString());
			#endif

			_continue = false;
			if(_serverSocket != null)
			{
				try
				{
					_serverSocket.Close();

					if(!_success) // if it is a success we are trying to abort this thread so no need to wait
					{
						// Wait for the end of the thread
						while(!_end)
						{
							Thread.Sleep(100);
						}
					}
				}
				catch(Java.IO.IOException e)
				{
					Log.Error(BluetoothManager.Tag, "Close() of server failed", e);
				}
			}
			_serverSocket = null;
		}
	}
}

