using System;
using Android.Widget;

namespace Tetrim
{
	public class PlayerView
	{
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		private Player _player; // Instance of the player to display
		public GridView _gridView = null; // View of the player's grid

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public PlayerView (Player player)
		{
			// Associate the instance
			_player = player;
		}

		//--------------------------------------------------------------
		// METHODES
		//--------------------------------------------------------------
		public void Draw(TextView playerName, TextView playerScore, TextView playerLevel, TextView playerRows)
		{
			// Write the text
			playerName.Text = _player._name;
			playerScore.Text = _player._score.ToString();
			playerLevel.Text = _player._level.ToString();
			playerRows.Text = _player._removedRows.ToString();
		}

		public void Update()
		{
			// Update the grid
			if(_gridView != null)
				_gridView.Update();
		}
	}
}

