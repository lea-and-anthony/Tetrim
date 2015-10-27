using System;
using System.Collections.Generic;

using Android.Graphics;

namespace Tetris
{
	public class PieceView
	{
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		private Piece _piece; // Instance of the piece to display
		private bool _isShadow; // true if the piece is a shadow at the bottom of the grid
		private BlockView[] _blocksView; // Array of the BlockViews constituting the piece

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public PieceView (Piece piece, bool isShadow)
		{
			// Associate the instances
			_piece = piece;
			_isShadow = isShadow;

			// Create the associated BlockViews
			_blocksView = new BlockView[Constants.BlockPerPiece];
			for (uint i = 0 ; i < _piece.m_blocks.GetLength(0) ; i++)
			{
				_blocksView[i] = new BlockView(_piece.m_blocks[i], isShadow);
			}
		}

		//--------------------------------------------------------------
		// PUBLIC METHODES
		//--------------------------------------------------------------
		public void Draw (Canvas canvas, float blockSize, Dictionary<TetrisColor, Bitmap> blockImages)
		{
			// Draw each block of the piece
			for (uint i = 0 ; i < _piece.m_blocks.GetLength(0) ; i++)
			{
				_blocksView[i].Draw(canvas, blockSize, blockImages);
			}
		}

		public void Update(Piece piece, bool isShadow)
		{
			// Associate the new instances
			_piece = piece;
			_isShadow = isShadow;

			// Update the BlockViews
			for (uint i = 0 ; i < _piece.m_blocks.GetLength(0) ; i++)
			{
				_blocksView[i].Update(_piece.m_blocks[i], isShadow);
			}
		}
	}
}

