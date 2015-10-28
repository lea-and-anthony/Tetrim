﻿using System;

using Android.Bluetooth;
using Java.Lang;
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
		private object locker = new object ();

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
		// PUBLIC METHODES
		//--------------------------------------------------------------
		public override void Run()
		{
			#if DEBUG
			Log.Debug(BluetoothManager.Tag, "BEGIN AcceptThread " + this.ToString());
			#endif

			Name = "AcceptThread";
			BluetoothSocket socket = null;

			// Listen to the server socket if we're not connected
			while(_service.GetState() != BluetoothManager.State.Connected)
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
					break;
				}

				// If a connection was accepted
				if(socket != null)
				{
					lock (locker)
					{
						switch (_service.GetState())
						{
						case BluetoothManager.State.Listen:
						case BluetoothManager.State.Connecting:
							// Situation normal. Start the connected thread.
							_service.Connected (socket, socket.RemoteDevice);
							break;
						case BluetoothManager.State.None:
						case BluetoothManager.State.Connected:
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

			#if DEBUG
			Log.Info(BluetoothManager.Tag, "END AcceptThread");
			#endif
		}

		public void Cancel()
		{
			#if DEBUG
			Log.Debug(BluetoothManager.Tag, "Cancel " + this.ToString());
			#endif

			try
			{
				_serverSocket.Close();
			}
			catch(Java.IO.IOException e)
			{
				Log.Error(BluetoothManager.Tag, "Close() of server failed", e);
			}
		}
	}
}
