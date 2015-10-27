using System;

using Android.Views;
using Android.Graphics;

namespace Tetris
{
	public class BlockView
	{
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		public Block m_block { get; private set; }
		public Paint m_color { get; private set; }
		public Paint m_borderColor { get; private set; }
		public Paint m_hightlightBorderColor { get; private set; }

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public BlockView (Block block, bool isShadow)
		{
			m_block = block;
			createPaint(isShadow);
		}

		//--------------------------------------------------------------
		// METHODES
		//--------------------------------------------------------------
		private void createPaint(bool isShadow)
		{
			if (m_block != null)
			{
				m_color = new Paint {
					AntiAlias = true,
					Color = Utils.getAndroidColor(m_block.m_color),
				};
				m_color.SetStyle(Paint.Style.Fill);
				if(isShadow)
				{
					m_color.Alpha = 100;
				}

				m_borderColor = new Paint {
					AntiAlias = true,
					Color = Utils.BorderColor,
				};
				m_borderColor.SetStyle(Paint.Style.Stroke);
				m_borderColor.StrokeWidth = Utils.StrokeWidth;
				if(isShadow)
				{
					m_borderColor.Alpha = 100;
				}

				m_hightlightBorderColor = new Paint {
					AntiAlias = true,
					Color = Utils.HightlightBorderColor,
				};
				m_hightlightBorderColor.SetStyle(Paint.Style.Stroke);
				m_hightlightBorderColor.StrokeWidth = Utils.StrokeWidthHightlight;
				if(isShadow)
				{
					m_hightlightBorderColor.Alpha = 100;
				}
			}
		}

		public void Draw (Canvas canvas, float blockSize, bool isHighlight = false)
		{
			if (m_block != null)
			{
				canvas.DrawRect(blockSize*m_block.m_x,
					Math.Abs(canvas.ClipBounds.Top - canvas.ClipBounds.Bottom)-blockSize*(m_block.m_y+1),
					blockSize*(m_block.m_x+1),
					Math.Abs(canvas.ClipBounds.Top - canvas.ClipBounds.Bottom)-blockSize*m_block.m_y,
					m_color);
					
				if (!isHighlight)
				{
					canvas.DrawRect(blockSize*m_block.m_x,
						Math.Abs(canvas.ClipBounds.Top - canvas.ClipBounds.Bottom)-blockSize*(m_block.m_y+1),
						blockSize*(m_block.m_x+1),
						Math.Abs(canvas.ClipBounds.Top - canvas.ClipBounds.Bottom)-blockSize*m_block.m_y,
						m_borderColor);
				}
				else
				{
					canvas.DrawRect(blockSize*m_block.m_x,
						Math.Abs(canvas.ClipBounds.Top - canvas.ClipBounds.Bottom)-blockSize*(m_block.m_y+1),
						blockSize*(m_block.m_x+1),
						Math.Abs(canvas.ClipBounds.Top - canvas.ClipBounds.Bottom)-blockSize*m_block.m_y,
						m_hightlightBorderColor);
				}
			}

		}
			
		public void Update(Block block, bool isShadow)
		{
			m_block = block;
			createPaint(isShadow);
		}
	}
}

