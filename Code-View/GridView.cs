using System;

using Android.Graphics;
using Android.Util;

namespace Tetris
{
	public class GridView
	{
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		public Grid m_grid { get; private set; }
		public BlockView[,] m_mapView { get; private set; }
		public PieceView m_fallingPieceView { get; private set; }
		public PieceView m_shadowPieceView { get; private set; }
		public int m_blockSize { get; private set; }
		public Paint m_backgroundColor { get; private set; }
		public Paint m_borderColor { get; private set; }

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public GridView (Grid grid)
		{
			m_blockSize = 0;
			m_grid = grid;
			m_fallingPieceView = new PieceView(m_grid.m_fallingPiece, false);
			m_shadowPieceView = new PieceView(m_grid.m_shadowPiece, true);
			m_mapView = new BlockView[Constants.GridSizeX, Constants.GridSizeY];
			for (uint i = 0 ; i < m_grid.m_map.GetLength(0) ; i++)
			{
				for (uint j = 0 ; j < m_grid.m_map.GetLength(1) ; j++)
				{
					m_mapView[i,j] = new BlockView(m_grid.m_map[i,j], false);
				}
			}

			m_backgroundColor = new Paint {
				AntiAlias = true,
				Color = Utils.BackgroundColor,
			};
			m_backgroundColor.SetStyle(Paint.Style.Fill);

			m_borderColor = new Paint {
				AntiAlias = true,
				Color = Utils.BorderColor,
			};
			m_borderColor.SetStyle(Paint.Style.Stroke);
			m_borderColor.StrokeWidth = Utils.StrokeWidth;
		}

		//--------------------------------------------------------------
		// METHODES
		//--------------------------------------------------------------
		public void Draw (Canvas canvas)
		{
			// If it is the first draw,
			// calculate the size of the block according to the size of the canvas
			if (m_blockSize == 0)
			{
				m_blockSize = calculateBlockSize(canvas);
			}

			// Draw the background
			canvas.DrawRect(m_blockSize*Constants.GridSizeXmin,
							Math.Abs(canvas.ClipBounds.Top - canvas.ClipBounds.Bottom)-m_blockSize*Constants.GridSizeYmin,
							m_blockSize*(Constants.GridSizeXmax+1),
							Math.Abs(canvas.ClipBounds.Top - canvas.ClipBounds.Bottom)-m_blockSize*(Constants.GridSizeYmax+1),
							m_backgroundColor);

			// Draw the border
			canvas.DrawRect(m_blockSize * Constants.GridSizeXmin,
							Math.Abs (canvas.ClipBounds.Top - canvas.ClipBounds.Bottom) - m_blockSize * Constants.GridSizeYmin,
							m_blockSize*(Constants.GridSizeXmax+1),
							Math.Abs (canvas.ClipBounds.Top - canvas.ClipBounds.Bottom) - m_blockSize * (Constants.GridSizeYmax+1),
							m_borderColor);

			// Draw the piece
			m_shadowPieceView.Draw(canvas, m_blockSize);
			m_fallingPieceView.Draw(canvas, m_blockSize);

			// Draw the blocks
			for (uint i = 0 ; i < m_grid.m_map.GetLength(0) ; i++)
			{
				for (uint j = 0 ; j < m_grid.m_map.GetLength(1) ; j++)
				{
					m_mapView[i,j].Draw(canvas, m_blockSize);
				}
			}
		}

		public int calculateBlockSize(Canvas canvas)
		{
			return Math.Min(Math.Abs(canvas.ClipBounds.Right-canvas.ClipBounds.Left)/Constants.GridSizeX,
							Math.Abs(canvas.ClipBounds.Top - canvas.ClipBounds.Bottom)/Constants.GridSizeY);
		}

		public void Update()
		{
			m_shadowPieceView = new PieceView(m_grid.m_shadowPiece, true);
			m_fallingPieceView = new PieceView(m_grid.m_fallingPiece, false);
			for (uint i = 0 ; i < m_grid.m_map.GetLength(0) ; i++)
			{
				for (uint j = 0 ; j < m_grid.m_map.GetLength(1) ; j++)
				{
					m_mapView [i, j].Update (m_grid.m_map[i,j], false);
				}
			}
		}

	}
}

