using System;
using System.Timers;

using Android.App;
using Android.Graphics;
using Android.Graphics.Drawables;
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

			// Creation of the model
			_player2 = new Player();

			// And the view
			_player2View = new PlayerView(_player2);

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

			ProposedPieceView viewProposed = FindViewById<ProposedPieceView>(Resource.Id.player2piece);
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
			byte[] messageBuffer = new byte[Constants.SizeMessage[Constants.IdMessagePiece] - 1];
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
				Intent intent = UtilsDialog.CreateGameOverDialogMulti(this, false);
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
			Intent intent = UtilsDialog.CreateGameOverDialogMulti(this, true);
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
		// PUBLIC METHODES
		//--------------------------------------------------------------
		// Resume the game if it is us who asked for the pause
		public override void ResumeGame()
		{
			if(_originPause == StopOrigin.MyPause)
			{
				resumeGame(true);
			}
		}

		//--------------------------------------------------------------
		// PROTECTED METHODES
		//--------------------------------------------------------------
		protected override void initializeUI()
		{
			base.initializeUI();
			
			setPlayerName(Resource.Id.player2name, false);
			setPlayerStat(Resource.Id.player2score, false, false);
			setPlayerStat(Resource.Id.player2rows, false, false);
			setPlayerStat(Resource.Id.player2level, false, false);
			setPlayerStat(Resource.Id.score2, false, true);
			setPlayerStat(Resource.Id.rows2, false, true);
			setPlayerStat(Resource.Id.level2, false, true);
			setPlayerStat(Resource.Id.piece2, false, true);

			ProposedPieceView proposedPiecesView = FindViewById<ProposedPieceView>(Resource.Id.player2piece);
			proposedPiecesView.SetBackgroundColor(Utils.getAndroidColor(TetrisColor.Red));
			
			setBackground();
		}

		private void setBackground()
		{
			LinearLayout player2layout = FindViewById<LinearLayout>(Resource.Id.player2layout);
			// Create image
			Bitmap player2background = Bitmap.CreateBitmap(player2layout.Width, player2layout.Height, Bitmap.Config.Argb8888);
			Canvas backCanvas = new Canvas(player2background);

			// Background fill paint
			Paint fillBackPaint = new Paint();
			fillBackPaint.Color = UtilsUI.Player2Background;
			fillBackPaint.AntiAlias = true;

			// Background stroke paint
			// TODO : same width as buttons and set layout margins
			int strokeBorderWidth = Utils.GetPixelsFromDP(BaseContext, 10);
			Paint strokeBackPaint = new Paint();
			strokeBackPaint.Color = Utils.getAndroidColor(TetrisColor.Red);
			strokeBackPaint.SetStyle(Paint.Style.Stroke);
			strokeBackPaint.StrokeWidth = strokeBorderWidth;
			strokeBackPaint.AntiAlias = true;

			// Get rectangle
			Rect local = new Rect();
			player2layout.GetLocalVisibleRect(local);
			RectF bounds = new RectF(local);
			bounds.Left += strokeBorderWidth/2;
			bounds.Bottom -= strokeBorderWidth/2;
			bounds.Top -= strokeBorderWidth;
			bounds.Right += strokeBorderWidth;

			// Actually draw background
			int radiusIn = Utils.GetPixelsFromDP(BaseContext, 7);
			int radiusOut = Utils.GetPixelsFromDP(BaseContext, 5);
			backCanvas.DrawRoundRect(bounds, radiusOut, radiusOut, strokeBackPaint);
			backCanvas.DrawRoundRect(bounds, radiusIn, radiusIn, fillBackPaint);

			// Use it as background
			player2layout.SetBackgroundDrawable(new BitmapDrawable(player2background));
		}
		
		// Pause the game, display a pop-up and send a message to the remote device if asked
		protected override int pauseGame(bool requestFromUser)
		{
			_gameTimer.Stop();

			// Set the origin so it is the right player who restart the game
			_originPause = requestFromUser ? StopOrigin.MyPause : StopOrigin.PauseOpponent;

			// We need to pause the other game if it is not it which stop us
			if(requestFromUser)
			{
				byte[] message = {Constants.IdMessagePause};
				Network.Instance.CommunicationWay.Write(message);
			}

			Intent intent = UtilsDialog.CreatePauseGameDialog(this);
			StartActivity(intent);

			return 0;
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
				byte[] message = {Constants.IdMessageResume};
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
			if(message[Constants.SizeMessage[Constants.IdMessagePiecePut] - 1] == 1)
			{
				FindViewById<ProposedPieceView>(Resource.Id.player2piece).ChangeProposedPiece();
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
				byte[] message = new byte[Constants.SizeMessage[Constants.IdMessagePiecePut]];
				message[0] = Constants.IdMessagePiecePut;
				for(int i = 0; i < Constants.SizeMessage[Constants.IdMessagePiece] - 1; i++)
				{
					message[i+1] = messageBuffer[i];
				}
				message = _player1._grid.getMessagePiece(message, Constants.SizeMessage[Constants.IdMessagePiece]);

				// We say if we used the piece sent by the opponent or not (if he didn't send one)
				if(newNextPiece)
					message[Constants.SizeMessage[Constants.IdMessagePiecePut]-1] = 1;
				else
					message[Constants.SizeMessage[Constants.IdMessagePiecePut]-1] = 0;

				Network.Instance.CommunicationWay.Write(message);
			}
		}
	}
}
