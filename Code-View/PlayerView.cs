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
		private TextView _playerName = null;
		private TextView _playerScore = null;
		private TextView _playerLevel = null;
		private TextView _playerRows = null;
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
		// All the parameters must be different from null
		public void SetViews(TextView playerName, TextView playerScore, TextView playerLevel, TextView playerRows)
		{
			_playerName = playerName;
			_playerName.Text = _player._name;
			_playerScore = playerScore;
			_playerScore.Text = _player._score.ToString();
			_playerLevel = playerLevel;
			_playerLevel.Text = _player._level.ToString();
			_playerRows = playerRows;
			_playerRows.Text = _player._removedRows.ToString();
		}

		public void Draw()
		{
			// Only need to test for one of the 4 because we set them together
			if(_playerName != null)
			{
				// Write the text
				_playerScore.Text = _player._score.ToString();
				_playerLevel.Text = _player._level.ToString();
				_playerRows.Text = _player._removedRows.ToString();
			}
		}

		public void Update()
		{
			// Update the grid
			if(_gridView != null)
				_gridView.Update();
		}
	}
}

