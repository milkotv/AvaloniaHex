using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaHex.Document;
using AvaloniaHex.Editing;
using AvaloniaHex.Rendering;
using System;
using System.Diagnostics;
using System.Reactive.Linq;

namespace AvaloniaHex.Demo.Views
{
    public partial class HexBoxView : UserControl
    {
        public HexBoxView()
        {
            InitializeComponent();

            // Create some custom highlighters.
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

            // Enable the changes highlighter.
            HexBox.HexView.LineTransformers.Add(_changesHighlighter);
            HexBox.HexView.LineTransformers.Add(_invalidRangesHighlighter);

            // Divide each 8 bytes with a dashed line and separate colors.
            var layer = HexBox.HexView.Layers.Get<CellGroupsLayer>();
            layer.BytesPerGroup = 8;
            layer.Backgrounds.Add(new SolidColorBrush(Colors.Gray, 0.1D));
            layer.Backgrounds.Add(null);
            layer.Border = new Pen(Brushes.Gray, dashStyle: DashStyle.Dash);

            HexBox.DocumentChanged += HexBoxOnDocumentChanged;
            HexBox.Selection.RangeChanged += SelectionOnRangeChanged;
            HexBox.Caret.ModeChanged += CaretOnModeChanged;
            HexBox.Caret.LocationChanged += CaretOnLocationChanged;

            this.GetObservable(TextProperty)
              .Subscribe(x => UpdateText(x));

            this.GetObservable(TextProperty)
                .Subscribe(_ => RaisePropertyChanged(HexProperty, null, Hex));

            this.GetObservable(BytesPerLineProperty)
                .Subscribe(x => HexBox.HexView.BytesPerLine = (int?)x);

            this.GetObservable(BytesNumProperty)
                .Subscribe(x => UpdateBytesNum(x));

            this.GetObservable(IsReadOnlyProperty)
                .Subscribe(x => _document!.IsReadOnly = x);

            this.GetObservable(IsStatusLineVisibleProperty)
                .Subscribe(_ => UpdateLabels());

            this.GetObservable(IsOffsetColumnVisibleProperty)
                .Subscribe(_ => ToggleColumn<OffsetColumn>());
            this.GetObservable(IsHexColumnVisibleProperty)
                .Subscribe(_ => ToggleColumn<HexColumn>());
            this.GetObservable(IsBinaryColumnVisibleProperty)
                .Subscribe(_ => ToggleColumn<BinaryColumn>());
            this.GetObservable(IsAsciiColumnVisibleProperty)
                .Subscribe(_ => ToggleColumn<AsciiColumn>());
        }

        #region Fields
        private readonly int _labelsFontSize = 10;
        private readonly RangesHighlighter _changesHighlighter;
        private readonly ZeroesHighlighter _zeroesHighlighter;
        private readonly InvalidRangesHighlighter _invalidRangesHighlighter;
        private DynamicBinaryDocument? _document;
        #endregion

        #region Properties
        public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<HexBoxView, string>(nameof(Text), string.Empty);
        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DirectProperty<HexBoxView, string?> HexProperty = AvaloniaProperty.RegisterDirect<HexBoxView, string?>(nameof(Hex), o => o.Hex);
        public string Hex =>
            Text.Replace(" ", string.Empty) ?? string.Empty;

        public static readonly StyledProperty<uint> BytesPerLineProperty = AvaloniaProperty.Register<HexBoxView, uint>(nameof(BytesPerLine), 0);
        public uint BytesPerLine
        {
            get => GetValue(BytesPerLineProperty);
            set => SetValue(BytesPerLineProperty, value);
        }

        public static readonly StyledProperty<uint?> BytesNumProperty = AvaloniaProperty.Register<HexBoxView, uint?>(nameof(BytesNum), null);
        public uint? BytesNum
        {
            get => GetValue(BytesNumProperty);
            set => SetValue(BytesNumProperty, value);
        }

        public static readonly StyledProperty<bool> IsReadOnlyProperty = AvaloniaProperty.Register<HexBoxView, bool>(nameof(IsReadOnly));
        public bool IsReadOnly
        {
            get => GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public static readonly StyledProperty<bool> IsOffsetColumnVisibleProperty =
            AvaloniaProperty.Register<HexBoxView, bool>(nameof(IsOffsetColumnVisible), false);
        public bool IsOffsetColumnVisible
        {
            get => GetValue(IsOffsetColumnVisibleProperty);
            set => SetValue(IsOffsetColumnVisibleProperty, value);
        }

        public static readonly StyledProperty<bool> IsHexColumnVisibleProperty =
            AvaloniaProperty.Register<HexBoxView, bool>(nameof(IsHexColumnVisible), true);
        public bool IsHexColumnVisible
        {
            get => GetValue(IsHexColumnVisibleProperty);
            set => SetValue(IsHexColumnVisibleProperty, value);
        }

        public static readonly StyledProperty<bool> IsBinaryColumnVisibleProperty =
            AvaloniaProperty.Register<HexBoxView, bool>(nameof(IsBinaryColumnVisible), false);
        public bool IsBinaryColumnVisible
        {
            get => GetValue(IsBinaryColumnVisibleProperty);
            set => SetValue(IsBinaryColumnVisibleProperty, value);
        }

        public static readonly StyledProperty<bool> IsAsciiColumnVisibleProperty =
            AvaloniaProperty.Register<HexBoxView, bool>(nameof(IsAsciiColumnVisible), false);
        public bool IsAsciiColumnVisible
        {
            get => GetValue(IsAsciiColumnVisibleProperty);
            set => SetValue(IsAsciiColumnVisibleProperty, value);
        }

        public static readonly StyledProperty<bool> IsStatusLineVisibleProperty =
            AvaloniaProperty.Register<HexBoxView, bool>(nameof(IsStatusLineVisible), false);
        public bool IsStatusLineVisible
        {
            get => GetValue(IsStatusLineVisibleProperty);
            set => SetValue(IsStatusLineVisibleProperty, value);
        }

        #endregion


        /// <summary>
        /// When the view is loaded
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            try
            {
                // Create the document first!
                UpdateText(Text);

                // Add the columns second!
                HexBox.Columns.Clear();
                HexBox.Columns.Add(new OffsetColumn { IsVisible = IsOffsetColumnVisible });
                HexBox.Columns.Add(new HexColumn { IsVisible = IsHexColumnVisible });
                HexBox.Columns.Add(new BinaryColumn { IsVisible = IsBinaryColumnVisible });
                HexBox.Columns.Add(new AsciiColumn { IsVisible = IsAsciiColumnVisible });

                HexBox.Caret.Mode = EditingMode.Insert;

                OffsetLabel.FontSize = _labelsFontSize;
                SelectionLabel.FontSize = _labelsFontSize;
                ModeLabel.FontSize = _labelsFontSize;
            }
            catch (Exception ex)
            {
                Debug.Print($"Failed to create DynamicBinaryDocument ({ex.Message})");
            }
        }

        private void UpdateText(string hex)
        {
            _document = new DynamicBinaryDocument(Convert.FromHexString((hex ?? string.Empty)));
            HexBox.HexView.Document = _document;
        }

        /// <summary>
        /// Extend or shrink the docuemnt data
        /// </summary>
        /// <param name="bytesNum"></param>
        private void UpdateBytesNum(uint? _bytesNum)
        {
            if((_bytesNum ?? 0) == 0)
                return;

            var bytesNum = _bytesNum!.Value;
            if (bytesNum > _document!.Length)
            {
                var padding = new byte[bytesNum - _document.Length];
                Array.Fill(padding, Convert.ToByte(HexBox.FillChar.ToString(), 16));
                _document.InsertBytes(_document.Length, padding);
            }
            else if (bytesNum < _document.Length)
            {
                _document.RemoveBytes(bytesNum - 1, _document.Length - bytesNum);
            }
            UpdateLabels();
        }


        private void ToggleColumn<TColumn>()
            where TColumn : Column
        {
            var column = HexBox.Columns.Get<TColumn>();
            column.IsVisible = !column.IsVisible;
        }

        private void HexBoxOnDocumentChanged(object? sender, DocumentChangedEventArgs e)
        {
            _changesHighlighter.Ranges.Clear();
            if (e.Old is not null)
                e.Old.Changed -= DocumentOnChanged;
            if (e.New is not null)
                e.New.Changed += DocumentOnChanged;

            UpdateLabels();
        }

        private void DocumentOnChanged(object? sender, BinaryDocumentChange change)
        {
            Debug.Print($"HexBox: {HexBox.Width}, {HexBox.Height}");

            var doc = (sender as IBinaryDocument)!;
            switch (change.Type)
            {
                case BinaryDocumentChangeType.Modify:
                    _changesHighlighter.Ranges.Add(change.AffectedRange);
                    break;

                case BinaryDocumentChangeType.Insert:
                case BinaryDocumentChangeType.Remove:
                    _changesHighlighter.Ranges.Add(change.AffectedRange.ExtendTo(doc.ValidRanges.EnclosingRange.End));
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void CaretOnLocationChanged(object? sender, EventArgs e) => UpdateLabels();
        private void SelectionOnRangeChanged(object? sender, EventArgs e) => UpdateLabels();
        private void CaretOnModeChanged(object? sender, EventArgs e) => UpdateLabels();
        private void UpdateLabels()
        {
            StatusLine.IsVisible = IsStatusLineVisible;
            if (IsStatusLineVisible)
            {
                OffsetLabel.Text =
                    $"{HexBox.Caret.Location.ByteIndex + 1}{(BytesNum > 0 ? $"/{HexBox.Document!.Length}" : string.Empty)}";
                SelectionLabel.Text = HexBox.Selection.Range.ByteLength > 1 ?
                    $"Selected {HexBox.Selection.Range.ByteLength} bytes [{HexBox.Selection.Range.Start.ByteIndex}-{HexBox.Selection.Range.End.ByteIndex}]" : string.Empty;
                ModeLabel.Text = HexBox.Caret.Mode == EditingMode.Insert ? "INS" : "OVR";
            }
        }
    }
}