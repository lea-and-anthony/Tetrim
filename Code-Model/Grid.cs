using System;

namespace Tetris
{
	public class Grid
	{
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		public Block[,] m_map { get; private set; }
		public Piece m_fallingPiece { get; private set; }
		public Piece m_shadowPiece { get; private set; }
		public Piece m_nextPiece { get; private set; }
		public bool m_isNextPieceModified { get; private set; }

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public Grid ()
		{
			m_map = new Block[Constants.GridSizeX, Constants.GridSizeY];
			m_fallingPiece = new Piece (this);
			m_shadowPiece = new Piece (this, m_fallingPiece);
			m_nextPiece = new Piece(0,0);
			m_isNextPieceModified = false;
			UpdateShadowPiece();
		}

		//--------------------------------------------------------------
		// METHODES
		//--------------------------------------------------------------
		public bool isBlock(int x, int y)
		{
			if (m_map[x,y] != null)
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
				if (m_map [x, yRow] == null)
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
					if(m_map[x, y] != null)
						m_map[x, y].MoveDown();
					m_map[x, y-1] = m_map[x, y];
				}
			}
			for (int x = 0 ; x <= Constants.GridSizeXmax ; x++)
			{
				m_map [x, Constants.GridSizeYmax] = null;
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
			int minY = m_fallingPiece.m_blocks[0].m_y;
			int maxY = m_fallingPiece.m_blocks[0].m_y;
			for (int i = 0; i < Constants.BlockPerPiece; i++)
			{
				m_map [m_fallingPiece.m_blocks[i].m_x, m_fallingPiece.m_blocks[i].m_y] = m_fallingPiece.m_blocks[i];
				if (m_fallingPiece.m_blocks[i].m_y < minY)
				{
					minY = m_fallingPiece.m_blocks[i].m_y;
				}
				else if (m_fallingPiece.m_blocks[i].m_y > maxY)
				{
					maxY = m_fallingPiece.m_blocks[i].m_y;
				}
			}
			int nbRemovedRows = RemoveFullRows(minY, maxY);
			if (nbRemovedRows != 0)
			{
				player.UpdatePlayerRemoveRow(nbRemovedRows);
			}

			// We create next the new piece
			m_fallingPiece = new Piece (this, m_nextPiece.m_shape);
			m_shadowPiece = new Piece (this, m_fallingPiece);
			m_nextPiece = new Piece (0, 0);
			m_isNextPieceModified = false;
			UpdateShadowPiece();
		}
			

		public void UpdateShadowPiece()
		{
			m_shadowPiece.UpdateShadowPiece(m_fallingPiece);
			m_shadowPiece.MoveBottom(this);
		}

		public bool isGameOver ()
		{
			for (int i = Constants.GridSizeXmin ; i < Constants.GridSizeXmax ; i++)
			{
				if (m_map[i, Constants.GridSizeYmax] != null)
				{
					return true;
				}
			}
			return false;
		}

		// Move the piece down and if it is at the bottom, create a new piece
		public bool MovePieceDown (Player player)
		{
			if (!m_fallingPiece.MoveDown (this))
			{
				AddPieceToMap (player);

				return false;
			}
			return true;
		}

		public void addBlock(int x, int y, TetrisColor color)
		{
			m_map[x,y] = new Block(x, y, color);
		}

		public void addPiece(Shape shape, uint angle, Grid grid)
		{
			m_fallingPiece = new Piece(shape, angle, grid);
			UpdateShadowPiece();
		}

		public void MoveLeft()
		{
			m_fallingPiece.MoveLeft(this);
			UpdateShadowPiece();
		}

		public void MoveRight()
		{
			m_fallingPiece.MoveRight(this);
			UpdateShadowPiece();
		}

		public void MoveDown()
		{
			m_fallingPiece.MoveDown(this);
		}

		public int MoveBottom()
		{
			return m_fallingPiece.MoveBottom (this);
		}

		public void TurnLeft()
		{
			m_fallingPiece.TurnLeft(this);
			UpdateShadowPiece();
		}

		public void TurnRight()
		{
			m_fallingPiece.TurnRight(this);
			UpdateShadowPiece();
		}

		// Create an array of byte representing the position of the first block, the rotation of the piece
		// and the shape (the color depends on the shape)
		public byte[] getMessagePiece(byte[] bytes, uint begin)
		{
			return m_fallingPiece.getMessage(bytes, begin);
		}

		// Create an array of byte representing the actual grid and the piece
		public byte[] getMessageGrid(byte[] bytesMessage, uint begin)
		{
			bytesMessage = getMessagePiece(bytesMessage, begin + Constants.GridSizeX*Constants.GridSizeY);

			for (uint y = 0; y < Constants.GridSizeY; y++)
			{
				for (uint x = 0; x < Constants.GridSizeX; x++)
				{
					if(m_map[x, y] != null)
						bytesMessage[begin + Constants.GridSizeX*y + x] = (byte) m_map[x, y].m_color;
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
						m_map[x, y] = null;
					else if(m_map[x, y] != null)
						m_map[x, y].m_color = (TetrisColor) bytesMessage[begin + Constants.GridSizeX*y + x];
					else 
						m_map[x, y] = new Block(x, y, (TetrisColor) bytesMessage[begin + Constants.GridSizeX*y + x]);
				}
			}
		}

		// Interpret the message to place the piece at the good position
		public void interpretPiece(byte[] bytesMessage, uint begin)
		{
			m_fallingPiece.placePiece(bytesMessage, begin);
			UpdateShadowPiece();
		}

		// Interpret the message of the next shape to use
		public void interpretNextPiece(byte[] bytesMessage, uint begin)
		{
			m_nextPiece.ChangeShape((Shape) bytesMessage[begin], 0, 0);
			m_isNextPieceModified = true;
		}
	}
}

