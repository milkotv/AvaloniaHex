using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaHex.Document;
using AvaloniaHex.Editing;
using AvaloniaHex.Rendering;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Windows.Input;

namespace AvaloniaHex.Controls
{
    public class HexBox2  : HexEditor
    {
        private readonly RangesHighlighter _changesHighlighter;
        private readonly ZeroesHighlighter _zeroesHighlighter;
        private readonly InvalidRangesHighlighter _invalidRangesHighlighter;
        private DynamicBinaryDocument? _document;

        #region StyledProperties
        public static readonly StyledProperty<ulong> BytesProperty = AvaloniaProperty.Register<HexBox2, ulong>(nameof(Bytes));
        public static readonly StyledProperty<ulong> ColumnsNumProperty = AvaloniaProperty.Register<HexBox2, ulong>(nameof(ColumnsNum), 0);
        public static readonly StyledProperty<bool> IsReadOnlyProperty = AvaloniaProperty.Register<HexBox2, bool>(nameof(IsReadOnly));

        public static readonly StyledProperty<ulong> CountProperty = AvaloniaProperty.Register<HexBox2, ulong>(nameof(Count));
        public static readonly StyledProperty<ulong> PositionProperty = AvaloniaProperty.Register<HexBox2, ulong>(nameof(Position));
        public static readonly StyledProperty<ulong> SelectionLengthProperty = AvaloniaProperty.Register<HexBox2, ulong>(nameof(SelectionLength));
        public static readonly StyledProperty<ulong> LengthProperty = AvaloniaProperty.Register<HexBox2, ulong>(nameof(Length));
        public static readonly StyledProperty<bool> IsOverwriteProperty = AvaloniaProperty.Register<HexBox2, bool>(nameof(IsOverwrite));
        public static readonly StyledProperty<bool> NeedsFocusProperty = AvaloniaProperty.Register<HexBox2, bool>(nameof(NeedsFocus));
        public static readonly StyledProperty<bool> IsValidProperty = AvaloniaProperty.Register<HexBox2, bool>(nameof(IsValid));
        public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<HexBox2, string>(nameof(Text));
        public static readonly StyledProperty<string> HexTextProperty = AvaloniaProperty.Register<HexBox2, string>(nameof(HexText));
        public static readonly StyledProperty<HexBox2> ThisProperty = AvaloniaProperty.Register<HexBox2, HexBox2>(nameof(This));
        #endregion

        #region Properties
        public ulong Bytes
        {
            get => GetValue(BytesProperty);
            set => SetValue(BytesProperty, value);
        }

        public ulong ColumnsNum
        {
            get => GetValue(ColumnsNumProperty);
            set => SetValue(ColumnsNumProperty, value);
        }

        public ulong Count
        {
            get => GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }

        public ulong Position
        {
            get => GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        public ulong SelectionLength
        {
            get => GetValue(SelectionLengthProperty);
            set => SetValue(SelectionLengthProperty, value);
        }

        public ulong Length
        {
            get => GetValue(LengthProperty);
            set => SetValue(LengthProperty, value);
        }

        public bool IsOverwrite
        {
            get => GetValue(IsOverwriteProperty);
            set => SetValue(IsOverwriteProperty, value);
        }

        public bool NeedsFocus
        {
            get => GetValue(NeedsFocusProperty);
            set => SetValue(NeedsFocusProperty, value);
        }

        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string HexText
        {
            get => GetValue(HexTextProperty);
            set => SetValue(HexTextProperty, value);
        }

        public HexBox2 This
        {
            get => GetValue(ThisProperty);
            set => SetValue(ThisProperty, value);
        }

        public bool IsValid
        {
            get => GetValue(IsValidProperty);
            set => SetValue(IsValidProperty, value);
        }       

        public bool IsReadOnly
        {
            get => GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        protected override Type StyleKeyOverride => typeof(TextBox);

        #endregion

        #region Properties for the template 
        // Define StyledProperties for controlling column visibility
        public static readonly StyledProperty<bool> IsOffsetColumnVisibleProperty =
            AvaloniaProperty.Register<HexBox2, bool>(nameof(IsOffsetColumnVisible), false);

        public static readonly StyledProperty<bool> IsHexColumnVisibleProperty =
            AvaloniaProperty.Register<HexBox2, bool>(nameof(IsHexColumnVisible), true);

        public static readonly StyledProperty<bool> IsBinaryColumnVisibleProperty =
            AvaloniaProperty.Register<HexBox2, bool>(nameof(IsBinaryColumnVisible), false);

        public static readonly StyledProperty<bool> IsAsciiColumnVisibleProperty =
            AvaloniaProperty.Register<HexBox2, bool>(nameof(IsAsciiColumnVisible), false);

        public static readonly StyledProperty<ICommand> CopyCommandProperty =
           AvaloniaProperty.Register<HexBox2, ICommand>(nameof(CopyCommand));

        public bool IsOffsetColumnVisible
        {
            get => GetValue(IsOffsetColumnVisibleProperty);
            set => SetValue(IsOffsetColumnVisibleProperty, value);
        }

        public bool IsHexColumnVisible
        {
            get => GetValue(IsHexColumnVisibleProperty);
            set => SetValue(IsHexColumnVisibleProperty, value);
        }

        public bool IsBinaryColumnVisible
        {
            get => GetValue(IsBinaryColumnVisibleProperty);
            set => SetValue(IsBinaryColumnVisibleProperty, value);
        }

        public bool IsAsciiColumnVisible
        {
            get => GetValue(IsAsciiColumnVisibleProperty);
            set => SetValue(IsAsciiColumnVisibleProperty, value);
        }

        public ICommand CopyCommand
        {
            get => GetValue(CopyCommandProperty);
            set => SetValue(CopyCommandProperty, value);
        }

        #endregion

        public HexBox2()
        {
            UpdateColumns();
            UpdateDocument(string.Empty);

            // Create custom highlighters
            _zeroesHighlighter = new ZeroesHighlighter
            {
                Foreground = new SolidColorBrush(new Color(255, 75, 75, 75)),
            };
            _changesHighlighter = new RangesHighlighter
            {
                Foreground = Brushes.Red
            };
            _invalidRangesHighlighter = new InvalidRangesHighlighter
            {
                Foreground = new SolidColorBrush(Colors.Gray, 0.5)
            };

            // Enable the highliters
            HexView.LineTransformers.Add(_changesHighlighter);
            HexView.LineTransformers.Add(_invalidRangesHighlighter);

            // Divide each 8 bytes with a dashed line and separate colors.
            var layer = HexView.Layers.Get<CellGroupsLayer>();
            layer.BytesPerGroup = 8;
            layer.Backgrounds.Add(new SolidColorBrush(Colors.Gray, 0.1D));
            layer.Backgrounds.Add(null);
            layer.Border = new Pen(Brushes.Gray, dashStyle: DashStyle.Dash);

            // Capture events
            DocumentChanged += HexEditorOnDocumentChanged;
            Selection.RangeChanged += SelectionOnRangeChanged;
            Caret.ModeChanged += CaretOnModeChanged;

            CopyCommand = ReactiveCommand.Create(ExecuteCopy);

            // Setup the observables
            this.GetObservable(TextProperty).Subscribe(x => UpdateDocument(x));
            this.GetObservable(BytesProperty).Subscribe(x => UpdateBytes(x));
            this.GetObservable(IsReadOnlyProperty).Subscribe(x => UpdateIsReadOnly(x));
            this.GetObservable(IsOffsetColumnVisibleProperty).Subscribe(_ => UpdateColumns());
            this.GetObservable(IsHexColumnVisibleProperty).Subscribe(_ => UpdateColumns());
            this.GetObservable(IsBinaryColumnVisibleProperty).Subscribe(_ => UpdateColumns());
            this.GetObservable(IsAsciiColumnVisibleProperty).Subscribe(_ => UpdateColumns());
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            // The order matters!
            UpdateColumns();
            UpdateDocument(Text);
            UpdateBytes(Bytes);
            UpdateIsReadOnly(IsReadOnly);

            Caret.Mode = EditingMode.Insert;
        }

        private void UpdateColumns()
        {
            Columns.Clear();
            Columns.Add(new OffsetColumn { IsVisible = IsOffsetColumnVisible });
            Columns.Add(new HexColumn { IsVisible = IsHexColumnVisible });
            Columns.Add(new BinaryColumn { IsVisible = IsBinaryColumnVisible });
            Columns.Add(new AsciiColumn { IsVisible = IsAsciiColumnVisible });
        }

        private void UpdateDocument(string hex)
        {
            HexView.Document = new DynamicBinaryDocument(Convert.FromHexString((hex ?? "")));
            _document = HexView.Document as DynamicBinaryDocument;
        }

        private void UpdateBytes(ulong bytesNum)
        {
            // Extend if needed
            if (bytesNum > _document!.Length)
            {
                var padding = new byte[bytesNum - _document.Length];
                Array.Fill(padding, Convert.ToByte(FillChar.ToString(), 16));
                _document.InsertBytes(_document.Length, padding);
            }
            // Shrink if needed
            else if (bytesNum < _document.Length)
            {
                _document.RemoveBytes(_document.Length - bytesNum - 1, bytesNum);
            }
        }


        private void UpdateIsReadOnly(bool isReadOnly)
        {
            _document!.IsReadOnly = isReadOnly;
        }         

        private void SelectionOnRangeChanged(object? _, EventArgs e)
        {
            var status = Selection.Range.ToString();
            Debug.Print($"SelectionOnRangeChanged: {status}");

        }

        private void CaretOnModeChanged(object? _, EventArgs e)
        {
            var status = Caret.Mode.ToString();
            Debug.Print($"CaretOnModeChanged: {status}");
        }

        private void HexEditorOnDocumentChanged(object? _, DocumentChangedEventArgs e)
        {
            _changesHighlighter.Ranges.Clear();
            if (e.Old is not null)
                e.Old.Changed -= DocumentOnChanged;
            if (e.New is not null)
                e.New.Changed += DocumentOnChanged;
        }

        private void DocumentOnChanged(object? _, BinaryDocumentChange change)
        {
            switch (change.Type)
            {
                case BinaryDocumentChangeType.Modify:
                    _changesHighlighter.Ranges.Add(change.AffectedRange);
                    break;

                case BinaryDocumentChangeType.Insert:
                case BinaryDocumentChangeType.Remove:
                    _changesHighlighter.Ranges.Add(change.AffectedRange.ExtendTo(Document!.ValidRanges.EnclosingRange.End));
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"Unknown action: {change.Type}");
            }
        }

        private void ExecuteCopy()
        {
            // Implement your copy logic here
            Console.WriteLine("Copy Command Executed!");
        }

    }
}
