using Android.Views;
using Android.Graphics;
using Android.Content;
using Android.Util;

namespace Tetrim
{
	public class MyView : View
	{
		public GridView m_gridView;

		public MyView(Context context, IAttributeSet attrs) : base(context, attrs)
		{
			m_gridView = null;
		}

		protected override void OnDraw(Canvas canvas)
		{
			base.OnDraw(canvas);

			if(m_gridView != null)
				m_gridView.Draw(canvas);
		}
	}
}

