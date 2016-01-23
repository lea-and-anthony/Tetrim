namespace Tetrim
{
	public class Block
	{
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		public int _x { get; set; }
		public int _y { get; set; }
		public TetrisColor _color { get; set; }

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public Block (int x, int y, TetrisColor color)
		{
			_x = x;
			_y = y;
			_color = color;
		}

		public Block (Block block)
		{
			_x = block._x;
			_y = block._y;
			_color = block._color;
		}

		//--------------------------------------------------------------
		// METHODES
		//--------------------------------------------------------------
		public void MoveDown ()
		{
			_y --;
		}
	}
}

