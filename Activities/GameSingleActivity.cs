﻿using System;
using System.Timers;

using Android.App;
using Android.Content;
using Android.OS;

namespace Tetrim
{
	[Activity(Label = "Tetrim", Icon = "@drawable/icon", Theme = "@android:style/Theme.NoTitleBar.Fullscreen", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
	public class GameSingleActivity : GameActivity
	{
		//--------------------------------------------------------------
		// EVENT CATCHING METHODES
		//--------------------------------------------------------------
		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			// Set our view from the "main" layout resource
			SetContentView(Resource.Layout.GameMulti);

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
				Intent intent = Utils.CreateGameOverDialogSingle(this, Resources, _player1._score);
				User.Instance.AddHighScore(_player1._score);
				StartActivity(intent);
			}

			// Display of the current model
			FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
		}
		//--------------------------------------------------------------
		// PROTECTED METHODES
		//--------------------------------------------------------------
		// Pause the game and display a pop-up
		protected override int pauseGame(bool requestFromUser)
		{
			_gameTimer.Stop();

			Utils.PopUpEndEvent += resumeGame;
			// TODO : change dialog
			Utils.ShowAlert(Resource.String.Pause_title, Resource.String.Pause, this);

			return 0;
		}

		// Resume the game by restarting the timer
		protected override void resumeGame()
		{
			_gameTimer.AutoReset = true;
			_gameTimer.Interval = getTimerLapse();
			_gameTimer.Start();
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
				_gameTimer.Stop();

				// Display of the current model
				FindViewById(Resource.Id.PlayerGridView).PostInvalidate();
				_player1View.Draw();

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

				_gameTimer.Start();
			}
		}
	}
}
