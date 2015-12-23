using System;
using System.Timers;

using Android.App;
using Android.Graphics;
using Android.Content;
using Android.Widget;
using Android.OS;
using Android.Util;
using Android.Bluetooth;
using Android.Views;

namespace Tetrim
{
	[Activity(Label = "Tetrim", Icon = "@drawable/icon", Theme = "@android:style/Theme.NoTitleBar.Fullscreen", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
	public class MainActivity : Activity, ViewTreeObserver.IOnGlobalLayoutListener
	{
		//--------------------------------------------------------------
		// CONSTANTS
		//--------------------------------------------------------------
		private const string Tag = "Tetrim-MainActivity";

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
		private Timer _gameTimer = null;
		private StopOrigin _originPause = StopOrigin.None;

		public Game _game { get; private set; }
		public GameView _gameView { get; private set; }

		//--------------------------------------------------------------
		// EVENT CATCHING METHODES
		//--------------------------------------------------------------
		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			// Set our view from the "main" layout resource
			SetContentView(Resource.Layout.GameMulti);

			// Creation of the model
			_game = new Game();

			LinearLayout gameLayout = FindViewById<LinearLayout>(Resource.Id.layoutGameMulti);

			// Test if the view is created so we can resize the buttons
			if(gameLayout.ViewTreeObserver.IsAlive)
			{
				gameLayout.ViewTreeObserver.AddOnGlobalLayoutListener(this);
			}

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

				// Hook on network event
				Network.Instance.EraseAllEvent();
				Network.Instance.UsualGameMessage += UpdateOpponentView;
				Network.Instance.PiecePutMessage += OpponentPiecePut;
				Network.Instance.NextPieceMessage += _game._player1.interpretMessage;
				Network.Instance.PauseMessage += pauseGame;
				Network.Instance.ResumeMessage += resumeGame;
				Network.Instance.ScoreMessage += OnReceiveScoreMessage;
				Network.Instance.EndMessage += OnReceiveEndMessage;
				Network.Instance.ConnectionLostEvent += OnLostConnection;
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
		}

		public void OnGlobalLayout()
		{
			// The view is completely loaded now, so getMeasuredWidth() won't return 0
			InitializeUI();

			// Destroy the onGlobalLayout afterwards, otherwise it keeps changing
			// the sizes non-stop, even though it's already done
			LinearLayout gameLayout = FindViewById<LinearLayout>(Resource.Id.layoutGameMulti);
			gameLayout.ViewTreeObserver.RemoveGlobalOnLayoutListener(this);
		}

		protected void InitializeUI()
		{
			// Set the text font
			Typeface niceFont = Typeface.CreateFromAsset(Assets,"Foo.ttf");
			SetText(niceFont, Resource.Id.player1name);
			SetText(niceFont, Resource.Id.player2name);
			SetText(niceFont, Resource.Id.player1score);
			SetText(niceFont, Resource.Id.player2score);
			SetText(niceFont, Resource.Id.player1rows);
			SetText(niceFont, Resource.Id.player2rows);
			SetText(niceFont, Resource.Id.player1level);
			SetText(niceFont, Resource.Id.player2level);
			SetText(niceFont, Resource.Id.score1);
			SetText(niceFont, Resource.Id.score2);
			SetText(niceFont, Resource.Id.rows1);
			SetText(niceFont, Resource.Id.rows2);
			SetText(niceFont, Resource.Id.level1);
			SetText(niceFont, Resource.Id.level2);

			// Set the buttons
			Typeface arrowFont = Typeface.CreateFromAsset(Assets,"Arrows.otf");
			SetButton(arrowFont, Resource.Id.buttonMoveLeft, Resource.String.left_arrow);
			SetButton(arrowFont, Resource.Id.buttonMoveRight, Resource.String.right_arrow);
			SetButton(arrowFont, Resource.Id.buttonTurnLeft, Resource.String.turn_left_arrow);
			SetButton(arrowFont, Resource.Id.buttonTurnRight, Resource.String.turn_right_arrow);
			SetButton(arrowFont, Resource.Id.buttonMoveDown, Resource.String.down_arrow);
			SetButton(arrowFont, Resource.Id.buttonMoveFoot, Resource.String.bottom_arrow);
		}

		protected void SetButton(Typeface font, int idButton, int idText)
		{
			ButtonStroked button = FindViewById<ButtonStroked>(idButton);
			button.IsSquared = true;
			button.SetTypeface(font, TypefaceStyle.Normal);
			button.Text = Resources.GetString(idText);
			button.SetMaxHeight(button.MeasuredWidth);
			button.SetMinimumHeight(button.MeasuredWidth);
			button.SetTextSize(ComplexUnitType.Px, button.MeasuredWidth);
		}

		protected void SetText(Typeface font, int idText)
		{
			TextView textView = FindViewById<TextView> (idText);
			textView.SetTypeface(font, TypefaceStyle.Normal);
		}

		// Called when an other application is displayed in front of this one
		// So here we are going to enter the pause
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
				_gameTimer.Stop();
				Utils.PopUpEndEvent += endGame;
				RunOnUiThread(() => Utils.ShowAlert (Resource.String.game_over_loose_title, Resource.String.game_over_loose, this));
			}

			//  Network
			actualizeViewOtherPlayer(isSamePiece, messageBuffer, newNextPiece);

			// Display of the current model
			FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
		}

		protected override void OnActivityResult (int requestCode, Result resultCode, Intent data)
		{
			#if DEBUG
			Log.Debug(Tag, "onActivityResult " + resultCode);
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

		private int OnReceiveScoreMessage(byte[] message)
		{
			_game._player2.InterpretScoreMessage(message);
			return 0;
		}

		private int OnReceiveEndMessage(byte[] message)
		{
			_gameTimer.Stop();
			Utils.PopUpEndEvent += endGame;
			RunOnUiThread(() => Utils.ShowAlert (Resource.String.game_over_win_title, Resource.String.game_over_win, this));
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

			var serverIntent = new Intent(this, typeof(ReconnectActivity));
			StartActivityForResult(serverIntent,(int) Utils.RequestCode.RequestReconnect);

			return 0;
		}

		//--------------------------------------------------------------
		// PRIVATES METHODES
		//--------------------------------------------------------------
		private int UpdateOpponentView(byte[] message)
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

		private int OpponentPiecePut(byte[] message)
		{
			UpdateOpponentView(message);
			if(message[Constants.SizeMessagePiecePut - 1] == 1)
			{
				FindViewById<ViewProposedPiece>(Resource.Id.ProposedPiecesView).ChangeProposedPiece();
			}

			return 0;
		}

		//pause the game, display a pop-up and send a message to the remote device if asked
		private int pauseGame(bool sendRequestToOverPlayer)
		{
			_gameTimer.Stop();

			// Set the origin so it is the right player who restart the game
			_originPause = sendRequestToOverPlayer ? StopOrigin.MyPause : StopOrigin.PauseOpponent;

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
			if(_originPause == StopOrigin.MyPause)
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
			_originPause = StopOrigin.None;

			return 0;
		}

		private void endGame()
		{
			Finish();
		}

		private void associateButtonsEvent()
		{
			FindViewById<ButtonStroked>(Resource.Id.buttonMoveLeft).Click += delegate {
				_game.MoveLeft();
				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
			};

			FindViewById<ButtonStroked>(Resource.Id.buttonMoveRight).Click += delegate {
				_game.MoveRight();
				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
			};

			FindViewById<ButtonStroked>(Resource.Id.buttonTurnLeft).Click += delegate {
				_game.TurnLeft();
				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
			};

			FindViewById<ButtonStroked>(Resource.Id.buttonTurnRight).Click += delegate {
				_game.TurnRight();
				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
			};

			FindViewById<ButtonStroked>(Resource.Id.buttonMoveDown).Click += delegate {
				_gameTimer.Stop();

				_game.MoveDown();
				actualizeViewOtherPlayer(true, null, false);
				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();

				_gameTimer.Start();
			};

			FindViewById<ButtonStroked>(Resource.Id.buttonMoveFoot).Click += delegate {
				_gameTimer.Stop();

				_game.MoveBottom();
				actualizeViewOtherPlayer(true, null, false);
				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();

				_gameTimer.Start();
			};
		}

		// isSamePiece must be set to false if it is a new piece that is falling
		// in this case messageBuffer must contains the message of the old piece before it was added to the map and
		// newNextPiece must be set to true if we used a piece sent by our opponent, false otherwise
		// if it is still the samePiece that is falling, isSamePiece must be set to true and messageBuffer and newNextPiece are ignored
		private void actualizeViewOtherPlayer(bool isSamePiece, byte[] messageBuffer, bool newNextPiece)
		{
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

					// We say if we used the piece sent by the opponent or not (if he didn't send one)
					if(newNextPiece)
						message[Constants.SizeMessagePiecePut-1] = 1;
					else
						message[Constants.SizeMessagePiecePut-1] = 0;

					Network.Instance.CommunicationWay.Write(message);
				}

				if(!isSamePiece) // the piece was added on the map so the score changed
				{
					Network.Instance.CommunicationWay.Write(_game._player1.GetScoreMessage());
				}

				if (_game._player1._grid.isGameOver())
				{
					Network.Instance.CommunicationWay.Write(_game._player1.GetEndMessage());
				}
			}
		}

		private int getTimerLapse()
		{
			return 1000;
		}
	}
}


