//using Android.annotation.TargetApi;
using Java.Lang;

using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Util;
using Android.Widget;

public class AutoResizeTextView : TextView
{
	//--------------------------------------------------------------
	// INTERFACE
	//--------------------------------------------------------------
	//--------------------------------------------------------------
	// CONSTANTS
	//--------------------------------------------------------------
	private static int NO_LINE_LIMIT = -1;

	//--------------------------------------------------------------
	// ATTRIBUTES
	//--------------------------------------------------------------
	private RectF _textRect = new RectF();
	private RectF _availableSpaceRect;
	private SparseIntArray _textCachedSizes;
	private TextPaint _paint;
	private float _maxTextSize;
	private float _minTextSize = 20;
	private float _spacingMult = 1.0f;
	private float _spacingAdd = 0.0f;
	private int _widthLimit;
	private int _maxLines;
	private bool _enableSizeCache = true;
	private bool _initiallized;

	//--------------------------------------------------------------
	// CONSTRUCTORS
	//--------------------------------------------------------------
	public AutoResizeTextView(Context context) : base(context)
	{
		initialize();
	}

	public AutoResizeTextView(Context context, IAttributeSet attrs) : base(context, attrs)
	{
		initialize();
	}

	public AutoResizeTextView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
	{
		initialize();
	}

	private void initialize()
	{
		_paint = new TextPaint(Paint);
		_maxTextSize = TextSize;
		_availableSpaceRect = new RectF();
		_textCachedSizes = new SparseIntArray();
		if (_maxLines == 0) {
			// no value was assigned during construction
			_maxLines = NO_LINE_LIMIT;
		}
		_initiallized = true;
	}


	//--------------------------------------------------------------
	// OVERRIDES
	//--------------------------------------------------------------
	public int MaxLines
	{
		get
		{
			return _maxLines;
		}
	}

	//--------------------------------------------------------------
	// OVERRIDES
	//--------------------------------------------------------------
	public override void SetText (ICharSequence text, BufferType type)
	{
		base.SetText (text, type);
		adjustTextSize(text.ToString());
	}

	public override void SetTextSize (ComplexUnitType unit, float size)
	{
		Context c = Context;
		Resources r;
		if (c == null)
			r = Resources.System;
		else
			r = c.Resources;
		_maxTextSize = TypedValue.ApplyDimension(unit, size, r.DisplayMetrics);
		_textCachedSizes.Clear();
		adjustTextSize(Text);
	}

	public override void SetMaxLines (int maxlines)
	{
		base.SetMaxLines (maxlines);
		_maxLines = maxlines;
		reAdjust();
	}

	public override void SetSingleLine ()
	{
		base.SetSingleLine ();
		_maxLines = 1;
		reAdjust();
	}

	public override void SetSingleLine (bool singleLine)
	{
		base.SetSingleLine (singleLine);
		if (singleLine) {
			_maxLines = 1;
		} else {
			_maxLines = NO_LINE_LIMIT;
		}
		reAdjust();
	}

	public override void SetLines (int lines)
	{
		base.SetLines (lines);
		_maxLines = lines;
		reAdjust();
	}

	public override void SetLineSpacing (float add, float mult)
	{
		base.SetLineSpacing (add, mult);
		_spacingMult = mult;
		_spacingAdd = add;
	}

	/**
	 * Set the lower text size limit and invalidate the view
	 * 
	 * @param minTextSize
	 */
	public void SetMinTextSize(float minTextSize) {
		_minTextSize = minTextSize;
		reAdjust();
	}

	private void reAdjust() {
		adjustTextSize(Text);
	}

	private void adjustTextSize(string str)
	{
		if (!_initiallized)
		{
			return;
		}
		int startSize = (int) _minTextSize;
		int heightLimit = MeasuredHeight - CompoundPaddingBottom - CompoundPaddingTop;
		_widthLimit = MeasuredWidth - CompoundPaddingLeft	- CompoundPaddingRight;
		_availableSpaceRect.Right = _widthLimit;
		_availableSpaceRect.Bottom = heightLimit;
		base.SetTextSize(ComplexUnitType.Px, efficientTextSizeSearch(startSize, (int) _maxTextSize, _availableSpaceRect));
	}

	/*
	 * suggestedSize: Size of text to be tested
	 * availableSpace : available space in which text must fit
	 * return an integer < 0 if after applying {@code suggestedSize} to text, it takes less space than {@code availableSpace}, > 0 otherwise
	 */
	private int onTestSize(int suggestedSize, RectF availableSPace)
	{
		_paint.TextSize = suggestedSize;
		string text = Text;
		bool singleline = (MaxLines == 1);
		if (singleline)
		{
			_textRect.Bottom = _paint.FontSpacing;
			_textRect.Right = _paint.MeasureText(text);
		}
		else
		{
			StaticLayout layout = new StaticLayout(text, _paint,
				_widthLimit, Layout.Alignment.AlignNormal, _spacingMult,
				_spacingAdd, true);
			// return early if we have more lines
			if (MaxLines != NO_LINE_LIMIT && layout.LineCount > MaxLines)
			{
				return 1;
			}
			_textRect.Bottom = layout.Height;
			int maxWidth = -1;
			for (int i = 0; i < layout.LineCount; i++)
			{
				if (maxWidth < layout.GetLineWidth(i))
				{
					maxWidth = (int) layout.GetLineWidth(i);
				}
			}
			_textRect.Right = maxWidth;
		}

		_textRect.OffsetTo(0, 0);
		if (availableSPace.Contains(_textRect))
		{
			// may be too small, don't worry we will find the best match
			return -1;
		}
		else
		{
			// too big
			return 1;
		}
	}

	/*
	 * Enables or disables size caching, enabling it will improve performance
	 * where you are animating a value inside TextView. This stores the font
	 * size against getText().length() Be careful though while enabling it as 0
	 * takes more space than 1 on some fonts and so on.
	 */
	public void enableSizeCache(bool enable)
	{
		_enableSizeCache = enable;
		_textCachedSizes.Clear();
		adjustTextSize(Text);
	}

	private int efficientTextSizeSearch(int start, int end,	RectF availableSpace)
	{
		if (!_enableSizeCache) {
			return binarySearch(start, end, availableSpace);
		}
		string text = Text;
		int key = (text == null) ? 0 : text.Length;
		int size = _textCachedSizes.Get(key);
		if (size != 0) {
			return size;
		}
		size = binarySearch(start, end, availableSpace);
		_textCachedSizes.Put(key, size);
		return size;
	}

	private int binarySearch(int start, int end, RectF availableSpace)
	{
		int lastBest = start;
		int low = start;
		int high = end - 1;
		int middle = 0;
		while (low <= high)
		{
			middle = (low + high) >> 1;
			int midValCmp = onTestSize(middle, availableSpace);
			if (midValCmp < 0)
			{
				lastBest = low;
				low = middle + 1;
			}
			else if (midValCmp > 0)
			{
				high = middle - 1;
				lastBest = high;
			}
			else
			{
				return middle;
			}
		}
		// make sure to return last best
		// this is what should always be returned
		return lastBest;
	}

	protected override void OnTextChanged (ICharSequence text, int start, int before, int after)
	{
		base.OnTextChanged (text, start, before, after);
		reAdjust();
	}

	protected override void OnSizeChanged (int w, int h, int oldw, int oldh)
	{
		_textCachedSizes.Clear();
		base.OnSizeChanged (w, h, oldw, oldh);
		if (w != oldw || h != oldh)
		{
			reAdjust();
		}
	}
}