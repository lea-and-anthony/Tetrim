using System;
using System.Timers;

using Android.App;
using Android.Content;
using Android.Widget;
using Android.OS;
using Android.Util;

namespace Tetrim
{
	[Activity(Label = "Tetrim", Icon = "@drawable/icon", Theme = "@android:style/Theme.NoTitleBar.Fullscreen", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
	public class GameMultiActivity : GameActivity
	{
		//--------------------------------------------------------------
		// CONSTANTS
		//--------------------------------------------------------------
		private enum StopOrigin
		{
			None,
			MyPause,
			PauseOpponent,
			LostConnection
		}

		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		public Player _player2 { get; private set; }
		public PlayerView _player2View { get; private set; } // View of the player 2

		private StopOrigin _originPause = StopOrigin.None;
		private bool endMessageSent = false;

		//--------------------------------------------------------------
		// EVENT CATCHING METHODES
		//--------------------------------------------------------------
		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			if(!Network.Instance.Connected())
			{
				// If we are trying to start a multi activity and we are not connected to someone, we stop
				Finish();
			}

			// Set our view from the "main" layout resource
			SetContentView(Resource.Layout.GameMulti);

			initGame();

			GridView gridOpponent = FindViewById<GridView>(Resource.Id.OpponentGridView);
			gridOpponent.Init(_player2._grid);
			_player2View._gridView = gridOpponent;

			TextView player2name = FindViewById<TextView> (Resource.Id.player2name);
			TextView player2score = FindViewById<TextView> (Resource.Id.player2score);
			TextView player2level = FindViewById<TextView> (Resource.Id.player2level);
			TextView player2rows = FindViewById<TextView> (Resource.Id.player2rows);
			_player2View.SetViews(player2name, player2score, player2level, player2rows);

			ViewProposedPiece viewProposed = FindViewById<ViewProposedPiece>(Resource.Id.ProposedPiecesView);
			viewProposed.SetPlayer(_player1);

			// Hook on network event
			Network.Instance.EraseAllEvent();
			Network.Instance.UsualGameMessage += UpdateOpponentView;
			Network.Instance.PiecePutMessage += OpponentPiecePut;
			Network.Instance.NextPieceMessage += _player1.interpretMessage;
			Network.Instance.PauseMessage += pauseGame;
			Network.Instance.ResumeMessage += resumeGame;
			Network.Instance.ScoreMessage += OnReceiveScoreMessage;
			Network.Instance.EndMessage += OnReceiveEndMessage;
			Network.Instance.ConnectionLostEvent += OnLostConnection;

			startGame();
		}

		protected override void OnDestroy ()
		{
			// If we are still playing, we send the end message to the opponent
			if (Network.Instance.Connected() && !endMessageSent)
			{
				Network.Instance.CommunicationWay.Write(_player1.GetEndMessage());
			}

			base.OnDestroy();
		}

		protected override void OnActivityResult (int requestCode, Result resultCode, Intent data)
		{
			#if DEBUG
			Log.Debug(Tag, "onActivityResult, resultCode=" + resultCode);
			#endif
			if(requestCode == (int) Utils.RequestCode.RequestReconnect)
			{
				if(resultCode == Result.Ok)
				{
					// We can restart the game if the connection is working again
					_gameTimer.Start();
					_originPause = StopOrigin.None;
				}
				else
				{
					// We didn't reconnect to the other device so we end the party
					Finish();
				}
			}
		}

		protected override void OnTimerElapsed(object source, ElapsedEventArgs e)
		{
			bool newNextPiece = _player1._grid._isNextPieceModified;
			byte[] messageBuffer = new byte[Constants.SizeMessagePiece - 1];
			messageBuffer = _player1._grid._fallingPiece.getMessage(messageBuffer, 0);

			bool isSamePiece = _player1._grid.MovePieceDown(_player1);
			if(!isSamePiece)
			{
				_player1View.Update();
			}

			RunOnUiThread(_player1View.Draw);

			if (_player1._grid.isGameOver())
			{
				_gameTimer.Stop();
				//Utils.PopUpEndEvent += endGame;
				Intent intent = Utils.CreateGameOverDialogMulti(this, Resources, false);
				StartActivity(intent);
			}

			//  Network
			actualizeViewOtherPlayer(isSamePiece, messageBuffer, newNextPiece);

			// Display of the current model
			FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
		}

		private int OnReceiveScoreMessage(byte[] message)
		{
			#if DEBUG
			Log.Debug(Tag, "OnReceiveScoreMessage");
			#endif

			_player2.InterpretScoreMessage(message);
			return 0;
		}

		private int OnReceiveEndMessage(byte[] message)
		{
			_gameTimer.Stop();
			//Utils.PopUpEndEvent += endGame;
			//RunOnUiThread(() => Utils.ShowAlert (Resource.String.game_over_win_title, Resource.String.game_over_win, this));
			Intent intent = Utils.CreateGameOverDialogMulti(this, Resources, true);
			StartActivity(intent);
			return 0;
		}

		private int OnLostConnection(byte[] message)
		{
			#if DEBUG
			Log.Debug(Tag, "OnLostConnection");
			#endif

			// If we lost the connection, we stop the game display a pop-up and try to reconnect
			_gameTimer.Stop();
			_originPause = StopOrigin.LostConnection;

			ReconnectActivity._messageFail = message;
			var serverIntent = new Intent(this, typeof(ReconnectActivity));
			StartActivityForResult(serverIntent,(int) Utils.RequestCode.RequestReconnect);

			return 0;
		}

		//--------------------------------------------------------------
		// PROTECTED METHODES
		//--------------------------------------------------------------
		// Pause the game, display a pop-up and send a message to the remote device if asked
		protected override int pauseGame(bool requestFromUser)
		{
			_gameTimer.Stop();

			// Set the origin so it is the right player who restart the game
			_originPause = requestFromUser ? StopOrigin.MyPause : StopOrigin.PauseOpponent;

			// We need to pause the other game if it is not it which stop us
			if(requestFromUser)
			{
				byte[] message = new byte[Constants.SizeMessagePause];
				message[0] = Constants.IdMessagePause;
				Network.Instance.CommunicationWay.Write(message);
			}

			Utils.PopUpEndEvent += resumeGame;
			// TODO : change dialog
			Utils.ShowAlert(Resource.String.Pause_title, Resource.String.Pause, this);

			return 0;
		}

		// Resume the game if it is us who asked for the pause
		protected override void resumeGame()
		{
			if(_originPause == StopOrigin.MyPause)
			{
				resumeGame(true);
			}
		}

		protected override void moveLeftButtonPressed(object sender, EventArgs e)
		{
			_player1.MoveLeft();
			// Display of the current model
			FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
			actualizeViewOtherPlayer(true, null, false);
		}

		protected override void moveRightButtonPressed(object sender, EventArgs e)
		{
			_player1.MoveRight();
			// Display of the current model
			FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
			actualizeViewOtherPlayer(true, null, false);
		}

		protected override void turnLeftButtonPressed(object sender, EventArgs e)
		{
			_player1.TurnLeft();
			// Display of the current model
			FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
			actualizeViewOtherPlayer(true, null, false);
		}

		protected override void turnRightButtonPressed(object sender, EventArgs e)
		{
			_player1.TurnRight();
			// Display of the current model
			FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
			actualizeViewOtherPlayer(true, null, false);
		}

		protected override void moveDownButtonPressed(object sender, EventArgs e)
		{
			if(_player1.MoveDown())
			{
				_gameTimer.Stop();

				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
				_player1View.Draw();
				actualizeViewOtherPlayer(true, null, false);

				_gameTimer.Start();
			}
		}

		protected override void moveFootButtonPressed(object sender, EventArgs e)
		{
			if(_player1.MoveBottom())
			{
				_gameTimer.Stop();

				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
				_player1View.Draw();
				actualizeViewOtherPlayer(true, null, false);

				_gameTimer.Start();
			}
		}

		// Resume the game by restarting the timer and send the restart message to the opponent
		private int resumeGame(bool sendRequestToOverPlayer)
		{
			// If it is a 2 player game we need to resume the other game if it is not it which resume us
			if(sendRequestToOverPlayer)
			{
				byte[] message = new byte[Constants.SizeMessageResume];
				message[0] = Constants.IdMessageResume;
				Network.Instance.CommunicationWay.Write(message);
			}

			_gameTimer.AutoReset = true;
			_gameTimer.Interval = getTimerLapse();
			_gameTimer.Start();

			_originPause = StopOrigin.None;

			return 0;
		}

		private int UpdateOpponentView(byte[] message)
		{
			// Interpret the message
			_player2.interpretMessage(message);

			// Update of the opponent grid (the display will be done with the other grid)
			_player2View.Update();

			// Display of the model of the opponent
			FindViewById(Resource.Id.OpponentGridView).PostInvalidate();
			_player2View.Draw();

			return 0;
		}

		private int OpponentPiecePut(byte[] message)
		{
			UpdateOpponentView(message);
			if(message[Constants.SizeMessagePiecePut - 1] == 1)
			{
				FindViewById<ViewProposedPiece>(Resource.Id.ProposedPiecesView).ChangeProposedPiece();
			}

			return 0;
		}


		// isSamePiece must be set to false if it is a new piece that is falling
		// in this case messageBuffer must contains the message of the old piece before it was added to the map and
		// newNextPiece must be set to true if we used a piece sent by our opponent, false otherwise
		// if it is still the samePiece that is falling, isSamePiece must be set to true and messageBuffer and newNextPiece are ignored
		private void actualizeViewOtherPlayer(bool isSamePiece, byte[] messageBuffer, bool newNextPiece)
		{
			// We send the message to the other player
			if (_player1._grid.isGameOver())
			{
				Network.Instance.CommunicationWay.Write(_player1.GetEndMessage());
				endMessageSent = true;
			}

			if(!isSamePiece) // the piece was added on the map so the score changed
			{
				Network.Instance.CommunicationWay.Write(_player1.GetScoreMessage());
			}

			// If it is the same piece we only send the position of the piece
			if(isSamePiece)
				Network.Instance.CommunicationWay.Write(_player1.getMessagePiece());
			// If it is a new piece, we send the old piece and the new one
			else
			{
				byte[] message = new byte[Constants.SizeMessagePiecePut];
				message[0] = Constants.IdMessagePiecePut;
				for(int i = 0; i < Constants.SizeMessagePiece - 1; i++)
				{
					message[i+1] = messageBuffer[i];
				}
				message = _player1._grid.getMessagePiece(message, Constants.SizeMessagePiece);

				// We say if we used the piece sent by the opponent or not (if he didn't send one)
				if(newNextPiece)
					message[Constants.SizeMessagePiecePut-1] = 1;
				else
					message[Constants.SizeMessagePiecePut-1] = 0;

				Network.Instance.CommunicationWay.Write(message);
			}
		}
	}
}
