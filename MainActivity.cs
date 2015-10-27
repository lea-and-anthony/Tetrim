using System;
using System.Timers;

using Android.App;
using Android.Content;
using Android.Widget;
using Android.OS;
using Android.Util;
using Android.Bluetooth;

namespace Tetris
{
	[Activity(Label = "Tetris", Icon = "@drawable/icon", Theme = "@android:style/Theme.NoTitleBar.Fullscreen")]
	public class MainActivity : Activity
	{
		//--------------------------------------------------------------
		// CONSTANTS
		//--------------------------------------------------------------
		private const string TAG = "Tetris-MainActivity";

		//--------------------------------------------------------------
		// MEMBERS
		//--------------------------------------------------------------
		private Timer gameTimer = null;
		private bool originPause = false; // Set to true if it's us who ask for the pause

		public Game m_game { get; private set; }
		public GameView m_gameView { get; private set; }


		//--------------------------------------------------------------
		// EVENT REPONDING METHODES
		//--------------------------------------------------------------
		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			// Set our view from the "main" layout resource
			SetContentView(Resource.Layout.Main);

			// Creation of the model
			m_game = new Game();

			// Creation of the view
			m_gameView = new GameView(m_game);
			MyView view = FindViewById<MyView>(Resource.Id.PlayerGridView);
			view.m_gridView = m_gameView.m_player1View._gridView;
			// If it is a 2 player game
			if(Network.Instance.Connected())
			{
				MyView view2 = FindViewById<MyView>(Resource.Id.OpponentGridView);
				view2.m_gridView = m_gameView.m_player2View._gridView;

				ViewProposedPiece viewProposed = FindViewById<ViewProposedPiece>(Resource.Id.ProposedPiecesView);
				viewProposed.SetPlayer(m_game.m_player1);
				viewProposed.SetBluetooth(Network.Instance.CommunicationWay);
			}

			// Linkage of the button with the methods
			associateButtonsEvent();

			// Launch the main timer of the application
			int time = getTimerLapse();
			gameTimer = new Timer(time);
			gameTimer.Elapsed += new ElapsedEventHandler(OnTimerElapsed);
			gameTimer.Interval = time;
			gameTimer.AutoReset = true;
			gameTimer.Start();

			// Hook on network event
			Network.Instance.UsualGameMessage += UpdateOpponentView;
			Network.Instance.PiecePutMessage += OpponentPiecePut;
			Network.Instance.NextPieceMessage += m_game.m_player1.interpretMessage;
			Network.Instance.PauseMessage += pauseGame;
			Network.Instance.ResumeMessage += resumeGame;
		}

		protected override void OnPause()
		{
			base.OnPause();

			if(gameTimer != null)
			{
				if(gameTimer.Enabled)
					pauseGame(true);
				else
					resumeGame();
			}
		}

		protected override void OnDestroy ()
		{
			base.OnDestroy ();

			// Stop the timer
			if (gameTimer != null)
			{
				gameTimer.Stop();
				gameTimer = null;
    		}

			#if DEBUG
			Log.Error (TAG, "--- ON DESTROY ---");
			#endif
		}

		private void OnTimerElapsed(object source, ElapsedEventArgs e)
		{
			bool newNextPiece = m_game.m_player1.m_grid.m_isNextPieceModified;
			byte[] messageBuffer = new byte[Constants.SizeMessagePiece - 1];
			messageBuffer = m_game.m_player1.m_grid.m_fallingPiece.getMessage(messageBuffer, 0);

			bool isSamePiece = m_game.m_player1.m_grid.MovePieceDown(m_game.m_player1);
			if(!isSamePiece)
				m_gameView.m_player1View.Update();

			TextView player1name = FindViewById<TextView> (Resource.Id.player1name);
			TextView player1score = FindViewById<TextView> (Resource.Id.player1score);
			TextView player1level = FindViewById<TextView> (Resource.Id.player1level);
			TextView player1rows = FindViewById<TextView> (Resource.Id.player1rows);

			RunOnUiThread(() => m_gameView.m_player1View.Draw(player1name, player1score, player1level, player1rows));
			
			if (m_game.m_player1.m_grid.isGameOver())
			{
				RunOnUiThread(() => Utils.ShowAlert (Resource.String.game_over, Resource.String.game_over, this));
			}

			//  Network
			// We send the message to the other player
			if(Network.Instance.Connected())
			{
				// If it is the same piece we only send the position of the piece
				if(isSamePiece)
					Network.Instance.CommunicationWay.Write(m_game.m_player1.getMessagePiece());
				// If it is a new piece, we send the old piece and the new one
				else
				{
					byte[] message = new byte[Constants.SizeMessagePiecePut];
					message[0] = Constants.IdMessagePiecePut;
					for(int i = 0; i < Constants.SizeMessagePiece - 1; i++)
					{
						message[i+1] = messageBuffer[i];
					}
					message = m_game.m_player1.m_grid.getMessagePiece(message, Constants.SizeMessagePiece);

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
			if(gameTimer != null)
			{
				if(gameTimer.Enabled)
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
			m_game.m_player2.interpretMessage(message);

			// Update of the opponent grid (the display will be done with the other grid)
			m_gameView.m_player2View.Update();

			// Display of the model of the opponent
			FindViewById(Resource.Id.OpponentGridView).PostInvalidate();
			TextView player2name = FindViewById<TextView> (Resource.Id.player2name);
			TextView player2score = FindViewById<TextView> (Resource.Id.player2score);
			TextView player2level = FindViewById<TextView> (Resource.Id.player2level);
			TextView player2rows = FindViewById<TextView> (Resource.Id.player2rows);
			m_gameView.m_player2View.Draw(player2name, player2score, player2level, player2rows);

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
			gameTimer.Stop();

			//It is us who asked for a pause if we need to notify the other player
			originPause = sendRequestToOverPlayer;

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
			if(originPause)
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

			gameTimer.AutoReset = true;
			gameTimer.Interval = getTimerLapse();
			gameTimer.Start();

			return 0;
		}

		private void associateButtonsEvent()
		{
			FindViewById<Button>(Resource.Id.buttonMoveLeft).Click += delegate {
				m_game.MoveLeft();
				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
			};

			FindViewById<Button>(Resource.Id.buttonMoveRight).Click += delegate {
				m_game.MoveRight();
				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
			};

			FindViewById<Button>(Resource.Id.buttonTurnLeft).Click += delegate {
				m_game.TurnLeft();
				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
			};

			FindViewById<Button>(Resource.Id.buttonTurnRight).Click += delegate {
				m_game.TurnRight();
				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
			};

			FindViewById<Button>(Resource.Id.buttonMoveDown).Click += delegate {
				m_game.MoveDown();
				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
			};

			FindViewById<Button>(Resource.Id.buttonMoveFoot).Click += delegate {
				m_game.MoveBottom();
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


