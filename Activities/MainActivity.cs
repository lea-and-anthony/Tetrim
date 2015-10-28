﻿using System;
using System.Timers;

using Android.App;
using Android.Content;
using Android.Widget;
using Android.OS;
using Android.Util;
using Android.Bluetooth;

namespace Tetrim
{
	[Activity(Label = "Tetrim", Icon = "@drawable/icon", Theme = "@android:style/Theme.NoTitleBar.Fullscreen")]
	public class MainActivity : Activity
	{
		//--------------------------------------------------------------
		// CONSTANTS
		//--------------------------------------------------------------
		private const string Tag = "Tetrim-MainActivity";

		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		private Timer _gameTimer = null;
		private bool _originPause = false; // Set to true if it's us who ask for the pause

		public Game _game { get; private set; }
		public GameView _gameView { get; private set; }

		//--------------------------------------------------------------
		// EVENT CATCHING METHODES
		//--------------------------------------------------------------
		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			// Set our view from the "main" layout resource
			SetContentView(Resource.Layout.Main);

			// Creation of the model
			_game = new Game();

			// Creation of the view
			_gameView = new GameView(_game);
			MyView view = FindViewById<MyView>(Resource.Id.PlayerGridView);
			view.m_gridView = _gameView.m_player1View._gridView;

			// If it is a 2 player game
			if(Network.Instance.Connected())
			{
				MyView view2 = FindViewById<MyView>(Resource.Id.OpponentGridView);
				view2.m_gridView = _gameView.m_player2View._gridView;

				ViewProposedPiece viewProposed = FindViewById<ViewProposedPiece>(Resource.Id.ProposedPiecesView);
				viewProposed.SetPlayer(_game._player1);
				viewProposed.SetBluetooth(Network.Instance.CommunicationWay);
			}

			// Associate the buttons with the methods
			associateButtonsEvent();

			// Launch the main timer of the application
			int time = getTimerLapse();
			_gameTimer = new Timer(time);
			_gameTimer.Elapsed += new ElapsedEventHandler(OnTimerElapsed);
			_gameTimer.Interval = time;
			_gameTimer.AutoReset = true;
			_gameTimer.Start();

			// Hook on network event
			Network.Instance.UsualGameMessage += UpdateOpponentView;
			Network.Instance.PiecePutMessage += OpponentPiecePut;
			Network.Instance.NextPieceMessage += _game._player1.interpretMessage;
			Network.Instance.PauseMessage += pauseGame;
			Network.Instance.ResumeMessage += resumeGame;
		}

		protected override void OnPause()
		{
			base.OnPause();

			if(_gameTimer != null)
			{
				if(_gameTimer.Enabled)
					pauseGame(true);
				else
					resumeGame();
			}
		}

		protected override void OnDestroy ()
		{
			base.OnDestroy ();

			// Stop the timer
			if (_gameTimer != null)
			{
				_gameTimer.Stop();
				_gameTimer = null;
    		}

			#if DEBUG
			Log.Error (Tag, "--- ON DESTROY ---");
			#endif
		}

		private void OnTimerElapsed(object source, ElapsedEventArgs e)
		{
			bool newNextPiece = _game._player1._grid._isNextPieceModified;
			byte[] messageBuffer = new byte[Constants.SizeMessagePiece - 1];
			messageBuffer = _game._player1._grid._fallingPiece.getMessage(messageBuffer, 0);

			bool isSamePiece = _game._player1._grid.MovePieceDown(_game._player1);
			if(!isSamePiece)
			{
				_gameView.m_player1View.Update();
			}

			TextView player1name = FindViewById<TextView> (Resource.Id.player1name);
			TextView player1score = FindViewById<TextView> (Resource.Id.player1score);
			TextView player1level = FindViewById<TextView> (Resource.Id.player1level);
			TextView player1rows = FindViewById<TextView> (Resource.Id.player1rows);

			RunOnUiThread(() => _gameView.m_player1View.Draw(player1name, player1score, player1level, player1rows));
			
			if (_game._player1._grid.isGameOver())
			{
				RunOnUiThread(() => Utils.ShowAlert (Resource.String.game_over, Resource.String.game_over, this));
			}

			//  Network
			// We send the message to the other player
			if(Network.Instance.Connected())
			{
				// If it is the same piece we only send the position of the piece
				if(isSamePiece)
					Network.Instance.CommunicationWay.Write(_game._player1.getMessagePiece());
				// If it is a new piece, we send the old piece and the new one
				else
				{
					byte[] message = new byte[Constants.SizeMessagePiecePut];
					message[0] = Constants.IdMessagePiecePut;
					for(int i = 0; i < Constants.SizeMessagePiece - 1; i++)
					{
						message[i+1] = messageBuffer[i];
					}
					message = _game._player1._grid.getMessagePiece(message, Constants.SizeMessagePiece);

					// We say if we use the piece sent by the opponent or not (if he didn't send one)
					if(newNextPiece)
						message[Constants.SizeMessagePiecePut-1] = 1;
					else
						message[Constants.SizeMessagePiecePut-1] = 0;

					Network.Instance.CommunicationWay.Write(message);
				}
			}

			// Display of the current model
			FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
		}

		public override void OnBackPressed()
		{
			if(_gameTimer != null)
			{
				if(_gameTimer.Enabled)
					pauseGame(true);
				else
					resumeGame(true);
			}
		}

		//--------------------------------------------------------------
		// PUBLICS METHODES
		//--------------------------------------------------------------
		public int UpdateOpponentView(byte[] message)
		{
			// Interpret the message
			_game._player2.interpretMessage(message);

			// Update of the opponent grid (the display will be done with the other grid)
			_gameView.m_player2View.Update();

			// Display of the model of the opponent
			FindViewById(Resource.Id.OpponentGridView).PostInvalidate();
			TextView player2name = FindViewById<TextView> (Resource.Id.player2name);
			TextView player2score = FindViewById<TextView> (Resource.Id.player2score);
			TextView player2level = FindViewById<TextView> (Resource.Id.player2level);
			TextView player2rows = FindViewById<TextView> (Resource.Id.player2rows);
			_gameView.m_player2View.Draw(player2name, player2score, player2level, player2rows);

			return 0;
		}

		public int OpponentPiecePut(byte[] message)
		{
			UpdateOpponentView(message);
			if(message[Constants.SizeMessagePiecePut - 1] == 1)
			{
				FindViewById<ViewProposedPiece>(Resource.Id.ProposedPiecesView).ChangeProposedPiece();
			}

			return 0;
		}

		//--------------------------------------------------------------
		// PRIVATES METHODES
		//--------------------------------------------------------------
		//pause the game, display a pop-up and send a message to the remote device if asked
		private int pauseGame(bool sendRequestToOverPlayer)
		{
			_gameTimer.Stop();

			//It is us who asked for a pause if we need to notify the other player
			_originPause = sendRequestToOverPlayer;

			// If it is a 2 player game we need to pause the other game
			if(sendRequestToOverPlayer && Network.Instance.Connected())
			{
				byte[] message = new byte[Constants.SizeMessagePause];
				message[0] = Constants.IdMessagePause;
				Network.Instance.CommunicationWay.Write(message);
			}

			Utils.PopUpEndEvent += resumeGame;
			Utils.ShowAlert(Resource.String.Pause_title, Resource.String.Pause, this);

			return 0;
		}

		//resume the game and send a message to the remote device
		private void resumeGame()
		{
			Utils.PopUpEndEvent -= resumeGame;
			if(_originPause)
			{
				resumeGame(true);
			}
		}
		private int resumeGame(bool sendRequestToOverPlayer)
		{
			// If it is a 2 player game we need to resume the other game
			if(sendRequestToOverPlayer && Network.Instance.Connected())
			{
				byte[] message = new byte[Constants.SizeMessageResume];
				message[0] = Constants.IdMessageResume;
				Network.Instance.CommunicationWay.Write(message);
			}

			_gameTimer.AutoReset = true;
			_gameTimer.Interval = getTimerLapse();
			_gameTimer.Start();

			return 0;
		}

		private void associateButtonsEvent()
		{
			FindViewById<Button>(Resource.Id.buttonMoveLeft).Click += delegate {
				_game.MoveLeft();
				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
			};

			FindViewById<Button>(Resource.Id.buttonMoveRight).Click += delegate {
				_game.MoveRight();
				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
			};

			FindViewById<Button>(Resource.Id.buttonTurnLeft).Click += delegate {
				_game.TurnLeft();
				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
			};

			FindViewById<Button>(Resource.Id.buttonTurnRight).Click += delegate {
				_game.TurnRight();
				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
			};

			FindViewById<Button>(Resource.Id.buttonMoveDown).Click += delegate {
				_game.MoveDown();
				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
			};

			FindViewById<Button>(Resource.Id.buttonMoveFoot).Click += delegate {
				_game.MoveBottom();
				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
			};
		}

		private int getTimerLapse()
		{
			return 1000;
		}
	}
}

