using System;
using System.Threading;
using System.Collections.Generic;

using Android.Graphics;

namespace Tetrim
{
	public class GridView
	{
		//--------------------------------------------------------------
		// CONSTANTS
		//--------------------------------------------------------------
		// Width of the line around the grid
		private const float StrokeWidthBorder = 5;

		// Transparency of the quartering o f the grid, between 0 and 255
		private const int QuarteringAlpha = 50;

		// Colors used for the paints of the grid
		private static Color BorderColor = Color.Gainsboro;
		private static Color HightlightBorderColor = Color.White;
		private static Color BackgroundColor = Color.Rgb(25, 20, 35);

		// Paints used to draw the grid
		private static Paint BackgroundPaint = createPaintWithStyle(
			new Paint {AntiAlias = true, Color = BackgroundColor},
			Paint.Style.Fill);
		private static Paint BorderPaint = createPaintWithStyle(
			new Paint {AntiAlias = true, Color = BorderColor, StrokeWidth = StrokeWidthBorder},
			Paint.Style.Stroke);
		private static Paint GridPaint = createPaintWithStyle(
			new Paint {AntiAlias = true, Color = BorderColor, StrokeWidth = StrokeWidthBorder, Alpha = QuarteringAlpha},
			Paint.Style.Stroke);

		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		private Grid _grid; // Instance of the grid to display
		private BlockView[,] _mapView; // Array of the BlockViews constituting the grid

		private PieceView _fallingPieceView; // View of the piece falling from the top of the grid
		private PieceView _shadowPieceView; // View of the shadow of the piece falling from the top of the grid

		private int _blockSize = 0; // Size of the blocks in pixels according to the screen resolution
		private Dictionary<TetrisColor, Bitmap> _blockImages = new Dictionary<TetrisColor, Bitmap>(); // Images of the blocks

		private Mutex _mutexView = null; // To Prevent the modification of the view while it is displayed

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public GridView (Grid grid)
		{
			// Associate the instance
			_grid = grid;

			// Create the associated PieceViews
			_fallingPieceView = new PieceView(_grid._fallingPiece, false);
			_shadowPieceView = new PieceView(_grid._shadowPiece, true);

			// Create the associated BlockViews
			_mapView = new BlockView[Constants.GridSizeX, Constants.GridSizeY];
			for (uint i = 0 ; i < _grid._map.GetLength(0) ; i++)
			{
				for (uint j = 0 ; j < _grid._map.GetLength(1) ; j++)
				{
					_mapView[i,j] = new BlockView(_grid._map[i,j], false);
				}
			}

			_mutexView = new Mutex(false);
		}

		//--------------------------------------------------------------
		// STATIC METHODES
		//--------------------------------------------------------------
		private static Paint createPaintWithStyle(Paint paint, Paint.Style style)
		{
			paint.SetStyle(style);
			return paint;
		}

		//--------------------------------------------------------------
		// PUBLIC METHODES
		//--------------------------------------------------------------
		public void Draw (Canvas canvas)
		{
			// If it is the first draw, calculate the size of the block according to the size of the canvas
			if (_blockSize == 0)
			{
				// Calculate the size of the block
				_blockSize = calculateBlockSize(canvas);

				// Create the blocks images with the right size
				foreach(TetrisColor color in Enum.GetValues(typeof(TetrisColor)))
				{
					_blockImages.Add(color, BlockView.CreateImage(_blockSize, color));
				}
			}

			// Calculate the boundaries of the grid
			float left = _blockSize*Constants.GridSizeXmin;
			float top = Math.Abs(canvas.ClipBounds.Top - canvas.ClipBounds.Bottom)-_blockSize*Constants.GridSizeYmin;
			float right = _blockSize*(Constants.GridSizeXmax+1);
			float bottom = Math.Abs (canvas.ClipBounds.Top - canvas.ClipBounds.Bottom) - _blockSize * (Constants.GridSizeYmax+1);

			// Draw the background
			canvas.DrawRect(left, top, right, bottom, BackgroundPaint);

			// Draw the border
			canvas.DrawRect(left + StrokeWidthBorder/2, top, right, bottom - StrokeWidthBorder/2, BorderPaint);

			// Draw the vertical quartering
			for(float x = left ; x < right ; x += _blockSize)
			{
				canvas.DrawLine(x, top, x, bottom, GridPaint);
			}

			// Draw the horizontal quartering
			for(float y = bottom ; y < top ; y += _blockSize)
			{
				canvas.DrawLine(left, y, right, y, GridPaint);
			}

			// Before drawing any block, we need to set the mutex so we don't change the view while it is displayed
			_mutexView.WaitOne();

			// Draw the pieces
			_shadowPieceView.Draw(canvas, _blockSize, _blockImages);
			_fallingPieceView.Draw(canvas, _blockSize, _blockImages);

			// Draw the blocks
			for (uint i = 0 ; i < _grid._map.GetLength(0) ; i++)
			{
				for (uint j = 0 ; j < _grid._map.GetLength(1) ; j++)
				{
					_mapView[i,j].Draw(canvas, _blockSize, _blockImages);
				}
			}

			// Now we can change the view
			_mutexView.ReleaseMutex();
		}

		public void Update()
		{
			// Before updating any block, we need to set the mutex so we don't display the view while it is updating
			_mutexView.WaitOne();

			// Update the pieces
			_shadowPieceView.Update(_grid._shadowPiece, true);
			_fallingPieceView.Update(_grid._fallingPiece, false);

			// Update the blocks of the grid
			for (uint i = 0 ; i < _grid._map.GetLength(0) ; i++)
			{
				for (uint j = 0 ; j < _grid._map.GetLength(1) ; j++)
				{
					_mapView [i, j].Update(_grid._map[i,j], false);
				}
			}

			// Now we can display the view
			_mutexView.ReleaseMutex();
		}
			
		//--------------------------------------------------------------
		// PRIVATE METHODES
		//--------------------------------------------------------------
		private int calculateBlockSize(Canvas canvas)
		{
			return Math.Min(Math.Abs(canvas.ClipBounds.Right-canvas.ClipBounds.Left)/Constants.GridSizeX,
				Math.Abs(canvas.ClipBounds.Top - canvas.ClipBounds.Bottom)/Constants.GridSizeY);
		}
	}
}

