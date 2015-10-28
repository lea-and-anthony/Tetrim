using System;

namespace Tetrim
{
	public class Grid
	{
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		public Block[,] _map { get; private set; }
		public Piece _fallingPiece { get; private set; }
		public Piece _shadowPiece { get; private set; }
		public Piece _nextPiece { get; private set; }
		public bool _isNextPieceModified { get; private set; }

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public Grid ()
		{
			_map = new Block[Constants.GridSizeX, Constants.GridSizeY];
			_fallingPiece = new Piece (this);
			_shadowPiece = new Piece (this, _fallingPiece);
			_nextPiece = new Piece(0,0);
			_isNextPieceModified = false;
			UpdateShadowPiece();
		}

		//--------------------------------------------------------------
		// METHODES
		//--------------------------------------------------------------
		public bool isBlock(int x, int y)
		{
			if (_map[x,y] != null)
			{
				return true;
			}
			return false;
		}

		public bool isOutOfGrid(int x, int y)
		{
			if(x < 0 || x > Constants.GridSizeXmax || y < 0 || y > Constants.GridSizeYmax)
			{
				return true;
			}
			return false;
		}

		public Piece generatePiece()
		{
			return new Piece(this);
		}

		private bool isRowFull (int yRow)
		{
			for (int x = 0; x <= Constants.GridSizeXmax; x++)
			{
				if (_map [x, yRow] == null)
				{
					return false;
				}
			}
			return true;
		}

		private void removeOneRow (int yRow)
		{
			for (int y = yRow + 1 ; y <= Constants.GridSizeYmax ; y++)
			{
				for (int x = 0 ; x <= Constants.GridSizeXmax; x++)
				{
					if(_map[x, y] != null)
						_map[x, y].MoveDown();
					_map[x, y-1] = _map[x, y];
				}
			}
			for (int x = 0 ; x <= Constants.GridSizeXmax ; x++)
			{
				_map [x, Constants.GridSizeYmax] = null;
			}
		}

		public int RemoveFullRows (int startY, int endY)
		{
			int nbRemovedRows = 0;
			for (int i = startY ; i <= endY ; i++)
			{
				if (isRowFull (i))
				{
					removeOneRow (i);
					nbRemovedRows++;
					i--;
				}
			}
			return nbRemovedRows;
		}

		public void AddPieceToMap (Player player)
		{
			int minY = _fallingPiece._blocks[0]._y;
			int maxY = _fallingPiece._blocks[0]._y;
			for (int i = 0; i < Constants.BlockPerPiece; i++)
			{
				_map [_fallingPiece._blocks[i]._x, _fallingPiece._blocks[i]._y] = _fallingPiece._blocks[i];
				if (_fallingPiece._blocks[i]._y < minY)
				{
					minY = _fallingPiece._blocks[i]._y;
				}
				else if (_fallingPiece._blocks[i]._y > maxY)
				{
					maxY = _fallingPiece._blocks[i]._y;
				}
			}
			int nbRemovedRows = RemoveFullRows(minY, maxY);
			if (nbRemovedRows != 0)
			{
				player.UpdatePlayerRemoveRow(nbRemovedRows);
			}

			// We create next the new piece
			_fallingPiece = new Piece (this, _nextPiece._shape);
			_shadowPiece = new Piece (this, _fallingPiece);
			_nextPiece = new Piece (0, 0);
			_isNextPieceModified = false;
			UpdateShadowPiece();
		}
			

		public void UpdateShadowPiece()
		{
			_shadowPiece.UpdateShadowPiece(_fallingPiece);
			_shadowPiece.MoveBottom(this);
		}

		public bool isGameOver ()
		{
			for (int i = Constants.GridSizeXmin ; i < Constants.GridSizeXmax ; i++)
			{
				if (_map[i, Constants.GridSizeYmax] != null)
				{
					return true;
				}
			}
			return false;
		}

		// Move the piece down and if it is at the bottom, create a new piece
		public bool MovePieceDown (Player player)
		{
			if (!_fallingPiece.MoveDown (this))
			{
				AddPieceToMap (player);

				return false;
			}
			return true;
		}

		public void addBlock(int x, int y, TetrisColor color)
		{
			_map[x,y] = new Block(x, y, color);
		}

		public void addPiece(Shape shape, uint angle, Grid grid)
		{
			_fallingPiece = new Piece(shape, angle, grid);
			UpdateShadowPiece();
		}

		public void MoveLeft()
		{
			_fallingPiece.MoveLeft(this);
			UpdateShadowPiece();
		}

		public void MoveRight()
		{
			_fallingPiece.MoveRight(this);
			UpdateShadowPiece();
		}

		public void MoveDown()
		{
			_fallingPiece.MoveDown(this);
		}

		public int MoveBottom()
		{
			return _fallingPiece.MoveBottom (this);
		}

		public void TurnLeft()
		{
			_fallingPiece.TurnLeft(this);
			UpdateShadowPiece();
		}

		public void TurnRight()
		{
			_fallingPiece.TurnRight(this);
			UpdateShadowPiece();
		}

		// Create an array of byte representing the position of the first block, the rotation of the piece
		// and the shape (the color depends on the shape)
		public byte[] getMessagePiece(byte[] bytes, uint begin)
		{
			return _fallingPiece.getMessage(bytes, begin);
		}

		// Create an array of byte representing the actual grid and the piece
		public byte[] getMessageGrid(byte[] bytesMessage, uint begin)
		{
			bytesMessage = getMessagePiece(bytesMessage, begin + Constants.GridSizeX*Constants.GridSizeY);

			for (uint y = 0; y < Constants.GridSizeY; y++)
			{
				for (uint x = 0; x < Constants.GridSizeX; x++)
				{
					if(_map[x, y] != null)
						bytesMessage[begin + Constants.GridSizeX*y + x] = (byte) _map[x, y]._color;
					else
						bytesMessage[begin + Constants.GridSizeX*y + x] = (byte) TetrisColor.ColorMax;
				}
			}

			return bytesMessage;
		}

		// Interpret the message to modify the model and the display
		public void interpretGrid(byte[] bytesMessage, uint begin)
		{
			for (int y = 0; y < Constants.GridSizeY; y++)
			{
				for (int x = 0; x < Constants.GridSizeX; x++)
				{
					if(bytesMessage[begin + Constants.GridSizeX*y + x] == (byte) TetrisColor.ColorMax)
						_map[x, y] = null;
					else if(_map[x, y] != null)
						_map[x, y]._color = (TetrisColor) bytesMessage[begin + Constants.GridSizeX*y + x];
					else 
						_map[x, y] = new Block(x, y, (TetrisColor) bytesMessage[begin + Constants.GridSizeX*y + x]);
				}
			}
		}

		// Interpret the message to place the piece at the good position
		public void interpretPiece(byte[] bytesMessage, uint begin)
		{
			_fallingPiece.placePiece(bytesMessage, begin);
			UpdateShadowPiece();
		}

		// Interpret the message of the next shape to use
		public void interpretNextPiece(byte[] bytesMessage, uint begin)
		{
			_nextPiece.ChangeShape((Shape) bytesMessage[begin], 0, 0);
			_isNextPieceModified = true;
		}
	}
}

