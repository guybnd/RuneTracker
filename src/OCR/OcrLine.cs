namespace RuneshapePriceChecker.OCR;

/// <summary>
/// One line of recognised text and where it sat in the bitmap it was read from.
///
/// The price pipeline only ever needed a line's vertical position, so
/// <see cref="WindowsOcrEngine.Recognize"/> reduces each line to an averaged Y and drops the rest.
/// Reading a panel that can be anywhere on screen needs the whole box: the horizontal extent says
/// which lines belong to the same panel, and the exact rectangle is where an overlay draws.
/// </summary>
/// <param name="Text">The line's words joined by single spaces.</param>
/// <param name="Bounds">The union of the words' bounding boxes, in bitmap pixels.</param>
public readonly record struct OcrLine(string Text, Rectangle Bounds);
