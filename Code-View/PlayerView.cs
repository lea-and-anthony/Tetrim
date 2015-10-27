using System;
using Android.Widget;

namespace Tetris
{
	public class PlayerView
	{
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		private Player _player; // Instance of the player to display
		public GridView _gridView { get; private set; } // View of the player's grid

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public PlayerView (Player player)
		{
			// Associate the instance
			_player = player;

			// Create the associated GridView
			_gridView = new GridView(_player.m_grid);
		}

		//--------------------------------------------------------------
		// METHODES
		//--------------------------------------------------------------
		public void Draw(TextView playerName, TextView playerScore, TextView playerLevel, TextView playerRows)
		{
			// Write the text
			playerName.Text = _player.m_name;
			playerScore.Text = _player.m_score.ToString();
			playerLevel.Text = _player.m_level.ToString();
			playerRows.Text = _player.m_removedRows.ToString();
		}

		public void Update()
		{
			// Update the grid
			_gridView.Update();
		}
	}
}

