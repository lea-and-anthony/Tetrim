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
			new Paint {AntiAlias = true, Color = BorderColor},
			Paint.Style.Fill);
		private static Paint GridPaint = Utils.createPaintWithStyle(
			new Paint {AntiAlias = true, Color = BorderColor, Alpha = QuarteringAlpha},
			Paint.Style.Fill);

		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		public bool _frameRendered { get; private set; }

		private Grid _grid = null; // Instance of the grid to display
		private BlockView[,] _mapView = null; // Array of the BlockViews constituting the grid

		private PieceView _fallingPieceView = null; // View of the piece falling from the top of the grid
		private PieceView _shadowPieceView = null; // View of the shadow of the piece falling from the top of the grid

		// Width of the line around the grid
		private int _strokeWidthBorder = 4;
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
		// STATIC METHODS
		//--------------------------------------------------------------
		public static int CalculateBlockSize(int width, int height, int strokeWidthBorder)
		{
			return Math.Min((width - 2*strokeWidthBorder)/Constants.GridSizeX, (height - 2*strokeWidthBorder)/Constants.GridSizeY);
		}

		public static Point CalculateUseSize(int width, int height)
		{
			int strokeWidthBorder = calculateBorderSize(width, height);
			int blockSize = CalculateBlockSize(width, height, strokeWidthBorder);
			return new Point(blockSize * Constants.GridSizeX + 2 * strokeWidthBorder, blockSize * Constants.GridSizeY + 2 * strokeWidthBorder);
		}

		private static int calculateBorderSize(int width, int height)
		{
			return Math.Max(1, Math.Min(4 * width / 400, 4 * height / 800));
		}

		//--------------------------------------------------------------
		// OVERRIDE METHODS
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
			_shadowPieceView.Draw(canvas, _blockSize, _blockImages, _strokeWidthBorder, _strokeWidthBorder);
			_fallingPieceView.Draw(canvas, _blockSize, _blockImages, _strokeWidthBorder, _strokeWidthBorder);

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
		// PUBLIC METHODS
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
		// PRIVATE METHODS
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
					_mapView[i,j].Draw(bitmapCanvas, _blockSize, _blockImages, _strokeWidthBorder, _strokeWidthBorder);
				}
			}

			bitmapCanvas.Dispose();

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
				/*Rect rect = new Rect();
				GetDrawingRect(rect);
				int height = Math.Abs(rect.Bottom - rect.Top);
				int width = Math.Abs(rect.Right - rect.Left);*/
				_strokeWidthBorder = calculateBorderSize(Width, Height);
				_blockSize = CalculateBlockSize(Width, Height, _strokeWidthBorder);

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
			float left = _blockSize*Constants.GridSizeXmin + _strokeWidthBorder;
			float top = _blockSize*Constants.GridSizeYmin + _strokeWidthBorder; // the O point is in the left hand corner
			float right = _blockSize*(Constants.GridSizeXmax+1) + _strokeWidthBorder;
			float bottom = _blockSize*(Constants.GridSizeYmax+1) + _strokeWidthBorder;

			// Draw the background
			bitmapCanvas.DrawRect(left, top, right, bottom, BackgroundPaint);

			// Draw the borders
			bitmapCanvas.DrawRect(0, top - _strokeWidthBorder, _strokeWidthBorder, bottom + _strokeWidthBorder, BorderPaint);
			bitmapCanvas.DrawRect(right, top - _strokeWidthBorder, right + _strokeWidthBorder, bottom + _strokeWidthBorder, BorderPaint);
			bitmapCanvas.DrawRect(left - _strokeWidthBorder, 0, right + _strokeWidthBorder, _strokeWidthBorder, BorderPaint);
			bitmapCanvas.DrawRect(left - _strokeWidthBorder, bottom, right + _strokeWidthBorder, bottom +  _strokeWidthBorder, BorderPaint);

			// Draw the vertical quartering
			for(float x = left + _blockSize; x < right ; x += _blockSize)
			{
				bitmapCanvas.DrawRect(x - _strokeWidthBorder / 2, top, x + _strokeWidthBorder - _strokeWidthBorder / 2, bottom, GridPaint);
			}

			// Draw the horizontal quartering
			for(float y = top + _blockSize; y < bottom ; y += _blockSize)
			{
				bitmapCanvas.DrawRect(left, y - _strokeWidthBorder / 2, right, y + _strokeWidthBorder - _strokeWidthBorder / 2, GridPaint);
			}

			bitmapCanvas.Dispose();
		}
	}
}

