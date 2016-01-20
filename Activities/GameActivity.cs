using System;
using System.Timers;

using Android.App;
using Android.Graphics;
using Android.Widget;
using Android.Util;
using Android.Views;

namespace Tetrim
{
	public abstract class GameActivity : Activity, ViewTreeObserver.IOnGlobalLayoutListener
	{
		//--------------------------------------------------------------
		// CONSTANTS
		//--------------------------------------------------------------
		protected const string Tag = "Tetrim-GameActivity";

		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		protected Timer _gameTimer = null;

		public Player _player1 { get; protected set; }
		public PlayerView _player1View { get; protected set; } // View of the player 1

		//--------------------------------------------------------------
		// EVENT CATCHING METHODES
		//--------------------------------------------------------------
		public void OnGlobalLayout()
		{
			// The view is completely loaded now, so getMeasuredWidth() won't return 0
			initializeUI();

			// Destroy the onGlobalLayout afterwards, otherwise it keeps changing
			// the sizes non-stop, even though it's already done
			LinearLayout gameLayout = FindViewById<LinearLayout>(Resource.Id.layoutGame);
			gameLayout.ViewTreeObserver.RemoveGlobalOnLayoutListener(this);
		}

		// Called when an other application is displayed in front of this one
		// So here we are going to enter the pause
		protected override void OnPause()
		{
			base.OnPause();

			if(_gameTimer != null && _gameTimer.Enabled)
			{
				pauseGame(true);
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

		public override void OnBackPressed()
		{
			if(_gameTimer != null)
			{
				if(_gameTimer.Enabled)
					pauseGame(true);
				else
					ResumeGame();
			}
		}

		//--------------------------------------------------------------
		// ABSTRATCS METHODES
		//--------------------------------------------------------------
		protected abstract void OnTimerElapsed(object source, ElapsedEventArgs e);
		protected abstract int pauseGame(bool requestFromUser);
		public abstract void ResumeGame();
		protected abstract void moveLeftButtonPressed(object sender, EventArgs e);
		protected abstract void moveRightButtonPressed(object sender, EventArgs e);
		protected abstract void turnLeftButtonPressed(object sender, EventArgs e);
		protected abstract void turnRightButtonPressed(object sender, EventArgs e);
		protected abstract void moveDownButtonPressed(object sender, EventArgs e);
		protected abstract void moveFootButtonPressed(object sender, EventArgs e);

		//--------------------------------------------------------------
		// PROTECTED METHODES
		//--------------------------------------------------------------
		// Init the model and the view for a one player game
		protected void initGame()
		{
			LinearLayout gameLayout = FindViewById<LinearLayout>(Resource.Id.layoutGame);

			// Test if the view is created so we can resize the buttons
			if(gameLayout.ViewTreeObserver.IsAlive)
			{
				gameLayout.ViewTreeObserver.AddOnGlobalLayoutListener(this);
			}

			// Creation of the model
			_player1 = new Player();

			// And the view
			_player1View = new PlayerView(_player1);

			GridView myGrid = FindViewById<GridView>(Resource.Id.PlayerGridView);
			myGrid.Init(_player1._grid);
			_player1View._gridView = myGrid;

			TextView player1name = FindViewById<TextView> (Resource.Id.player1name);
			TextView player1score = FindViewById<TextView> (Resource.Id.player1score);
			TextView player1level = FindViewById<TextView> (Resource.Id.player1level);
			TextView player1rows = FindViewById<TextView> (Resource.Id.player1rows);
			_player1View.SetViews(player1name, player1score, player1level, player1rows);

			associateButtonsEvent();
		}

		// Start the timer of the game
		protected void startGame()
		{
			int time = getTimerLapse();
			_gameTimer = new Timer(time);
			_gameTimer.Elapsed += OnTimerElapsed;
			_gameTimer.Interval = time;
			_gameTimer.AutoReset = true;
			_gameTimer.Start();
		}

		protected virtual void initializeUI()
		{
			setPlayerName(Resource.Id.player1name, true);
			setPlayerStat(Resource.Id.player1score, true, false);
			setPlayerStat(Resource.Id.player1rows, true, false);
			setPlayerStat(Resource.Id.player1level, true, false);
			setPlayerStat(Resource.Id.score1, true, true);
			setPlayerStat(Resource.Id.rows1, true, true);
			setPlayerStat(Resource.Id.level1, true, true);
			setPlayerStat(Resource.Id.piece1, true, true);

			// Change the size of the components to center them
			GridView myGrid = FindViewById<GridView>(Resource.Id.PlayerGridView);
			Point size = GridView.CalculateUseSize(myGrid.Width, myGrid.Height);
			int difference = (myGrid.Width - size.X) / 2;
			myGrid.LayoutParameters = new LinearLayout.LayoutParams(size.X, size.Y);

			// Change the size of the components to center them
			NextPieceView nextPieceView = FindViewById<NextPieceView>(Resource.Id.player1piece);
			nextPieceView.SetPlayer(_player1);
			nextPieceView.SetBackgroundColor(Utils.getAndroidColor(TetrisColor.Cyan));

			// Set the buttons
			UtilsUI.SetArrowButton(FindViewById<ButtonStroked>(Resource.Id.buttonMoveLeft), TetrisColor.Green, difference);
			UtilsUI.SetArrowButton(FindViewById<ButtonStroked>(Resource.Id.buttonMoveRight), TetrisColor.Green, difference);
			UtilsUI.SetArrowButton(FindViewById<ButtonStroked>(Resource.Id.buttonTurnLeft), TetrisColor.Cyan, difference);
			UtilsUI.SetArrowButton(FindViewById<ButtonStroked>(Resource.Id.buttonTurnRight), TetrisColor.Cyan, difference);
			UtilsUI.SetArrowButton(FindViewById<ButtonStroked>(Resource.Id.buttonMoveDown), TetrisColor.Red, difference);
			UtilsUI.SetArrowButton(FindViewById<ButtonStroked>(Resource.Id.buttonMoveFoot), TetrisColor.Red, difference);

			// Reset the weight sum of the layout containing the grid and the buttons because
			// now we have set the size it is useless and it will allow the layout to actualize
			LinearLayout gameLayout = FindViewById<LinearLayout>(Resource.Id.gridButtonLayout);
			gameLayout.WeightSum = 0;
		}

		protected void setPlayerStat(int id, bool me, bool isTitle)
		{
			TextView textView = FindViewById<TextView>(id);
			UtilsUI.SetTextFont(textView);
			textView.SetBackgroundColor(Utils.getAndroidColor(me ? TetrisColor.Cyan : TetrisColor.Red));
			textView.SetTextColor(!isTitle ? (me ? UtilsUI.Player1Background : UtilsUI.Player2Background)
				: Utils.getAndroidDarkColor(me ? TetrisColor.Cyan : TetrisColor.Red));
		}

		protected void setPlayerName(int id, bool me)
		{
			TextView textView = FindViewById<TextView>(id);
			UtilsUI.SetTextFont(textView);
			textView.SetTextColor(Utils.getAndroidColor(me ? TetrisColor.Cyan : TetrisColor.Red));
		}

		protected void endGame()
		{
			Finish();
		}

		protected void associateButtonsEvent()
		{
			FindViewById<ButtonStroked>(Resource.Id.buttonMoveLeft).Click += moveLeftButtonPressed;
			FindViewById<ButtonStroked>(Resource.Id.buttonMoveRight).Click += moveRightButtonPressed;
			FindViewById<ButtonStroked>(Resource.Id.buttonTurnLeft).Click += turnLeftButtonPressed;
			FindViewById<ButtonStroked>(Resource.Id.buttonTurnRight).Click += turnRightButtonPressed;
			FindViewById<ButtonStroked>(Resource.Id.buttonMoveDown).Click += moveDownButtonPressed;
			FindViewById<ButtonStroked>(Resource.Id.buttonMoveFoot).Click += moveFootButtonPressed;
		}

		protected int getTimerLapse()
		{
			return 1000;
		}
	}
}


