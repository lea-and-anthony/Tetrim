using System;

using Android.Graphics;

namespace Tetris
{
	public class PieceView
	{
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		public Piece m_piece { get; private set; }
		public BlockView[] m_blocksView { get; private set; }

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public PieceView (Piece piece, bool isShadow)
		{
			m_piece = piece;
			m_blocksView = new BlockView[Constants.BlockPerPiece];
			for (uint i = 0 ; i < m_piece.m_blocks.GetLength(0) ; i++)
			{
				m_blocksView[i] = new BlockView(m_piece.m_blocks[i],isShadow);
			}
		}

		//--------------------------------------------------------------
		// METHODES
		//--------------------------------------------------------------
		public void Draw (Canvas canvas, float blockSize, bool isHighlight = false)
		{
			for (uint i = 0 ; i < m_piece.m_blocks.GetLength(0) ; i++)
			{
				m_blocksView[i].Draw(canvas, blockSize, isHighlight);
			}
		}
	}
}

