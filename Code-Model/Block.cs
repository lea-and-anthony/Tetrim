using System;

namespace Tetris
{
	public class Block
	{
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		public int m_x { get; set; }
		public int m_y { get; set; }
		public TetrisColor m_color { get; set; }

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public Block (int x, int y, TetrisColor color)
		{
			m_x = x;
			m_y = y;
			m_color = color;
		}

		public Block (Block block)
		{
			m_x = block.m_x;
			m_y = block.m_y;
			m_color = block.m_color;
		}

		//--------------------------------------------------------------
		// METHODES
		//--------------------------------------------------------------
		public void MoveDown ()
		{
			m_y --;
		}
	}
}

