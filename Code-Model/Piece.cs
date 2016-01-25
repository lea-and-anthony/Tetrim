using System;
using System.Collections.Generic;

namespace Tetrim
{

	public class Piece
	{
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		public Shape _shape { get; private set; }
		public TetrisColor _color { get; private set; }
		public uint _angle { get; private set; }
		public Block[] _blocks { get; private set; }
		private static Dictionary<int, Stack<Shape>> randomGenerator = new Dictionary<int, Stack<Shape>>();

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public Piece (Grid grid, int generatorKey) : this (pickShape(randomGenerator, generatorKey), pickAngle(), grid)
		{
		}

		public Piece (Grid grid, Shape shape) : this (shape, pickAngle(), grid)
		{
		}

		public Piece (Piece piece)
		{
			_shape = piece._shape;
			_color = piece._color;
			_blocks = new Block[Constants.BlockPerPiece];
			_angle = piece._angle;
			for (uint i = 0 ; i < Constants.BlockPerPiece ; i++)
			{
				_blocks[i] = new Block(piece._blocks[i]);
			}
		}

		public Piece (Shape shape, uint angle, Grid grid)
		{
			_shape = shape;
			_color = pickColor (_shape);
			_blocks = new Block[Constants.BlockPerPiece];
			placeBlockAccordingToShape((Constants.GridSizeXmin + Constants.GridSizeXmax) / 2, Constants.GridSizeYmax);
			_angle = 0;
			turnPieceAccordingToAngle(grid, angle);
			movePieceUp(Constants.GridSizeYmax);
		}

		public Piece(int x , int y, int generatorKey)
		{
			_shape = pickShape(randomGenerator, generatorKey);
			_color = pickColor(_shape);
			_blocks = new Block[Constants.BlockPerPiece];
			placeBlockAccordingToShape(x+1, y+2);
			_angle = 0;
			turnPieceAccordingToAngle(null, pickAngle());
		}

		//--------------------------------------------------------------
		// STATICS METHODES
		//--------------------------------------------------------------
		private static Shape pickShape (Dictionary<int, Stack<Shape>> randomGenerator, int generatorKey)
		{
			Stack<Shape> followingShapes = null;
			if(!randomGenerator.TryGetValue(generatorKey, out followingShapes) || followingShapes == null || followingShapes.Count == 0)
			{
				// Remove of the old key if it exists
				randomGenerator.Remove(generatorKey);

				// Create the "bag" of pieces
				followingShapes = new Stack<Shape>();
				List<Shape> pieces = new List<Shape>((IEnumerable<Shape>) Enum.GetValues(typeof(Shape)));
				while(pieces.Count > 0)
				{
					int index = Constants.Rand.Next(pieces.Count);
					followingShapes.Push(pieces[index]);
					pieces.RemoveAt(index);
				}

				randomGenerator.Add(generatorKey, followingShapes);
			}

			return followingShapes.Pop();
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
				return TetrisColor.Magenta;
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
		// PUBLICS METHODES
		//--------------------------------------------------------------
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
				newX[i] = -_blocks[i]._y + _blocks[0]._y + _blocks[0]._x;
				newY[i] =  _blocks[i]._x - _blocks[0]._x + _blocks[0]._y;
			}

			// Check if the piece can be rotated
			canRotate = pieceCanMove(grid, newX, newY);

			// Rotate the piece if it can be rotated
			if(canRotate)
			{
				for (uint i = 0 ; i < Constants.BlockPerPiece; i++)
				{
					_blocks[i]._x = newX[i];
					_blocks[i]._y = newY[i];
				}
				_angle = (_angle+1) >= 4 ? _angle - 3 : _angle+1;
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
				newX[i] =  _blocks[i]._y - _blocks[0]._y + _blocks[0]._x;
				newY[i] = -_blocks[i]._x + _blocks[0]._x + _blocks[0]._y;
			}

			// Check if the piece can be rotated
			canRotate = pieceCanMove(grid, newX, newY);

			// Rotate the piece if it can be rotated
			if(canRotate)
			{
				for (uint i = 0 ; canRotate && i < Constants.BlockPerPiece; i++)
				{
					_blocks[i]._x = newX[i];
					_blocks[i]._y = newY[i];
				}
				_angle = (_angle == 0) ? (_angle + 3) : _angle-1;
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
				newX[i] =  _blocks[i]._x - 1 ;
			}
			// Check if the piece can be moved
			for (uint i = 0 ; i < Constants.BlockPerPiece ; i++)
			{
				if (grid.isOutOfGrid(newX[i], _blocks[i]._y) || grid.isBlock(newX[i], _blocks[i]._y))
				{
					canMove = false;
				}
			}
			// Rotate the piece if it can be rotated
			for (uint i = 0 ; canMove && i < Constants.BlockPerPiece ; i++)
			{
				_blocks[i]._x = newX[i];
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
				newX[i] =  _blocks[i]._x + 1 ;
			}
			// Check if the piece can be moved
			for (uint i = 0 ; i < Constants.BlockPerPiece ; i++)
			{
				if (grid.isOutOfGrid(newX[i], _blocks[i]._y) || grid.isBlock(newX[i], _blocks[i]._y))
				{
					canMove = false;
				}
			}
			// Rotate the piece if it can be rotated
			for (uint i = 0 ; canMove && i < Constants.BlockPerPiece ; i++)
			{
				_blocks[i]._x = newX[i];
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
				newY[i] =  _blocks[i]._y - 1 ;
			}
			// Check if the piece can be moved
			for (uint i = 0 ; i < Constants.BlockPerPiece ; i++)
			{
				if (grid.isOutOfGrid(_blocks[i]._x, newY[i]) || grid.isBlock(_blocks[i]._x, newY[i]))
				{
					canMove = false;
					break;
				}
			}

			// Validate the new position if it is ok
			if(canMove)
			{
				for (uint i = 0 ; i < Constants.BlockPerPiece ; i++)
				{
					_blocks[i]._y = newY[i];
				}
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
			_color = piece._color;
			_shape = piece._shape;
			_angle = piece._angle;
			for (uint i = 0 ; i < Constants.BlockPerPiece ; i++)
			{
				_blocks[i]._x = piece._blocks[i]._x;
				_blocks[i]._y = piece._blocks[i]._y;
				_blocks[i]._color = piece._color;
			}
		}

		public void placePiece(byte[] bytesMessage, uint begin)
		{
			_shape = (Shape) bytesMessage[begin + 3];
			_color = pickColor(_shape);
			_angle = 0;
			placeBlockAccordingToShape((int) bytesMessage[begin], (int) bytesMessage[begin + 1]);
			turnPieceAccordingToAngle(null, (uint) bytesMessage[begin + 2]);
		}

		public void ChangeShape(Shape newShape, uint newAngle)
		{
			_shape = newShape;
			_color = pickColor(_shape);
			_angle = 0;
			placeBlockAccordingToShape(0, 0);
			turnPieceAccordingToAngle(null, newAngle);
		}

		public byte[] getMessage(byte[] bytes, uint begin)
		{
			// The position x and y will never be over 127 and less than 0 so we just have to cast the int to byte
			bytes[begin] = (byte) _blocks[0]._x;
			bytes[begin+1] = (byte) _blocks[0]._y;
			bytes[begin+2] = (byte) _angle;
			bytes[begin+3] = (byte) _shape;

			return bytes;
		}

		public byte[] getMessageNextPiece()
		{
			byte[] bytes = {Constants.IdMessageNextPiece, (byte) _shape, (byte) _angle};
			return bytes;
		}

		// Will place the upper-left corner of the piece in 0,0
		public void MoveToZero()
		{
			int minX = _blocks[0]._x;
			int minY = _blocks[0]._y;
			for(int i = 1; i < Constants.BlockPerPiece; i++)
			{
				if(_blocks[i]._x < minX)
					minX = _blocks[i]._x;

				if(_blocks[i]._y < minY)
					minY = _blocks[i]._y;
			}

			for(int i = 0; i < Constants.BlockPerPiece; i++)
			{
				_blocks[i]._x -= minX;
				_blocks[i]._y -= minY;
			}
		}

		//--------------------------------------------------------------
		// PRIVATE METHODES
		//--------------------------------------------------------------
		private void placeBlockAccordingToShape(int x, int y)
		{
			switch (_shape)
			{
			case Shape.I:
				_blocks [0] = new Block (x, y, _color);
				_blocks [1] = new Block (x - 1, y, _color);
				_blocks [2] = new Block (x + 1, y, _color);
				_blocks [3] = new Block (x + 2, y, _color);
				break;
			case Shape.J:
				_blocks [0] = new Block (x , y, _color);
				_blocks [1] = new Block (x - 1, y, _color);
				_blocks [2] = new Block (x + 1, y, _color);
				_blocks [3] = new Block (x + 1, y - 1, _color);
				break;
			case Shape.L:
				_blocks [0] = new Block (x , y, _color);
				_blocks [1] = new Block (x - 1, y, _color);
				_blocks [2] = new Block (x - 1, y - 1, _color);
				_blocks [3] = new Block (x + 1, y, _color);
				break;
			case Shape.O:
				_blocks [0] = new Block (x , y, _color);
				_blocks [1] = new Block (x + 1, y, _color);
				_blocks [2] = new Block (x, y - 1, _color);
				_blocks [3] = new Block (x + 1, y - 1, _color);
				break;
			case Shape.S:
				_blocks [0] = new Block (x , y, _color);
				_blocks [1] = new Block (x + 1, y, _color);
				_blocks [2] = new Block (x, y - 1, _color);
				_blocks [3] = new Block (x - 1, y - 1, _color);
				break;
			case Shape.T:
				_blocks [0] = new Block (x , y, _color);
				_blocks [1] = new Block (x - 1, y, _color);
				_blocks [2] = new Block (x + 1, y, _color);
				_blocks [3] = new Block (x, y - 1, _color);
				break;
			case Shape.Z:
				_blocks [0] = new Block (x , y, _color);
				_blocks [1] = new Block (x - 1, y, _color);
				_blocks [2] = new Block (x, y - 1, _color);
				_blocks [3] = new Block (x + 1, y - 1, _color);
				break;
			}
		}

		private void turnPieceAccordingToAngle(Grid grid, uint angle)
		{
			for (uint i = angle ; i > 0 && TurnLeft(grid); i--);
		}

		// Move the piece up so that at least one block has his y coordinate equal to maxY
		private void movePieceUp(int maxY)
		{
			int offsetUp = maxY - _blocks[0]._y;
			for(uint i = 1 ; i < Constants.BlockPerPiece; i++)
			{
				if(maxY - _blocks[i]._y < offsetUp)
					offsetUp = maxY - _blocks[i]._y;
			}

			if(offsetUp > 0)
			{
				for(uint i = 0 ; i < Constants.BlockPerPiece; i++)
				{
					_blocks[i]._y += offsetUp;
				}
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

		private bool pieceCanMove(Grid grid, int[] newX, int[] newY)
		{
			bool canRotate = true;
			if(grid != null)
			{
				// First we need to check if the piece is too high
				// In this case, we need to make it go down
				int offsetDown = 0;
				for(uint i = 0 ; i < Constants.BlockPerPiece; i++)
				{
					if(newY[i] - offsetDown > Constants.GridSizeYmax)
						offsetDown = newY[i] - Constants.GridSizeYmax;
				}

				if(offsetDown > 0)
				{
					for(uint i = 0 ; i < Constants.BlockPerPiece; i++)
					{
						newY[i] -= offsetDown;
					}
				}

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
							break;
						}
					}
					iteration++;
				} while(!canRotate && iteration < 3);
			}
			return canRotate;
   		}
	}
}

