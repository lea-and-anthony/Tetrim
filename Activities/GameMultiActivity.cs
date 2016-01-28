using System;
using System.Timers;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Tetrim
{
	[Activity(ScreenOrientation = ScreenOrientation.Portrait)]
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

		private enum GameState
		{
			Play,
			GameOverWin,
			GameOverLost,
			WaitingForRestart,
			OpponentReadyForRestart,
		}

		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		public Player _player2 { get; private set; }
		public PlayerView _player2View { get; private set; } // View of the player 2

		private Bitmap _player2background = null;
		private StopOrigin _originPause = StopOrigin.None;
		private GameState _gameState;
		private readonly object _locker = new object ();

		// for the restart
		private Network.StartState _restartState = Network.StartState.NONE;
		private readonly object _stateLocker = new object (); // locker on _restartState because this variable can be modified in several threads

		//--------------------------------------------------------------
		// EVENT CATCHING METHODS
		//--------------------------------------------------------------
		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);
			SetResult(Result.Ok);

			if(!Network.Instance.Connected)
			{
				// If we are trying to start a multi activity and we are not connected to someone, we stop
				Finish();
			}

			// Creation of the model
			_player2 = new Player(Intent.GetStringExtra(Utils.OpponentNameExtra));

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
			Network.Instance.NewGameMessage += OnReceiveNewGame;
			Network.Instance.WriteEvent += WriteMessageEventReceived;

			_gameState = GameState.Play;
			startGame();
		}

		protected override void OnDestroy ()
		{
			DialogActivity.CloseAll();

			Network.Instance.EraseAllEvent();

			// If we are still playing, we send the end message to the opponent
			if (Network.Instance.Connected && _gameState == GameState.Play)
			{
				Network.Instance.CommunicationWay.Write(_player1.GetEndMessage());
			}

			if(_player2background != null)
			{
				_player2background.Recycle();
				_player2background.Dispose();
				_player2background = null;
			}

			_player2View._gridView.RemoveBitmaps(); // Remove of the bitmap
			FindViewById<ProposedPieceView>(Resource.Id.player2piece).RemoveBitmaps(); // Remove of the dictionnary of bitmap
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
					Network.Instance.ConnectionLostEvent += OnLostConnection;
					// We can restart the game if the connection is working again
					_gameTimer.Start();
					_originPause = StopOrigin.None;
				}
				else
				{
					// We didn't reconnect to the other device so we end the party
					SetResult(Result.Ok);
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
				lock (_locker)
				{
					_gameTimer.Stop();
					if(_gameState == GameState.Play)
					{
						_gameState = GameState.GameOverLost;
						// no need to close all dialog because the timer shouldn't be running if a dialog is displayed
						Intent intent = UtilsDialog.CreateGameOverDialogMulti(this, false);
						StartActivity(intent);
					}
				}
			}

			//  Network
			actualizeViewOtherPlayer(isSamePiece, messageBuffer, newNextPiece);

			//	Speed
			changeSpeedIfNecessary();

			// Display of the current model
			actualizeView();
		}

		public int WriteMessageEventReceived(byte[] writeBuf)
		{
			if(writeBuf[0] == Constants.IdMessageResume)
			{
				_gameTimer.AutoReset = true;
				_gameTimer.Interval = getTimerLapse();
				_gameTimer.Start();

				_originPause = StopOrigin.None;
			}
			else if(writeBuf[0] == Constants.IdMessageNewGame)
			{
				lock (_stateLocker)
				{
					if(_restartState == Network.StartState.OPPONENT_READY)
					{
						SetResult(Result.FirstUser);
						Finish();
					}
					else if(_restartState == Network.StartState.NONE)
					{
						_restartState = Network.StartState.WAITING_FOR_OPPONENT;
					}
				}
			}
			return 0;
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
			lock (_locker)
			{
				if(_gameTimer != null)
					_gameTimer.Stop();
				
				if(_gameState == GameState.Play)
				{
					_gameState = GameState.GameOverWin;

					DialogActivity.CloseAll();
					Intent intent = UtilsDialog.CreateGameOverDialogMulti(this, true);
					StartActivity(intent);
				}
			}
			return 0;
		}

		private int OnLostConnection(byte[] message)
		{
			#if DEBUG
			Log.Debug(Tag, "OnLostConnection");
			#endif

			DialogActivity.CloseAll();

			if(_gameState == GameState.Play)
			{
				// If we lost the connection, we stop the game display a pop-up and try to reconnect
				Network.Instance.ConnectionLostEvent -= OnLostConnection;
				_gameTimer.Stop();
				_originPause = StopOrigin.LostConnection;

				ReconnectActivity._messageFail = message;
				var serverIntent = new Intent(this, typeof(ReconnectActivity));
				StartActivityForResult(serverIntent,(int) Utils.RequestCode.RequestReconnect);
			}
			else
			{
				Intent intent = null;

				switch(_gameState)
				{
				case GameState.GameOverWin:
					intent = DialogActivity.CreateYesDialog(this, Resource.String.ConnectionLost, Resource.String.cannotRestartGameWin, Resource.String.ok, delegate {Finish();});
					break;
				case GameState.GameOverLost:
					intent = DialogActivity.CreateYesDialog(this, Resource.String.ConnectionLost, Resource.String.cannotRestartGameLost, Resource.String.ok, delegate {Finish();});
					break;
				case GameState.OpponentReadyForRestart:
				case GameState.WaitingForRestart: // display of the same pop-up in these case
					intent = DialogActivity.CreateYesDialog(this, Resource.String.ConnectionLost, Resource.String.lostFriend, Resource.String.ok, delegate {Finish();});
					break;
				}

				SetResult(Result.Ok);
				StartActivity(intent);
			}

			return 0;
		}

		private int OnReceiveNewGame()
		{
			lock (_stateLocker)
			{
				if(_restartState != Network.StartState.WAITING_FOR_OPPONENT)
				{
					_restartState = Network.StartState.OPPONENT_READY;
				}
				else
				{
					SetResult(Result.FirstUser);
					Finish();
				}
			}
			return 0;
		}

		//--------------------------------------------------------------
		// PUBLIC METHODS
		//--------------------------------------------------------------
		// Resume the game if it is us who asked for the pause
		public override void ResumeGame()
		{
			if(_originPause == StopOrigin.MyPause)
			{
				resumeGame(true);
			}
		}

		public override void NewGame()
		{
			byte[] message = {Constants.IdMessageNewGame};
			// We notify the opponent that we are ready for a new game
			Network.Instance.CommunicationWay.Write(message);

			if(_restartState == Network.StartState.NONE)
			{
				Intent dialog = DialogActivity.CreateYesNoDialog(this, -1, Resource.String.waiting_for_opponent,
					Resource.String.cancel, -1, delegate{SetResult(Result.Ok); Finish();}, null);
				StartActivity(dialog);
			}
		}

		//--------------------------------------------------------------
		// PROTECTED METHODS
		//--------------------------------------------------------------
		protected override void initializeUI()
		{
			base.initializeUI();
			
			UtilsUI.SetGamePlayerNameText(this, Resource.Id.player2name, false);
			UtilsUI.SetGamePlayerStatText(this, Resource.Id.player2score, false, false);
			UtilsUI.SetGamePlayerStatText(this, Resource.Id.player2rows, false, false);
			UtilsUI.SetGamePlayerStatText(this, Resource.Id.player2level, false, false);
			UtilsUI.SetGamePlayerStatText(this, Resource.Id.score2, false, true);
			UtilsUI.SetGamePlayerStatText(this, Resource.Id.rows2, false, true);
			UtilsUI.SetGamePlayerStatText(this, Resource.Id.level2, false, true);
			UtilsUI.SetGamePlayerStatText(this, Resource.Id.piece2, false, true);

			ProposedPieceView proposedPiecesView = FindViewById<ProposedPieceView>(Resource.Id.player2piece);
			proposedPiecesView.SetBackgroundColor(Utils.getAndroidColor(TetrisColor.Red));

			// Center the opponent grid
			Point size = GridView.CalculateUseSize(_player2View._gridView.MeasuredWidth, _player2View._gridView.MeasuredHeight);
			LinearLayout.LayoutParams newLayoutParams = new LinearLayout.LayoutParams(size.X, size.Y);
			newLayoutParams.Gravity = GravityFlags.CenterHorizontal;
			_player2View._gridView.LayoutParameters = newLayoutParams;

			setBackground();
		}

		private void setBackground()
		{
			LinearLayout player2layout = FindViewById<LinearLayout>(Resource.Id.player2layout);

			if(_player2background != null)
			{
				_player2background.Recycle();
				_player2background.Dispose();
			}
			// Create image
			_player2background = Bitmap.CreateBitmap(player2layout.Width, player2layout.Height, Bitmap.Config.Argb8888);
			Canvas backCanvas = new Canvas(_player2background);

			// Background stroke paint
			// TODO : same width as buttons and set layout margins
			float strokeBorderWidth = (FindViewById<ButtonStroked>(Resource.Id.buttonMoveLeft)).Settings.StrokeBorderWidth;
			int padding = (int)strokeBorderWidth/2 + Utils.GetPixelsFromDP(this, 5);
			player2layout.SetPadding(padding, 0, 0, padding);

			Paint strokeBackPaint = new Paint();
			strokeBackPaint.Color = Utils.getAndroidColor(TetrisColor.Red);
			strokeBackPaint.SetStyle(Paint.Style.Stroke);
			strokeBackPaint.StrokeWidth = strokeBorderWidth/2;
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
			int radiusIn = (FindViewById<ButtonStroked>(Resource.Id.buttonMoveLeft)).Settings.RadiusIn;
			int radiusOut = (FindViewById<ButtonStroked>(Resource.Id.buttonMoveLeft)).Settings.RadiusOut;
			backCanvas.DrawRoundRect(bounds, radiusOut, radiusOut, strokeBackPaint);

			// Use it as background
			player2layout.SetBackgroundDrawable(new BitmapDrawable(_player2background));

			backCanvas.Dispose();
		}
		
		// Pause the game, display a pop-up and send a message to the remote device if asked
		protected override int pauseGame(bool requestFromUser)
		{
			_gameTimer.Stop();

			// Set the origin so it is the right player who restart the game
			_originPause = requestFromUser ? StopOrigin.MyPause : StopOrigin.PauseOpponent;

			// We need to pause the other game if it is not it which stop us
			string name = null;
			if(requestFromUser)
			{
				byte[] message = {Constants.IdMessagePause};
				Network.Instance.CommunicationWay.Write(message);
			}
			else
			{
				name = _player2._name;
			}

			Intent intent = UtilsDialog.CreatePauseGameDialog(this, name);
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
			if(_player1.MoveDown() && _gameTimer.Enabled)
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
			if(_player1.MoveBottom() && _gameTimer.Enabled)
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
			else
			{
				// Restart the timer if it is the opponeny who send the message to restart
				// Else we are going to wait that he receives the message
				DialogActivity.CloseAll();

				_gameTimer.AutoReset = true;
				_gameTimer.Interval = getTimerLapse();
				_gameTimer.Start();

				_originPause = StopOrigin.None;
			}

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
