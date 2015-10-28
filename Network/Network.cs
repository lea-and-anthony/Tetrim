using System;

namespace Tetrim
{
	public sealed class Network
	{
		//--------------------------------------------------------------
		// EVENTS
		//--------------------------------------------------------------
		public delegate int WriteEventDelegate(byte[] writeBuf);
		public delegate int ReadEventDelegate(byte[] readBuf);
		public delegate int StateConnectingEventDelegate();
		public delegate int StateConnectedEventDelegate();
		public delegate int StateNoneDelegate();
		public delegate int DeviceNameDelegate(string deviceName);

		public delegate int UsualGameMessageDelegate(byte[] buffer);
		public delegate int PiecePutMessageDelegate(byte[] buffer);
		public delegate int NextPieceMessageDelegate(byte[] buffer);
		public delegate int StartMessageDelegate(byte[] buffer);
		public delegate int PauseMessageDelegate(bool initiator);
		public delegate int ResumeMessageDelegate(bool initiator);


		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		private static readonly Network _instance = new Network();

		public BluetoothManager _communicationWay = null;

		public event WriteEventDelegate WriteEvent;
		public event ReadEventDelegate ReadEvent;
		public event StateConnectingEventDelegate StateConnectingEvent;
		public event StateConnectedEventDelegate StateConnectedEvent;
		public event StateNoneDelegate StateNoneEvent;
		public event DeviceNameDelegate DeviceNameEvent;

		public event UsualGameMessageDelegate UsualGameMessage;
		public event PiecePutMessageDelegate PiecePutMessage;
		public event NextPieceMessageDelegate NextPieceMessage;
		public event StartMessageDelegate StartMessage;
		public event PauseMessageDelegate PauseMessage;
		public event ResumeMessageDelegate ResumeMessage;

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		private Network () {}

		//--------------------------------------------------------------
		// PROPERTIES
		//--------------------------------------------------------------
		public static Network Instance
		{
			get 
			{
				return _instance; 
			}
		}

		public BluetoothManager CommunicationWay
		{
			get 
			{
				return _communicationWay; 
			}
		}

		//--------------------------------------------------------------
		// METHODES
		//--------------------------------------------------------------
		public void EnableBluetooth()
		{
			_communicationWay = new BluetoothManager();
		}

		public void DisableBluetooth()
		{
			_communicationWay = null;
		}

		public bool Enable()
		{
			return _communicationWay != null;
		}

		public bool Connected()
		{
			return Enable() && _communicationWay.GetState() == BluetoothManager.State.Connected;
		}

		public bool WaitingForConnection()
		{
			return Enable() && _communicationWay.GetState() == BluetoothManager.State.None;
		}

		public void InterpretMessage(byte[] message)
		{
			switch(message[0])
			{
			// If it is a message for the game, i.e. it is the position of the piece or the position of
			// the piece we have to add to the grid or the grid of the opponent
			case Constants.IdMessageGrid:
			case Constants.IdMessagePiece:
				if(UsualGameMessage != null)
				{
					UsualGameMessage.Invoke(message);
				}
				break;
			// If it is the message telling that the piece is at the bottom of the grid
			case Constants.IdMessagePiecePut:
				if(PiecePutMessage != null)
				{
					PiecePutMessage.Invoke(message);
				}
				break;
			// If it is the message of the next piece for us
			case Constants.IdMessageNextPiece:
				if(NextPieceMessage != null)
				{
					NextPieceMessage.Invoke(message);
				}
				break;
			// It is a message for the main activity asking to begin the game
			case Constants.IdMessageStart:
				if(StartMessage != null)
				{
					StartMessage.Invoke(message);
				}
				break;
			// It is a pause demand, so we are going to treat it if the game is running
			case Constants.IdMessagePause:
				if(PauseMessage != null)
				{
					PauseMessage.Invoke(false);
				}
				break;
			// It is a resume demand, so we are going to treat it if the game is running
			case Constants.IdMessageResume:
				if(ResumeMessage != null)
				{
					ResumeMessage.Invoke(false);
				}
				break;
			}
		}

		public void NotifyWriteMessage(byte[] writeBuf)
		{
			if(WriteEvent != null)
			{
				WriteEvent.Invoke(writeBuf);
			}
		}

		public void NotifyReadMessage(byte[] readBuf)
		{
			if(ReadEvent != null)
			{
				ReadEvent.Invoke(readBuf);
			}
			InterpretMessage(readBuf);
		}

		public void NotifyStateConnecting()
		{
			if(StateConnectingEvent != null)
			{
				StateConnectingEvent.Invoke();
			}
		}

		public void NotifyStateConnected()
		{
			if(StateConnectedEvent != null)
			{
				StateConnectedEvent.Invoke();
			}
		}

		public void NotifyStateNone()
		{
			if(StateNoneEvent != null)
			{
				StateNoneEvent.Invoke();
			}
		}

		public void NotifyDeviceName(string deviceName)
		{
			if(DeviceNameEvent != null)
			{
				DeviceNameEvent.Invoke(deviceName);
			}
		}
	}
}
