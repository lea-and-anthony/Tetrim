using System;
using System.Collections.Generic;

using Android.Graphics;

namespace Tetrim
{
	public class BlockView
	{
		//--------------------------------------------------------------
		// CONSTANTS
		//--------------------------------------------------------------
		// Define the transparency of the shadow piece, between 0 and 255
		private const int AlphaShadow = 100;
		private const int AlphaOpaque = 255;

		// Percentages of the size of the block, between 0 and 1
		private const float BorderWidth = 0.02f; // %
		private const float BorderWidth3D = 0.15f; // %

		// Saturation and Value values for the different colours of the block, between 0 and 1
		private static float[] SvBaseColor = {1f, 1f};
		private static float[] SvLeftColor = {0.35f, 1f};
		private static float[] SvTopColor = {0.25f, 1f};
		private static float[] SvRightColor = {1f, 0.75f};
		private static float[] SvBottomColor = {1f, 0.65f};
		private static float[] SvBorderColor = {1f, 0.50f};

		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		private Block _block; // Instance of the block to display
		private bool _isShadow; // true if the block is a shadow at the bottom of the grid

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public BlockView (Block block, bool isShadow)
		{
			Update(block, isShadow);
		}

		//--------------------------------------------------------------
		// STATIC METHODS
		//--------------------------------------------------------------
		public static Bitmap CreateImage(int blockSize, TetrisColor color)
		{
			if(color == TetrisColor.ColorMax)
				return null;

			// Create the image with its canvas to draw in it
			Bitmap image = Bitmap.CreateBitmap(blockSize, blockSize, Bitmap.Config.Argb8888);
			Canvas imageCanvas = new Canvas(image);

			// Calculate the width of the borders with a percentage of the size of the block
			float borderWidth = BorderWidth * blockSize;
			float borderWidth3D = BorderWidth3D * blockSize;

			// Calculate all the colors for the block based on its tetris color
			Color baseColor = adjustColor(Utils.getAndroidColor(color), SvBaseColor);
			Color topColor = adjustColor(baseColor, SvTopColor);
			Color leftColor = adjustColor(baseColor, SvLeftColor);
			Color rightColor = adjustColor(baseColor, SvRightColor);
			Color bottomColor = adjustColor(baseColor, SvBottomColor);
			Color borderColor = adjustColor(baseColor, SvBorderColor);

			// Draw the border
			Paint borderPaint = new Paint {AntiAlias = true, Color = borderColor};
			for(int i = 0 ; i < BorderWidth ; i++)
			{
				imageCanvas.DrawRect(i, blockSize - i, blockSize - i, i, borderPaint);
			}

			// Define the corners of the big rectangle without the border
			PointF outerRectTopLeft = new PointF(
				borderWidth,
				borderWidth);
			PointF outerRectTopRight = new PointF(
				blockSize - borderWidth,
				borderWidth);
			PointF outerRectBottomRight = new PointF(
				blockSize - borderWidth,
				blockSize - borderWidth);
			PointF outerRectBottomLeft = new PointF(
				borderWidth,
				blockSize - borderWidth);

			// Define the corners of the small rectangle in the middle
			PointF innerRectTopLeft = new PointF(
				borderWidth + borderWidth3D,
				borderWidth + borderWidth3D);
			PointF innerRectTopRight = new PointF(
				blockSize -borderWidth - borderWidth3D,
				borderWidth + borderWidth3D);
			PointF innerRectBottomRight = new PointF(
				blockSize -borderWidth - borderWidth3D,
				blockSize -borderWidth - borderWidth3D);
			PointF innerRectBottomLeft = new PointF(
				borderWidth + borderWidth3D,
				blockSize -borderWidth - borderWidth3D);
			
			// Draw inner square
			PointF[] innerSquare = new[]
			{
				innerRectTopLeft,
				innerRectTopRight,
				innerRectBottomRight,
				innerRectBottomLeft
			};
			drawPolygonInCanvas(imageCanvas, innerSquare, baseColor);

			// Draw top 3D border
			PointF[] top3dBorder = new[]
			{
				outerRectTopLeft,
				outerRectTopRight,
				innerRectTopRight,
				innerRectTopLeft
			};
			drawPolygonInCanvas(imageCanvas, top3dBorder, topColor);

			// Draw bottom 3D border
			PointF[] bottom3dBorder = new[]
			{
				innerRectBottomLeft,
				innerRectBottomRight,
				outerRectBottomRight,
				outerRectBottomLeft
			};
			drawPolygonInCanvas(imageCanvas, bottom3dBorder, bottomColor);

			// Draw left 3D border
			PointF[] left3dBorder = new[]
			{
				outerRectTopLeft,
				innerRectTopLeft,
				innerRectBottomLeft,
				outerRectBottomLeft
			};
			drawPolygonInCanvas(imageCanvas, left3dBorder, leftColor);

			// Draw right 3D border
			PointF[] right3dBorder = new[]
			{
				innerRectTopRight,
				outerRectTopRight,
				outerRectBottomRight,
				innerRectBottomRight
			};
			drawPolygonInCanvas(imageCanvas, right3dBorder, rightColor);

			return image;
		}

		private static void drawPolygonInCanvas(Canvas canvas, PointF[] polygon, Color color)
		{
			var path = new Path();
			// Set the first point, that the drawing will start from.
			path.MoveTo(polygon[0].X, polygon[0].Y);
			for (var i = 1; i < polygon.Length; i++)
			{
				// Draw a line from the previous point in the path to the new point.
				path.LineTo(polygon[i].X, polygon[i].Y);
			}

			Paint paint = new Paint {
				AntiAlias = true,
				Color = color
			};
			paint.SetStyle(Paint.Style.Fill);
			canvas.DrawPath(path, paint);
		}

		// Adjust the saturation and value of a color keeping the hue intact
		private static Color adjustColor(Color color, float[] satVal)
		{
			return Color.HSVToColor( new float[] {color.GetHue(), satVal[0], satVal[1]} );
		}

		//--------------------------------------------------------------
		// PUBLIC METHODS
		//--------------------------------------------------------------
		public void Draw (Canvas canvas, float blockSize, Dictionary<TetrisColor, Bitmap> blockImages, float xOffset, float yOffset)
		{
			Draw(canvas, blockSize, blockImages, xOffset, yOffset, 0);
		}

		public void Draw (Canvas canvas, float blockSize, Dictionary<TetrisColor, Bitmap> blockImages, float xOffset, float yOffset, int height)
		{
			if (_block != null)
			{
				// Define the boundaries of the block
				float left = blockSize*_block._x + xOffset;
				//float right = left + blockSize;
				float top = height == 0 ? blockSize*(Constants.GridSizeYmax - _block._y) + yOffset :
					height - blockSize*(_block._y + 1) - yOffset;
				//float bottom = top + blockSize;

				// Draw the image inside the block
				Paint paint = new Paint();
				paint.Alpha = _isShadow ? AlphaShadow : AlphaOpaque;
				canvas.DrawBitmap(blockImages[_block._color], left, top, paint);
			}
		}
			
		public void Update(Block block, bool isShadow)
		{
			// Associate the new instances
			_block = block;
			_isShadow = isShadow;
		}

		// set xSize and ySize to the x and y size of the block (this function suppose that the piece is in 0,0)
		public void GetDrawnSize(float blockSize, ref float xSize, ref float ySize)
		{
			xSize = blockSize*(_block._x+1);
			ySize = blockSize*(_block._y+1);
		}
	}
}

