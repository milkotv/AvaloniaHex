using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaHex.Document;
using AvaloniaHex.Editing;
using AvaloniaHex.Extensions;
using AvaloniaHex.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AvaloniaHex;

/// <summary>
/// A control that allows for displaying and editing binary data in columns.
/// </summary>
public class HexEditorExt : HexEditor
{
    /// <summary>
    /// Creates a new empty modified hex editor.
    /// </summary>
    public HexEditorExt() : base()
    {
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Gets or sets the binary document that is currently being displayed.
    /// </summary>
    [RegularExpression("^[0-9a-fA-F]$", ErrorMessage = "FillChar must be 1 HEX character.")]
    public string FillChar { get; set; } = "0";

    /// <summary>
    /// Gets or sets the binary document that is currently being displayed.
    /// </summary>
    public bool CanResize { get; set; } = true;    

    /// <summary>
    /// Gets or sets if (in case of !CanResize) the Caret needs to rotate to the beining if reacues the end.
    /// </summary>
    public bool IsCyclic { get; set; }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(HexView);
        if (point.Properties.IsLeftButtonPressed)
        {
            var position = point.Position;

            if (HexView.GetColumnByPoint(position) is CellBasedColumn column)
            {
                Caret.PrimaryColumnIndex = column.Index;
                if (HexView.GetLocationByPoint(position) is { } location)
                {
                    // Do not allow to go boyond if !CanResize
                    if(IsOverflow(location.ByteIndex))
                    {
                        e.Handled = true;
                        return;
                    }
                }
            }
        }
        base.OnPointerPressed(e);
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (this.GetPropertyValue<bool>("_isMouseDragging")
            && this.GetPropertyValue<BitLocation?>("_selectionAnchorPoint") is { } anchorPoint
            && Caret.PrimaryColumn is { } column)
        {
            var position = e.GetPosition(HexView);
            if (HexView.GetLocationByPoint(position, column) is { } location)
            {
                // Do not allow to go boyond if !CanResize
                if (IsOverflow(location.ByteIndex))
                {
                    e.Handled = true;
                    return;
                }
            }
        }
        base.OnPointerMoved(e);
    }

    /// <inheritdoc />
    protected override void OnTextInput(TextInputEventArgs e)
    {
        // Are we in a writeable document?
        if (Document is not { IsReadOnly: false })
            return;

        // Do we have any text to write into a column?
        if (string.IsNullOrEmpty(e.Text) || Caret.PrimaryColumn is null)
            return;

        if (Caret.Mode == EditingMode.Insert)
        {
            // Can we insert?
            if (!Document.CanInsert)
                return;

            // If we selected something while inserting, a natural expectation is that the selection is deleted first.
            if (Selection.Range.ByteLength > 1)
            {
                if (!Document.CanRemove)
                    return;

                Delete();
            }
        }

        // Allow fill when > 1 b selected and not resizable
        if (!CanResize && Selection.Range.ByteLength > 1)
        {
            FillSelection(e.Text);
            DoUpdateSelection(Caret.Location, false);
            return;
        }

        // Dispatch text input to the primary column.
        var location = Caret.Location;
        if (!Caret.PrimaryColumn.HandleTextInput(ref location, e.Text, Caret.Mode))
            return;

        // Do not allow resizing and go to the begining if cyclic
        if (!CanResize && IsOverflow(location.ByteIndex) && location.BitIndex == 4)
        {
            if (IsCyclic)
            {
                Caret.GoToStartOfLine();
                DoUpdateSelection(location, false);
            }
            return;
        }

        // Update caret location.
        Caret.Location = location;
        DoUpdateSelection(Caret.Location, false);
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        var oldLocation = Caret.Location;
        bool isShiftDown = (e.KeyModifiers & KeyModifiers.Shift) != 0;

        switch (e.Key)
        {           
            case Key.Left:
                if (IsCyclic && oldLocation.ByteIndex == 0 && oldLocation.BitIndex == 4)
                {
                    Caret.GoToEndOfLine();
                    DoUpdateSelection(oldLocation, isShiftDown);
                    e.Handled = true;
                }
                break;            

            case Key.Right:
                if (!CanResize && IsOverflow(oldLocation.ByteIndex + 1) && oldLocation.BitIndex == 0)
                {
                    if (IsCyclic)
                    {
                        Caret.GoToStartOfLine();
                        DoUpdateSelection(oldLocation, isShiftDown);
                    }
                    e.Handled = true;
                }                                             
                break;

            case Key.Down:
                if (!CanResize)
                {
                    if (IsCyclic)
                    {
                        Caret.GoToEndOfLine();
                        DoUpdateSelection(oldLocation, isShiftDown);
                    }
                    e.Handled = true;
                }              
                break;

            case Key.PageDown:
                if (!CanResize)
                {
                    if (IsCyclic)
                    {
                        Caret.GoToEndOfLine();
                        DoUpdateSelection(oldLocation, isShiftDown);
                    }
                    e.Handled = true;
                }               
                break;

            case Key.Insert:
                Caret.Mode = Caret.Mode == EditingMode.Overwrite && CanResize == true
                    ? EditingMode.Insert
                    : EditingMode.Overwrite;
                e.Handled = true;
                break;

            case Key.Delete:
                Delete();
                e.Handled = true;
                break;

            case Key.Back:
                Backspace();
                e.Handled = true;
                break;
        }
    }

    
    /// <summary>
    /// Deletes the currently selected bytes from the document.
    /// </summary>
    public new void Delete()
    {
        if (Caret.PrimaryColumn is not { } column)
            return;

        if (Document is not { CanRemove: true } document)
            return;

        var selectionRange = Selection.Range;
        if (!CanResize)
        {
            if (selectionRange.ByteLength > 1)
                FillSelection(FillChar);
            else
            {
                var location = Caret.Location;
                if (Caret.PrimaryColumn.HandleTextInput(ref location, FillChar, EditingMode.Overwrite))
                    Caret.Location = location;
            }
            return;
        }
        else
            document.RemoveBytes(selectionRange.Start.ByteIndex, selectionRange.ByteLength);

        Caret.Location = new BitLocation(selectionRange.Start.ByteIndex, column.FirstBitIndex);
        Selection.Range = Caret.Location.ToSingleByteRange();
        this.SetPropertyValue<BitLocation?>("_selectionAnchorPoint", null);
    }

    /// <summary>
    /// Deletes the currently selected bytes and the previous byte from the document.
    /// </summary>
    public new void Backspace()
    {
        if (Caret.PrimaryColumn is not { } column)
            return;

        if (Document is not { CanRemove: true } document)
            return;

        var selectionRange = Selection.Range;
        if (!CanResize)
        {
            if (selectionRange.ByteLength > 1)
            {
                FillSelection(FillChar);
                return;
            }
            else
            {
                var _ = Caret.Location;
                if (!Caret.PrimaryColumn.HandleTextInput(ref _, FillChar, EditingMode.Overwrite))
                    return;
            }
        }       

        if (selectionRange.ByteLength <= 1)
        {
            if (Caret.Location.BitIndex == column.FirstBitIndex)
            {
                // If caret is at the left-most cell of a byte, it is more intuitive to have it remove the previous byte.
                // In this case, we can only perform the deletion if we're not at the beginning of the document.
                if (selectionRange.Start.ByteIndex != 0)
                {
                    if (CanResize)
                    {
                        document.RemoveBytes(selectionRange.Start.ByteIndex - 1, 1);
                        Caret.Location = new BitLocation(selectionRange.Start.ByteIndex - 1, column.FirstBitIndex);
                    }
                    else
                        Caret.Location = new BitLocation(selectionRange.Start.ByteIndex - 1, 0);
                }
            }
            else
            {
                // If caret is not at a left-most cell of a byte, it is more intuitive to have it remove the current byte.
                if (CanResize)
                    document.RemoveBytes(selectionRange.Start.ByteIndex, 1);

                Caret.Location = selectionRange.Start.ByteIndex == 0
                    ? new BitLocation(0, column.FirstBitIndex)
                    : new BitLocation(selectionRange.Start.ByteIndex, column.FirstBitIndex);
            }
        }
        else
        {
            // Otherwise, simply treat as a normal delete.
            if (CanResize)
                document.RemoveBytes(selectionRange.Start.ByteIndex, selectionRange.ByteLength);

            Caret.Location = new BitLocation(selectionRange.Start.ByteIndex, column.FirstBitIndex);
        }

        Selection.Range = Caret.Location.ToSingleByteRange();
        this.SetPropertyValue<BitLocation?>("_selectionAnchorPoint", null);
    }
   
    private bool IsOverflow(ulong byteIndex) => 
        !CanResize && Document != null && byteIndex >= Document.ValidRanges.EnclosingRange.ByteLength;

    private void FillSelection(string fill)
    {
        if (string.IsNullOrEmpty(fill))
            return;

        if (Caret.PrimaryColumn is not { } column)
            return;

        if (Document is not { IsReadOnly: false} document)
            return;

        var charToFill = fill[0];
        var selectionRange = Selection.Range;
        var buffer = new byte[selectionRange.ByteLength];

        Array.Fill(buffer, Convert.ToByte($"{charToFill}{charToFill}", 16));
        document.WriteBytes(selectionRange.Start.ByteIndex, buffer);

        Caret.Location = new BitLocation(selectionRange.Start.ByteIndex, column.FirstBitIndex);
    }

    private void DoUpdateSelection(BitLocation from, bool expand)
    {
        this.Call("UpdateSelection", [typeof(BitLocation), typeof(bool)], from, expand);
    }

    /// <inheritdoc />
    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        base.OnGotFocus(e);
        HexView.Focus();
        e.Handled = true;
    }
}