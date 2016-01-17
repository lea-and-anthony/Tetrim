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
			LinearLayout gameLayout = FindViewById<LinearLayout>(Resource.Id.layoutGameMulti);
			gameLayout.ViewTreeObserver.RemoveGlobalOnLayoutListener(this);
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

		public override void OnBackPressed()
		{
			if(_gameTimer != null)
			{
				if(_gameTimer.Enabled)
					pauseGame(true);
				else
					resumeGame();
			}
		}

		//--------------------------------------------------------------
		// ABSTRATCS METHODES
		//--------------------------------------------------------------
		protected abstract void OnTimerElapsed(object source, ElapsedEventArgs e);
		protected abstract int pauseGame(bool requestFromUser);
		protected abstract void resumeGame();
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
			LinearLayout gameLayout = FindViewById<LinearLayout>(Resource.Id.layoutGameMulti);

			// Test if the view is created so we can resize the buttons
			if(gameLayout.ViewTreeObserver.IsAlive)
			{
				gameLayout.ViewTreeObserver.AddOnGlobalLayoutListener(this);
			}

			// Creation of the model
			_player1 = new Player();

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
			Utils.SetTextFont(FindViewById<TextView>(Resource.Id.player1name));
			Utils.SetTextFont(FindViewById<TextView>(Resource.Id.player2name));
			Utils.SetTextFont(FindViewById<TextView>(Resource.Id.player1score));
			Utils.SetTextFont(FindViewById<TextView>(Resource.Id.player2score));
			Utils.SetTextFont(FindViewById<TextView>(Resource.Id.player1rows));
			Utils.SetTextFont(FindViewById<TextView>(Resource.Id.player2rows));
			Utils.SetTextFont(FindViewById<TextView>(Resource.Id.player1level));
			Utils.SetTextFont(FindViewById<TextView>(Resource.Id.player2level));
			Utils.SetTextFont(FindViewById<TextView>(Resource.Id.score1));
			Utils.SetTextFont(FindViewById<TextView>(Resource.Id.score2));
			Utils.SetTextFont(FindViewById<TextView>(Resource.Id.rows1));
			Utils.SetTextFont(FindViewById<TextView>(Resource.Id.rows2));
			Utils.SetTextFont(FindViewById<TextView>(Resource.Id.level1));
			Utils.SetTextFont(FindViewById<TextView>(Resource.Id.level2));

			// Change the size of the components to center them
			GridView myGrid = FindViewById<GridView>(Resource.Id.PlayerGridView);
			Point size = GridView.CalculateUseSize(myGrid.MeasuredWidth, myGrid.MeasuredHeight);
			int difference = (myGrid.MeasuredWidth - size.X) / 2;
			myGrid.LayoutParameters = new LinearLayout.LayoutParams(size.X, size.Y);

			// Change the size of the components to center them
			NextPieceView nextPieceView = FindViewById<NextPieceView>(Resource.Id.NextPieceView);
			nextPieceView.SetPlayer(_player1);
			nextPieceView.LayoutParameters = new RelativeLayout.LayoutParams(nextPieceView.MeasuredWidth + difference, 
				nextPieceView.MeasuredWidth + difference);

			// Set the buttons
			Utils.SetArrowButton(FindViewById<ButtonStroked>(Resource.Id.buttonMoveLeft), TetrisColor.Green, difference);
			Utils.SetArrowButton(FindViewById<ButtonStroked>(Resource.Id.buttonMoveRight), TetrisColor.Green, difference);
			Utils.SetArrowButton(FindViewById<ButtonStroked>(Resource.Id.buttonTurnLeft), TetrisColor.Cyan, difference);
			Utils.SetArrowButton(FindViewById<ButtonStroked>(Resource.Id.buttonTurnRight), TetrisColor.Cyan, difference);
			Utils.SetArrowButton(FindViewById<ButtonStroked>(Resource.Id.buttonMoveDown), TetrisColor.Red, difference);
			Utils.SetArrowButton(FindViewById<ButtonStroked>(Resource.Id.buttonMoveFoot), TetrisColor.Red, difference);

			// Reset the weight sum of the layout containing the grid and the buttons because
			// now we have set the size it is useless and it will allow the layout to actualize
			LinearLayout gameLayout = FindViewById<LinearLayout>(Resource.Id.gridButtonLayout);
			gameLayout.WeightSum = 0;
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


