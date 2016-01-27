using System;
using System.Collections.Generic;
using System.Threading;

using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;

namespace Tetrim
{
	public class GridView : View
	{
		//--------------------------------------------------------------
		// CONSTANTS
		//--------------------------------------------------------------
		// Width of the line around the grid
		private const int StrokeWidthBorder = 4;

		// Transparency of the quartering of the grid, between 0 and 255
		private const int QuarteringAlpha = 50;

		// Colors used for the paints of the grid
		private static Color BorderColor = Color.Gainsboro;
		private static Color HightlightBorderColor = Color.White;
		private static Color BackgroundColor = Color.Rgb(25, 20, 35);

		// Paints used to draw the grid
		private static Paint BackgroundPaint = Utils.createPaintWithStyle(
			new Paint {AntiAlias = true, Color = BackgroundColor},
			Paint.Style.Fill);
		private static Paint BorderPaint = Utils.createPaintWithStyle(
			new Paint {AntiAlias = true, Color = BorderColor, StrokeWidth = StrokeWidthBorder},
			Paint.Style.Stroke);
		private static Paint GridPaint = Utils.createPaintWithStyle(
			new Paint {AntiAlias = true, Color = BorderColor, StrokeWidth = StrokeWidthBorder, Alpha = QuarteringAlpha},
			Paint.Style.Stroke);

		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		public bool _frameRendered { get; private set; }

		private Grid _grid = null; // Instance of the grid to display
		private BlockView[,] _mapView = null; // Array of the BlockViews constituting the grid

		private PieceView _fallingPieceView = null; // View of the piece falling from the top of the grid
		private PieceView _shadowPieceView = null; // View of the shadow of the piece falling from the top of the grid

		private int _blockSize = 0; // Size of the blocks in pixels according to the screen resolution
		private Dictionary<TetrisColor, Bitmap> _blockImages = new Dictionary<TetrisColor, Bitmap>(); // Images of the blocks
		private bool _redraw = true;
		private Bitmap _bitmapBuffer = null;// Buffer to render the view faster
		private Bitmap _firstBitmapBuffer = null;// Buffer to render the view faster

		private Mutex _mutexView = null; // To Prevent the modification of the view while it is displayed

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public GridView (Context context, IAttributeSet attrs) : base(context, attrs)
		{
			_frameRendered = true;
		}

		//--------------------------------------------------------------
		// STATIC METHODES
		//--------------------------------------------------------------
		public static int CalculateBlockSize(Rect rect)
		{
			return Math.Min((Math.Abs(rect.Right - rect.Left) - 2*StrokeWidthBorder)/Constants.GridSizeX,
				(Math.Abs(rect.Top - rect.Bottom) - 2*StrokeWidthBorder)/Constants.GridSizeY);
		}

		public static Point CalculateUseSize(int width, int height)
		{
			int blockSize = CalculateBlockSize(new Rect(0, 0, width, height));
			return new Point(blockSize * Constants.GridSizeX + 2 * StrokeWidthBorder, blockSize * Constants.GridSizeY + 2 * StrokeWidthBorder);
		}

		//--------------------------------------------------------------
		// OVERRIDE METHODES
		//--------------------------------------------------------------
		public override void Draw (Canvas canvas)
		{
			_frameRendered = false;
			base.OnDraw(canvas);

			if(_grid == null)
				return; // if the gridView haven't been initialized, we stop

			// Before drawing any block, we need to set the mutex so we don't change the view while it is displayed
			_mutexView.WaitOne();

			if(_redraw)
				DrawBitmap();
			
			canvas.DrawBitmap(_bitmapBuffer, 0, 0, null);

			// Draw the pieces
			_shadowPieceView.Draw(canvas, _blockSize, _blockImages, StrokeWidthBorder, StrokeWidthBorder);
			_fallingPieceView.Draw(canvas, _blockSize, _blockImages, StrokeWidthBorder, StrokeWidthBorder);

			// Now we can change the view
			_mutexView.ReleaseMutex();
			_frameRendered = true;
		}

		public void RemoveBitmaps()
		{
			foreach(KeyValuePair<TetrisColor, Bitmap> entry in _blockImages)
			{
				entry.Value.Recycle();
				entry.Value.Dispose();
			}
			_blockImages.Clear();

			if(_bitmapBuffer != null)
			{
				_bitmapBuffer.Recycle();
				_bitmapBuffer.Dispose();
				_bitmapBuffer = null;
			}
			if(_firstBitmapBuffer != null)
			{
				_firstBitmapBuffer.Recycle();
				_firstBitmapBuffer.Dispose();
				_firstBitmapBuffer = null;
			}
		}

		//--------------------------------------------------------------
		// PUBLIC METHODES
		//--------------------------------------------------------------
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

			// We need to redraw the view
			_redraw = true;

			// Now we can display the view
			_mutexView.ReleaseMutex();
		}

		public void Init(Grid grid)
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
		// PRIVATE METHODES
		//--------------------------------------------------------------
		private void DrawBitmap()
		{
			if(_grid == null)
				return; // if the gridView haven't been initialized, we stop

			if(_bitmapBuffer == null)
			{
				_bitmapBuffer = Bitmap.CreateBitmap(Width, Height, Bitmap.Config.Argb8888);
			}

			Canvas bitmapCanvas = new Canvas(_bitmapBuffer);

			if(_firstBitmapBuffer == null)
				DrawFirstBitmap();

			bitmapCanvas.DrawBitmap(_firstBitmapBuffer, 0, 0, null);

			// Draw the blocks
			for (uint i = 0 ; i < _grid._map.GetLength(0) ; i++)
			{
				for (uint j = 0 ; j < _grid._map.GetLength(1) ; j++)
				{
					_mapView[i,j].Draw(bitmapCanvas, _blockSize, _blockImages, StrokeWidthBorder, StrokeWidthBorder);
				}
			}

			_redraw = false;
		}

		private void DrawFirstBitmap()
		{
			if(_grid == null)
				return; // if the gridView haven't been initialized, we stop

			if(_firstBitmapBuffer != null)
				return;
			
			_firstBitmapBuffer = Bitmap.CreateBitmap(Width, Height, Bitmap.Config.Argb8888);

			Canvas bitmapCanvas = new Canvas(_firstBitmapBuffer);

			// If it is the first draw, calculate the size of the block according to the size of the canvas
			if (_blockSize == 0)
			{
				// Calculate the size of the block
				Rect rectBound = new Rect();
				GetDrawingRect(rectBound);
				_blockSize = CalculateBlockSize(rectBound);

				// Create the blocks images with the right size
				foreach(TetrisColor color in Enum.GetValues(typeof(TetrisColor)))
				{
					Bitmap image = BlockView.CreateImage(_blockSize, color);
					if(image != null)
					{
						_blockImages.Add(color, image);
					}
				}
			}

			// Calculate the boundaries of the grid
			float left = _blockSize*Constants.GridSizeXmin + StrokeWidthBorder;
			float top = _blockSize*Constants.GridSizeYmin + StrokeWidthBorder; // the O point is in the left hand corner
			float right = _blockSize*(Constants.GridSizeXmax+1) + StrokeWidthBorder;
			float bottom = _blockSize*(Constants.GridSizeYmax+1) + StrokeWidthBorder;

			// Draw the background
			bitmapCanvas.DrawRect(left, top, right, bottom, BackgroundPaint);

			// Draw the borders
			bitmapCanvas.DrawLine(StrokeWidthBorder / 2, top - StrokeWidthBorder, StrokeWidthBorder / 2, bottom + 2 * StrokeWidthBorder, BorderPaint);
			bitmapCanvas.DrawLine(right + StrokeWidthBorder / 2, top - StrokeWidthBorder, right + StrokeWidthBorder / 2, bottom + 2 * StrokeWidthBorder, BorderPaint);
			bitmapCanvas.DrawLine(left - StrokeWidthBorder, StrokeWidthBorder / 2, right + 2 * StrokeWidthBorder, StrokeWidthBorder / 2, BorderPaint);
			bitmapCanvas.DrawLine(left - StrokeWidthBorder, bottom + StrokeWidthBorder / 2, right + 2 * StrokeWidthBorder, bottom + StrokeWidthBorder / 2, BorderPaint);

			// Draw the vertical quartering
			for(float x = left + _blockSize; x < right ; x += _blockSize)
			{
				bitmapCanvas.DrawLine(x, top, x, bottom, GridPaint);
			}

			// Draw the horizontal quartering
			for(float y = top + _blockSize; y < bottom ; y += _blockSize)
			{
				bitmapCanvas.DrawLine(left, y, right, y, GridPaint);
			}
		}
	}
}

