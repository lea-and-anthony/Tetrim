using System;

using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Util;

namespace Tetrim
{
	public sealed class Network
	{
		//--------------------------------------------------------------
		// CONSTANTS
		//--------------------------------------------------------------
		// Debugging
		private const string Tag = "Network";

		// Constants that indicate the current connection state
		public enum ResultEnabling
		{
			Activation = 0,		// Start an activity to enable the bluetooth
			NoMedium = 1,		// There is no bluetooth on the device
			Enabled = 2			// Bluetooth activated
		};

		public enum StartState
		{
			NONE,
			WAITING_FOR_OPPONENT,
			OPPONENT_READY
		};

		//--------------------------------------------------------------
		// EVENTS
		//--------------------------------------------------------------
		public delegate int BufferDelegate(byte[] buffer);
		public delegate int StandardDelegate();
		public delegate int DeviceNameDelegate(string deviceName);
		public delegate int GameMessageDelegate(byte[] buffer);
		public delegate int SimpleMessageDelegate(bool initiator);


		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		private static readonly Network _instance = new Network();

		public BluetoothManager _communicationWay = null;

		public event BufferDelegate WriteEvent;
		public event BufferDelegate ReadEvent;
		public event StandardDelegate StateConnectingEvent;
		public event StandardDelegate StateConnectedEvent;
		public event StandardDelegate StateNoneEvent;
		public event StandardDelegate StateListenEvent;
		public event BufferDelegate ConnectionLostEvent;

		public event GameMessageDelegate UsualGameMessage;
		public event GameMessageDelegate PiecePutMessage;
		public event GameMessageDelegate NextPieceMessage;
		public event GameMessageDelegate StartMessage;
		public event StandardDelegate RestartMessage;
		public event GameMessageDelegate EndMessage;
		public event GameMessageDelegate ScoreMessage;
		public event SimpleMessageDelegate PauseMessage;
		public event SimpleMessageDelegate ResumeMessage;
		public event DeviceNameDelegate DeviceNameEvent;
		public event StandardDelegate NewGameMessage;

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

		/* Return true if the bluetooth is activated (but not necessarily connected) */
		public bool Enabled
		{
			get 
			{
				return _communicationWay != null && _communicationWay.Enabled;
			}
		}

		/* Return true if the bluetooth is activated and connected to an other device */
		public bool Connected
		{
			get 
			{
				return Enabled && _communicationWay.State == BluetoothManager.StateEnum.Connected;
			}
		}

		/* Return true if the bluetooth is activated but no service is started */
		public bool WaitingForStart
		{
			get 
			{
				return Enabled && _communicationWay.State == BluetoothManager.StateEnum.None;
			}
		}

		/* Return true if the bluetooth is activated and waiting for an other device to start a connection */
		public bool WaitingForConnection
		{
			get 
			{
				return Enabled && _communicationWay.State == BluetoothManager.StateEnum.Listen;
			}
		}

		//--------------------------------------------------------------
		// METHODES
		//--------------------------------------------------------------
		/* Activate the bluetooth to allow connection with an other device*/
		public ResultEnabling TryEnablingBluetooth(Activity activity)
		{
			#if DEBUG
			Log.Debug(Tag, "TryEnablingBluetooth()");
			#endif

			// Get local Bluetooth adapter
			BluetoothAdapter bluetoothAdapter = BluetoothAdapter.DefaultAdapter;

			// If the adapter is null, then Bluetooth is not supported
			if(bluetoothAdapter == null)
			{
				#if DEBUG
				Log.Debug(Tag, "display of the alert");
				#endif

				UtilsDialog.PopUpEndEvent += activity.Finish;
				Intent intent = UtilsDialog.CreateBluetoothDialogNoCancel(activity, Resource.String.BTNotAvailable);
				activity.StartActivity(intent);
				return ResultEnabling.NoMedium;
			}

			// If the bluetooth is not enable, we try to activate it
			if(!bluetoothAdapter.IsEnabled)
			{
				#if DEBUG
				Log.Debug(Tag, "intent to activate bluetooth");
				#endif

				Intent enableIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
				activity.StartActivityForResult(enableIntent,(int) Utils.RequestCode.RequestEnableBluetooth);
				return ResultEnabling.Activation;
			}

			#if DEBUG
			Log.Debug(Tag, "creation of BluetoothManager");
			#endif

			EnableBluetooth();
			_communicationWay.Start();
			return ResultEnabling.Enabled;
		}

		public bool ResultBluetoothActivation(int requestCode, Result resultCode, Activity activity)
		{
			if(requestCode == (int) Utils.RequestCode.RequestEnableBluetooth)
			{
				// When the request to enable Bluetooth returns
				if(resultCode == Result.Ok)
				{
					// Bluetooth is now enabled
					EnableBluetooth();
					CommunicationWay.Start();
					return Enabled;
				}
				else
				{
					// User did not enable Bluetooth or an error occured
					#if DEBUG
					Log.Debug(Tag, "Bluetooth not enabled");
					#endif
					UtilsDialog.PopUpEndEvent += activity.Finish;
					Intent intent = UtilsDialog.CreateBluetoothDialogNoCancel(activity, Resource.String.BTNotEnabled);
					activity.StartActivity(intent);
				}
			}
			return false;
		}

		/* Activate the bluetooth to allow connection with an other device*/
		public void EnableBluetooth()
		{
			_communicationWay = new BluetoothManager();
		}

		/* Stop all buetooth activities */
		public void DisableBluetooth()
		{
			if(_communicationWay != null)
			{
				_communicationWay.Stop();
			}
			_communicationWay = null;
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
			// It is a message for the main activity telling that the opponent lost
			case Constants.IdMessageEnd:
				if(EndMessage != null)
				{
					EndMessage.Invoke(message);
				}
				break;
			// It is a message for the main activity telling the opponent score
			case Constants.IdMessageScore:
				if(ScoreMessage != null)
				{
					ScoreMessage.Invoke(message);
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
			case Constants.IdMessageRestart:
				if(RestartMessage != null)
				{
					RestartMessage.Invoke();
				}
				break;
			case Constants.IdMessageName:
				NotifyDeviceName(message);
				break;
			case Constants.IdMessageNewGame:
				if(NewGameMessage != null)
				{
					NewGameMessage.Invoke();
				}
				break;
			}
		}

		public void EraseAllEvent()
		{
			WriteEvent = null;
			ReadEvent = null;
			StateConnectingEvent = null;
			StateConnectedEvent = null;
			StateNoneEvent = null;
			StateListenEvent = null;
			DeviceNameEvent = null;
			ConnectionLostEvent = null;
			UsualGameMessage = null;
			PiecePutMessage = null;
			NextPieceMessage = null;
			StartMessage = null;
			EndMessage = null;
			ScoreMessage = null;
			PauseMessage = null;
			ResumeMessage = null;
			RestartMessage = null;
			NewGameMessage = null;
		}

		public void Stop()
		{
			EraseAllEvent();
			_communicationWay.Stop();
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

		public void NotifyStateListen()
		{
			if(StateListenEvent != null)
			{
				StateListenEvent.Invoke();
			}
		}

		public void NotifyStateNone()
		{
			if(StateNoneEvent != null)
			{
				StateNoneEvent.Invoke();
			}
		}

		public void NotifyDeviceName(byte[] message)
		{
			if(DeviceNameEvent != null)
			{
				char[] longName = new char[Constants.MaxLengthName];
				int length = 0;
				for(int i = 0; i < Constants.MaxLengthName && length == 0; i ++)
				{
					longName[i] = BitConverter.ToChar(message, 1 + i*sizeof(char));
					if(longName[i] == '\0')
					{
						length = i;
					}
				}
				char[] name = new char[length];
				Buffer.BlockCopy(longName, 0, name, 0, length*sizeof(char));
				DeviceNameEvent.Invoke(new String(name));
			}
		}

		public void NotifyConnectionLost(byte[] buffer)
		{
			if(ConnectionLostEvent != null)
			{
				ConnectionLostEvent.Invoke(buffer);
			}
		}
	}
}
