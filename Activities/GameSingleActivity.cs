﻿using System;
using System.Timers;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace Tetrim
{
	[Activity(ScreenOrientation = ScreenOrientation.Portrait)]
	public class GameSingleActivity : GameActivity
	{
		//--------------------------------------------------------------
		// EVENT CATCHING METHODS
		//--------------------------------------------------------------
		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			// Set our view from the "main" layout resource
			SetContentView(Resource.Layout.GameSingle);

			initGame();

			startGame();
		}

		protected override void OnTimerElapsed(object source, ElapsedEventArgs e)
		{
			if(!_player1._grid.MovePieceDown(_player1))
			{
				_player1View.Update();
			}

			RunOnUiThread(_player1View.Draw);

			if (_player1._grid.isGameOver())
			{
				_gameTimer.Stop();
				//Utils.PopUpEndEvent += endGame;
				Intent intent = UtilsDialog.CreateGameOverDialogSingle(this, _player1._score);
				User.Instance.AddHighScore(_player1._score);
				StartActivity(intent);
			}

			//	Speed
			changeSpeedIfNecessary();

			// Display of the current model
			actualizeView();
		}
		//--------------------------------------------------------------
		// PUBLIC METHODS
		//--------------------------------------------------------------
		// Resume the game by restarting the timer
		public override void ResumeGame()
		{
			_gameTimer.AutoReset = true;
			_gameTimer.Interval = getTimerLapse();
			_gameTimer.Start();
		}
			
		public override void NewGame()
		{
			MenuActivity.startOnePlayerGame(this);
			Finish();
		}

		//--------------------------------------------------------------
		// PROTECTED METHODS
		//--------------------------------------------------------------
		// Pause the game and display a pop-up
		protected override int pauseGame(bool requestFromUser)
		{
			_gameTimer.Stop();

			Intent intent = UtilsDialog.CreatePauseGameDialog(this);
			StartActivity(intent);

			return 0;
		}

		protected override void moveLeftButtonPressed(object sender, EventArgs e)
		{
			_player1.MoveLeft();
			// Display of the current model
			FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
		}

		protected override void moveRightButtonPressed(object sender, EventArgs e)
		{
			_player1.MoveRight();
			// Display of the current model
			FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
		}

		protected override void turnLeftButtonPressed(object sender, EventArgs e)
		{
			_player1.TurnLeft();
			// Display of the current model
			FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
		}

		protected override void turnRightButtonPressed(object sender, EventArgs e)
		{
			_player1.TurnRight();
			// Display of the current model
			FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
		}

		protected override void moveDownButtonPressed(object sender, EventArgs e)
		{
			if(_player1.MoveDown())
			{
				// the timer is stop and restart in the touch event
				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
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

				_gameTimer.Start();
			}
		}
	}
}

