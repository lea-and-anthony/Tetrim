using System;
using Android.Views;

namespace Tetris
{
	public class GameView
	{
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		public PlayerView m_player1View { get; private set; }
		public PlayerView m_player2View { get; private set; }

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public GameView (Game game)
		{
			m_player1View = new PlayerView(game.m_player1);
			m_player2View = new PlayerView(game.m_player2);
		}

		//--------------------------------------------------------------
		// METHODES
		//--------------------------------------------------------------

	}
}

