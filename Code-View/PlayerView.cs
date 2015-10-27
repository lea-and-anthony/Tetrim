using System;
using System.Collections.Generic;
using Android.Widget;

namespace Tetris
{
	public class PlayerView
	{
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		public Player m_player { get; private set; }
		public GridView m_gridView { get; private set; }
		public List<PieceView> m_proposedPiecesView { get; private set; }

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public PlayerView (Player player)
		{
			m_player = player;
			m_gridView = new GridView(m_player.m_grid);
		}

		//--------------------------------------------------------------
		// METHODES
		//--------------------------------------------------------------
		public void Update()
		{
			m_gridView.Update();
		}

		public void Draw(TextView playerName, TextView playerScore, TextView playerLevel, TextView playerRows)
		{
			playerName.Text = m_player.m_name;
			playerScore.Text = m_player.m_score.ToString();
			playerLevel.Text = m_player.m_level.ToString();
			playerRows.Text = m_player.m_removedRows.ToString();
		}
	}
}

