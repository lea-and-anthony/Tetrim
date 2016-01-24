using System;
using System.Collections.Generic;

using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;

namespace Tetrim
{
	public class NextPieceView : View
	{
		//--------------------------------------------------------------
		// CONSTANTS
		//--------------------------------------------------------------
		private const int MarginAroundPiece = 6;

		private const float StrokeWidthBorder = 5;
		private static Color BorderColor = Color.Gainsboro;
		private static Color BackgroundColor = Color.Rgb(25, 20, 35);

		// Paints used to draw the case
		private static Paint BackgroundPaint = Utils.createPaintWithStyle(
			new Paint {AntiAlias = true, Color = BackgroundColor},
			Paint.Style.Fill);
		private static Paint BorderPaint = Utils.createPaintWithStyle(
			new Paint {AntiAlias = true, Color = BorderColor, StrokeWidth = StrokeWidthBorder},
			Paint.Style.Stroke);

		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		private Player _player = null; // Instance of the player to whom the pieces are proposed
		private PieceView _nextPiece = null; // The next piece that will be displayed

		private int _blockSize = 0; // Size of the blocks in pixels according to the screen resolution
		private Dictionary<TetrisColor, Bitmap> _blockImages = new Dictionary<TetrisColor, Bitmap>(); // Images of the blocks

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public NextPieceView(Context context, IAttributeSet attrs) : base(context, attrs)
		{
		}

		//--------------------------------------------------------------
		// PUBLIC METHODES
		//--------------------------------------------------------------
		public void SetPlayer(Player player)
		{
			// Associate the new instance
			_player = player;
			player._grid.NextPieceChangedEvent += PostInvalidate;
			_nextPiece = new PieceView(player._grid._nextPiece, false);
		}

		//--------------------------------------------------------------
		// EVENT METHODES
		//--------------------------------------------------------------
		protected override void OnDraw(Canvas canvas)
		{
			base.OnDraw(canvas);

			// If it is the first draw, calculate the size of the block according to the size of the canvas
			if(_blockSize == 0)
			{
				// Calculate the size of the block. We need the space for at least 4 blocks.
				_blockSize = Math.Min((Width - MarginAroundPiece) / 4, (Height - MarginAroundPiece) / 4);

				// Create the blocks images with the right size
				foreach(TetrisColor color in Enum.GetValues(typeof(TetrisColor)))
				{
					_blockImages.Add(color, BlockView.CreateImage(_blockSize, color));
				}
			}

			if(_nextPiece != null && _player != null)
			{
				// First, we check if it is still the same piece we need to display
				_nextPiece.Update(_player._grid._nextPiece, false);

				// Get the size of the piece when drawn because if it's a 'O' or a 'I' it won't take the same space
				float xSize = 0;
				float ySize = 0;
				_nextPiece.GetDrawnSize(_blockSize, ref xSize, ref ySize);

				_nextPiece.Draw(canvas, _blockSize, _blockImages, (Width - xSize) / 2, (Height - ySize) / 2, Height);
			}
		}
	}
}

