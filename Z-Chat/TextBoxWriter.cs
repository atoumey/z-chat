using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Globalization;
using System.IO;
using System.Windows.Controls;
using ZChat;

[Serializable, ComVisible(true)]
public class TextBoxWriter : TextWriter
{
    // Fields
    private bool _isOpen;
    private TextBox _tb;
    private static UnicodeEncoding m_encoding;

    // Methods
    public TextBoxWriter(TextBox tb) : this(tb, CultureInfo.CurrentCulture)
    {
    }

    public TextBoxWriter(TextBox tb, IFormatProvider formatProvider) : base(formatProvider)
    {
        if (tb == null)
        {
            throw new ArgumentNullException("tb", "Argument cannot be null.");
        }
        this._tb = tb;
        this._isOpen = true;
    }

    public override void Close()
    {
        this.Dispose(true);
    }

    protected override void Dispose(bool disposing)
    {
        this._isOpen = false;
        base.Dispose(disposing);
    }

    public virtual TextBox GetTextBox()
    {
        return this._tb;
    }

    public override string ToString()
    {
        return this._tb.Text;
    }

    public override void Write(char value)
    {
        if (!this._isOpen)
        {
            throw new ObjectDisposedException("The writer is closed.");
        }
        _tb.Dispatcher.Invoke(new VoidDelegate(delegate
        {
            Chat.AppendAndMaybeScrollToBottom(_tb, value.ToString());
        }));
    }

    public override void Write(string value)
    {
        if (!this._isOpen)
        {
            throw new ObjectDisposedException("The writer is closed.");
        }
        if (value != null)
        {
            _tb.Dispatcher.Invoke(new VoidDelegate(delegate
            {
                Chat.AppendAndMaybeScrollToBottom(_tb, value);
            }));
        }
    }

    public override void Write(char[] buffer, int index, int count)
    {
        if (!this._isOpen)
        {
            throw new ObjectDisposedException("The writer is closed.");
        }
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer", "Argument cannot be null.");
        }
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException("index", "Argument was out of range.  Need a non-negative number.");
        }
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException("count", "Argument was out of range.  Need a non-negative number.");
        }
        if ((buffer.Length - index) < count)
        {
            throw new ArgumentException("Invalid offset length.");
        }
        _tb.Dispatcher.Invoke(new VoidDelegate(delegate
        {
            Chat.AppendAndMaybeScrollToBottom(_tb, new string(buffer, index, count));
        }));
    }

    // Properties
    public override Encoding Encoding
    {
        get
        {
            if (m_encoding == null)
            {
                m_encoding = new UnicodeEncoding(false, false);
            }
            return m_encoding;
        }
    }
}
