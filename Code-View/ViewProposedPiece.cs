using System;
using Android.Content;
using Android.Views;
using Android.Graphics;
using Android.Util;

namespace Tetris
{
	public class ViewProposedPiece : View
	{
		private Player m_player = null;
		PieceView[] m_piece = new PieceView[Constants.NbProposedPiece];
		private int m_blockSize = 0;
		private uint m_pieceHilite = Constants.NbProposedPiece;
		BluetoothManager m_bluetooth = null;

		public ViewProposedPiece(Context context, IAttributeSet attrs) : base(context, attrs)
		{
			for(int i = 0; i < Constants.NbProposedPiece; i++)
			{
				m_piece[i] = null;
			}
		}

		public void SetBluetooth(BluetoothManager bluetooth)
		{
			m_bluetooth = bluetooth;
		}

		public void SetPiece(Player player)
		{
			m_player = player;
			for(int i = 0; i < Constants.NbProposedPiece; i++)
			{
				if(i < player.m_proposedPieces.Length)
					m_piece[i] = new PieceView(player.m_proposedPieces[i], false);
				else
					m_piece[i] = null;
			}
		}

		public void ChangePieceHilite()
		{
			if(m_pieceHilite < Constants.NbProposedPiece)
			{
				m_player.ChangeProposedPiece((int) m_pieceHilite);
				m_piece[m_pieceHilite] = new PieceView(m_player.m_proposedPieces[m_pieceHilite], false);
				m_pieceHilite = Constants.NbProposedPiece;
			}
		}

		protected override void OnDraw(Canvas canvas)
		{
			base.OnDraw(canvas);

			if(m_blockSize == 0)
			{
				// on considere qu'il faut un espace de 5 blocks pour chaque piece (sauf pour la derniere)
				m_blockSize = Math.Min(Math.Abs(canvas.ClipBounds.Right - canvas.ClipBounds.Left)/((Constants.NbProposedPiece/Constants.NbLinePropPiece)*5 - 1),
										Math.Abs(canvas.ClipBounds.Top - canvas.ClipBounds.Bottom)/(Constants.NbLinePropPiece*5 - 1));
			}
			for(int i = 0; i < Constants.NbProposedPiece; i++)
			{
				if(m_piece[i] != null)
					m_piece[i].Draw(canvas, m_blockSize, i==m_pieceHilite);
			}
		}

		public override bool OnTouchEvent(MotionEvent e)
		{
			bool retour = base.OnTouchEvent(e);

			if(m_bluetooth != null)
			{
				int x = ((int) e.GetX())/(m_blockSize*5);
				int y = ((int) e.GetY())/(m_blockSize*5);

				// We low the value if it is too high
				if(x >= Constants.NbProposedPiece/Constants.NbLinePropPiece)
					x = Constants.NbProposedPiece/Constants.NbLinePropPiece - 1;
				if(y >= Constants.NbLinePropPiece)
					y = Constants.NbLinePropPiece - 1;

				int i = x + y*(Constants.NbProposedPiece/Constants.NbLinePropPiece);

				m_bluetooth.Write(m_player.GetMessageSendNewPiece(i));
				m_pieceHilite = (uint) i;
			}

			Invalidate();
			return retour;
		}
	}
}

