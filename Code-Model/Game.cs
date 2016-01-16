using System;
using System.Timers;

namespace Tetrim
{
	public class Game
	{
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		public Player _player1 { get; private set; }
		public Player _player2 { get; private set; }

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public Game ()
		{
			_player1 = new Player();
			_player2 = new Player();
		}

		public Game (string name_player1, string name_player2)
		{
			_player1 = new Player(name_player1);
			_player2 = new Player(name_player2);
		}

		//--------------------------------------------------------------
		// METHODES
		//--------------------------------------------------------------

		public void MoveLeft()
		{
			_player1.MoveLeft();
		}

		public void MoveRight()
		{
			_player1.MoveRight();
		}

		public bool MoveDown()
		{
			return _player1.MoveDown();
		}

		public bool MoveBottom()
		{
			return _player1.MoveBottom ();
		}

		public void TurnLeft()
		{
			_player1.TurnLeft();
		}

		public void TurnRight()
		{
			_player1.TurnRight();
		}
	}
}

