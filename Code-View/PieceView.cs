using System.Collections.Generic;

using Android.Graphics;

namespace Tetrim
{
	public class PieceView
	{
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		public Piece _piece {get; private set;} // Instance of the piece to display
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
			for (uint i = 0 ; i < Constants.BlockPerPiece ; i++)
			{
				_blocksView[i] = new BlockView(_piece._blocks[i], isShadow);
			}
		}

		//--------------------------------------------------------------
		// PUBLIC METHODES
		//--------------------------------------------------------------
		public void Draw (Canvas canvas, float blockSize, Dictionary<TetrisColor, Bitmap> blockImages, float xOffset, float yOffset)
		{
			Draw(canvas, blockSize, blockImages, xOffset, yOffset, 0);
		}

		public void Draw (Canvas canvas, float blockSize, Dictionary<TetrisColor, Bitmap> blockImages, float xOffset, float yOffset, int height)
		{
			// Draw each block of the piece
			for (uint i = 0 ; i < Constants.BlockPerPiece ; i++)
			{
				_blocksView[i].Draw(canvas, blockSize, blockImages, xOffset, yOffset, height);
			}
		}

		public void Update(Piece piece, bool isShadow)
		{
			// Associate the new instances
			_piece = piece;
			_isShadow = isShadow;

			// Update the BlockViews
			for (uint i = 0; i < Constants.BlockPerPiece; i++)
			{
				_blocksView[i].Update(_piece._blocks[i], isShadow);
			}
		}

		// set xSize and ySize to the x and y size of the block (this function suppose that the piece is in 0,0)
		public void GetDrawnSize(float blockSize, ref float xSize, ref float ySize)
		{
			float currentXSize = 0;
			float currentYSize = 0;
			xSize = 0;
			ySize = 0;
			for (uint i = 0; i < Constants.BlockPerPiece; i++)
			{
				_blocksView[i].GetDrawnSize(blockSize, ref currentXSize, ref currentYSize);

				if(currentXSize > xSize)
					xSize = currentXSize;

				if(currentYSize > ySize)
					ySize = currentYSize;
			}
		}
	}
}

