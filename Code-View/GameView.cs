using System;

namespace Tetrim
{
	public class GameView
	{
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		public PlayerView m_player1View { get; private set; } // View of the player 1
		public PlayerView m_player2View { get; private set; } // View of the player 2

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public GameView (Game game)
		{
			// Associate the instances
			m_player1View = new PlayerView(game._player1);
			m_player2View = new PlayerView(game._player2);
   		}
	}
}

