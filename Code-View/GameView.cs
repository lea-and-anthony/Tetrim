using System;

namespace Tetrim
{
	public class GameView
	{
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		public PlayerView _player1View { get; private set; } // View of the player 1
		public PlayerView _player2View { get; private set; } // View of the player 2

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public GameView (Game game)
		{
			// Associate the instances
			_player1View = new PlayerView(game._player1);
			_player2View = new PlayerView(game._player2);
   		}
	}
}

