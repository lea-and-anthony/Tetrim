using System;

namespace Tetris
{

	public class Piece
	{
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		public Shape m_shape { get; private set; }
		public TetrisColor m_color { get; private set; }
		public uint m_angle { get; private set; }
		public Block[] m_blocks { get; private set; }

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public Piece (Grid grid) : this (pickShape(), pickAngle(), grid)
		{
		}

		public Piece (Grid grid, Shape shape) : this (shape, pickAngle(), grid)
		{
		}

		public Piece (Grid grid, Piece piece)
		{
			m_shape = piece.m_shape;
			m_color = piece.m_color;
			m_blocks = new Block[Constants.BlockPerPiece];
			m_angle = piece.m_angle;
			for (uint i = 0 ; i < Constants.BlockPerPiece ; i++)
			{
				m_blocks[i] = new Block(piece.m_blocks[i]);
			}

		}

		public Piece (Shape shape, uint angle, Grid grid)
		{
			m_shape = shape;
			m_color = pickColor (m_shape);
			m_blocks = new Block[Constants.BlockPerPiece];
			placeBlockAccordingToShape(Constants.GridSizeXmin + Constants.GridSizeXmax / 2, Constants.GridSizeYmax);
			m_angle = angle;
			turnPieceAccordingToAngle(grid);
		}

		public Piece(int x , int y)
		{
			m_shape = pickShape();
			m_color = pickColor(m_shape);
			m_blocks = new Block[Constants.BlockPerPiece];
			placeBlockAccordingToShape(x+1, y+2);
			m_angle = pickAngle();
			turnPieceAccordingToAngle(null);
		}

		public static Shape pickShape ()
		{
			return (Shape) Constants.Rand.Next(Constants.ShapeMax);
		}

		private static TetrisColor pickColor (Shape shape)
		{
			switch (shape)
			{
			case Shape.I:
				return TetrisColor.Cyan;
			case Shape.J:
				return TetrisColor.Blue;
			case Shape.L:
				return TetrisColor.Orange;
			case Shape.O:
				return TetrisColor.Yellow;
			case Shape.S:
				return TetrisColor.Green;
			case Shape.T:
				return TetrisColor.Pink;
			case Shape.Z:
				return TetrisColor.Red;
			}
			return TetrisColor.Red;
		}

		private static uint pickAngle ()
		{
			return (uint) Constants.Rand.Next(4);
		}

		//--------------------------------------------------------------
		// METHODES
		//--------------------------------------------------------------
		private void placeBlockAccordingToShape(int x, int y)
		{
			switch (m_shape)
			{
			case Shape.I:
				m_blocks [0] = new Block (x, y, m_color);
				m_blocks [1] = new Block (x - 1, y, m_color);
				m_blocks [2] = new Block (x + 1, y, m_color);
				m_blocks [3] = new Block (x + 2, y, m_color);
				break;
			case Shape.J:
				m_blocks [0] = new Block (x , y, m_color);
				m_blocks [1] = new Block (x - 1, y, m_color);
				m_blocks [2] = new Block (x + 1, y, m_color);
				m_blocks [3] = new Block (x + 1, y - 1, m_color);
				break;
			case Shape.L:
				m_blocks [0] = new Block (x , y, m_color);
				m_blocks [1] = new Block (x - 1, y, m_color);
				m_blocks [2] = new Block (x - 1, y - 1, m_color);
				m_blocks [3] = new Block (x + 1, y, m_color);
				break;
			case Shape.O:
				m_blocks [0] = new Block (x , y, m_color);
				m_blocks [1] = new Block (x + 1, y, m_color);
				m_blocks [2] = new Block (x, y - 1, m_color);
				m_blocks [3] = new Block (x + 1, y - 1, m_color);
				break;
			case Shape.S:
				m_blocks [0] = new Block (x , y, m_color);
				m_blocks [1] = new Block (x + 1, y, m_color);
				m_blocks [2] = new Block (x, y - 1, m_color);
				m_blocks [3] = new Block (x - 1, y - 1, m_color);
				break;
			case Shape.T:
				m_blocks [0] = new Block (x , y, m_color);
				m_blocks [1] = new Block (x - 1, y, m_color);
				m_blocks [2] = new Block (x + 1, y, m_color);
				m_blocks [3] = new Block (x, y - 1, m_color);
				break;
			case Shape.Z:
				m_blocks [0] = new Block (x , y, m_color);
				m_blocks [1] = new Block (x - 1, y, m_color);
				m_blocks [2] = new Block (x, y - 1, m_color);
				m_blocks [3] = new Block (x + 1, y - 1, m_color);
				break;
			}
		}

		private void turnPieceAccordingToAngle(Grid grid)
		{
			for (uint i = m_angle ; i > 0; i--)
			{
				if(!TurnLeft(grid))
					m_angle--;
			}
		}

		public bool TurnLeft(Grid grid)
		{
			bool canRotate = true;
			int[] newX = new int[Constants.BlockPerPiece];
			int[] newY = new int[Constants.BlockPerPiece];

			// Calculate the new coordinates
			for (uint i = 0; i < Constants.BlockPerPiece; i++)
			{
				// Change the point of reference : x = x - x0 and y = y - y0
				// Rotate : x = - y and y = x
				// Go back to the original point of reference : x = x + x0 and y = y + y0
				newX[i] = -m_blocks[i].m_y + m_blocks[0].m_y + m_blocks[0].m_x;
				newY[i] =  m_blocks[i].m_x - m_blocks[0].m_x + m_blocks[0].m_y;
			}

			// Check if the piece can be rotated
			canRotate = pieceCanMove(grid, newX, newY);

			// Rotate the piece if it can be rotated
			if(canRotate)
			{
				for (uint i = 0 ; i < Constants.BlockPerPiece; i++)
				{
					m_blocks[i].m_x = newX[i];
					m_blocks[i].m_y = newY[i];
				}
				m_angle = (m_angle+1) >= 4 ? m_angle - 3 : m_angle+1;
			}

			return canRotate;
		}

		public bool TurnRight(Grid grid)
		{
			bool canRotate = true;
			int[] newX = new int[Constants.BlockPerPiece];
			int[] newY = new int[Constants.BlockPerPiece];

			// Calculate the new coordinates
			for (uint i = 0; i < Constants.BlockPerPiece; i++)
			{
				// Change the point of reference : x = x - x0 and y = y - y0
				// Rotate : x = y and y = - x
				// Go back to the original point of reference : x = x + x0 and y = y + y0
				newX[i] =  m_blocks[i].m_y - m_blocks[0].m_y + m_blocks[0].m_x;
				newY[i] = -m_blocks[i].m_x + m_blocks[0].m_x + m_blocks[0].m_y;
			}

			// Check if the piece can be rotated
			canRotate = pieceCanMove(grid, newX, newY);

			// Rotate the piece if it can be rotated
			if(canRotate)
			{
				for (uint i = 0 ; canRotate && i < Constants.BlockPerPiece; i++)
				{
					m_blocks[i].m_x = newX[i];
					m_blocks[i].m_y = newY[i];
				}
				m_angle = (m_angle == 0) ? (m_angle + 3) : m_angle-1;
			}

			return canRotate;
		}

		public bool MoveLeft(Grid grid)
		{
			bool canMove = true;
			int[] newX = new int[Constants.BlockPerPiece];

			// Calculate the new coordinates
			for (uint i = 0; i < Constants.BlockPerPiece ; i++)
			{
				newX[i] =  m_blocks[i].m_x - 1 ;
			}
			// Check if the piece can be moved
			for (uint i = 0 ; i < Constants.BlockPerPiece ; i++)
			{
				if (grid.isOutOfGrid(newX[i], m_blocks[i].m_y) || grid.isBlock(newX[i], m_blocks[i].m_y))
				{
					canMove = false;
				}
			}
			// Rotate the piece if it can be rotated
			for (uint i = 0 ; canMove && i < Constants.BlockPerPiece ; i++)
			{
				m_blocks[i].m_x = newX[i];
			}
			return canMove;
		}

		public bool MoveRight(Grid grid)
		{
			bool canMove = true;
			int[] newX = new int[Constants.BlockPerPiece];

			// Calculate the new coordinates
			for (uint i = 0; i < Constants.BlockPerPiece ; i++)
			{
				newX[i] =  m_blocks[i].m_x + 1 ;
			}
			// Check if the piece can be moved
			for (uint i = 0 ; i < Constants.BlockPerPiece ; i++)
			{
				if (grid.isOutOfGrid(newX[i], m_blocks[i].m_y) || grid.isBlock(newX[i], m_blocks[i].m_y))
				{
					canMove = false;
				}
			}
			// Rotate the piece if it can be rotated
			for (uint i = 0 ; canMove && i < Constants.BlockPerPiece ; i++)
			{
				m_blocks[i].m_x = newX[i];
			}
			return canMove;
		}

		public bool MoveDown(Grid grid)
		{
			bool canMove = true;
			int[] newY = new int[Constants.BlockPerPiece];

			// Calculate the new coordinates
			for (uint i = 0; i < Constants.BlockPerPiece; i++)
			{
				newY[i] =  m_blocks[i].m_y - 1 ;
			}
			// Check if the piece can be moved
			for (uint i = 0 ; i < Constants.BlockPerPiece ; i++)
			{
				if (grid.isOutOfGrid(m_blocks[i].m_x, newY[i]) || grid.isBlock(m_blocks[i].m_x, newY[i]))
				{
					canMove = false;
				}
			}
			// Rotate the piece if it can be rotated
			for (uint i = 0 ; canMove && i < Constants.BlockPerPiece ; i++)
			{
				m_blocks[i].m_y = newY[i];
			}
			return canMove;
		}

		public int MoveBottom(Grid grid)
		{
			int nbMoveDown = 0;
			while (this.MoveDown(grid))
			{
				nbMoveDown++;
			}
			return nbMoveDown;
		}

		public void UpdateShadowPiece(Piece piece)
		{
			m_color = piece.m_color;
			m_shape = piece.m_shape;
			m_angle = piece.m_angle;
			for (uint i = 0 ; i < Constants.BlockPerPiece ; i++)
			{
				m_blocks[i].m_x = piece.m_blocks[i].m_x;
				m_blocks[i].m_y = piece.m_blocks[i].m_y;
				m_blocks[i].m_color = piece.m_color;
			}
		}

		private bool isOutOfGrid(int x, int y)
		{
			if(x < 0 || x > Constants.GridSizeXmax || y < 0 || y > Constants.GridSizeYmax)
			{
				return true;
			}
			return false;
		}

		private bool isCollision(Block[,] map, int x, int y)
		{
			if(map[x,y] != null)
			{
				return true;
			}
			return false;
		}

		public void placePiece(byte[] bytesMessage, uint begin)
		{
			m_shape = (Shape) bytesMessage[begin + 3];
			m_color = pickColor(m_shape);
			m_angle = (uint) bytesMessage[begin + 2];
			placeBlockAccordingToShape((int) bytesMessage[begin], (int) bytesMessage[begin + 1]);
			turnPieceAccordingToAngle(null);
		}

		private bool pieceCanMove(Grid grid, int[] newX, int[] newY)
		{
			bool canRotate = true;
			if(grid != null)
			{
				// We are going to make 3 iterations to find a position that is good
				// First with the position already calculated, then with a shift to the right
				// and finally with a shift to the left
				int iteration = 0;
				do
				{
					if(iteration == 1)
					{
						// We shift to the left
						for (uint i = 0 ; i < Constants.BlockPerPiece; i++)
						{
							newX[i]--;
						}
						canRotate = true;
					}
					else if(iteration == 2)
					{
						// We shift to the right
						for (uint i = 0 ; i < Constants.BlockPerPiece; i++)
						{
							newX[i] += 2;
						}
						canRotate = true;
					}
					for (uint i = 0 ; i < Constants.BlockPerPiece; i++)
					{
						if (grid.isOutOfGrid(newX[i], newY[i]) || grid.isBlock(newX[i], newY[i]))
						{
							canRotate = false;
						}
					}
					iteration++;
				} while(!canRotate && iteration < 3);
			}
			return canRotate;
		}

		public void ChangeShape(Shape newShape, int x, int y)
		{
			m_shape = newShape;
			m_color = pickColor(m_shape);
			placeBlockAccordingToShape(x+1, y+2);
			m_angle = 0;
		}

		public byte[] getMessage(byte[] bytes, uint begin)
		{
			// The position x and y will never be over 255 and less than 0 so we just have to cast the int to byte
			bytes[begin] = (byte) m_blocks[0].m_x;
			bytes[begin+1] = (byte) m_blocks[0].m_y;
			bytes[begin+2] = (byte) m_angle;
			bytes[begin+3] = (byte) m_shape;

			return bytes;
		}

		public byte[] getMessageNextPiece()
		{
			byte[] bytes = new byte[Constants.SizeMessageNextPiece];
			bytes[0] = Constants.IdMessageNextPiece;
			bytes[1] = (byte) m_shape;
			return bytes;
		}
	}
}

