using System;
using System.Timers;

namespace Tetris
{
	public class Game
	{
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		public Player m_player1 { get; private set; }
		public Player m_player2 { get; private set; }

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public Game ()
		{
			m_player1 = new Player();
			m_player2 = new Player();
		}

		public Game (string name_player1, string name_player2)
		{
			m_player1 = new Player(name_player1);
			m_player2 = new Player(name_player2);
		}

		//--------------------------------------------------------------
		// METHODES
		//--------------------------------------------------------------

		public void MoveLeft()
		{
			m_player1.MoveLeft();
		}

		public void MoveRight()
		{
			m_player1.MoveRight();
		}

		public void MoveDown()
		{
			m_player1.MoveDown();
		}

		public void MoveBottom()
		{
			m_player1.MoveBottom ();
		}

		public void TurnLeft()
		{
			m_player1.TurnLeft();
		}

		public void TurnRight()
		{
			m_player1.TurnRight();
		}
	}
}

