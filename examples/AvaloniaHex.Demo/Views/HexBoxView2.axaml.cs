using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaHex.Document;
using AvaloniaHex.Editing;
using AvaloniaHex.Rendering;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;

namespace AvaloniaHex.Demo.Views
{
    public partial class HexBoxView2 : UserControl
    {
        public HexBoxView2()
        {
            InitializeComponent();

            // Default. Can change if !CanResize
            HexBox.Caret.Mode = EditingMode.Insert;

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
            HexBox.HexView.ScrollInvalidated += OnScrollInvalidated;

            MenuBytesPerLineAuto.PropertyChanged += OnMenuBytesPerLineChanged;
            MenuBytesPerLine.PropertyChanged += OnMenuBytesPerLineChanged;
            
            this.GetObservable(TextProperty)
                .Subscribe(x =>
                {
                    UpdateText(x);
                    RaisePropertyChanged(HexProperty, null, x);
                });

            this.GetObservable(BytesNumProperty)
                .Subscribe(x =>
                {
                    HexBox.CanResize = x == 0;
                    if (!HexBox.CanResize)
                        HexBox.Caret.Mode = EditingMode.Overwrite;
                    UpdateBytesNum(x);
                });

            this.GetObservable(IsReadOnlyProperty)
                .Where(x => _document != null)
                .Subscribe(x => _document!.IsReadOnly = x);

            this.GetObservable(IsCyclicProperty)
              .Subscribe(x => HexBox.IsCyclic = x);

            this.GetObservable(IsLabelModeVisibleProperty)
                .Subscribe(x => LabelMode.IsVisible = x);

            this.GetObservable(IsLabelPositionVisibleProperty)
                .Subscribe(x => LabelPosition.IsVisible = x);

            this.GetObservable(IsMenuVisibleProperty)
                .Subscribe(x => ContextMenu.IsVisible = x);

            this.GetObservable(BytesPerLineProperty)
                .Subscribe(x =>
                {
                    HexBox.HexView.BytesPerLine = x;
                    if (x != null)
                    {
                        MenuBytesPerLine.Value = x;
                        MenuBytesPerLineAuto.IsChecked = false;
                    }
                });

            this.GetObservable(IsOffsetColumnVisibleProperty)
                .Subscribe(x => OffsetColumn.IsVisible = x);

            this.GetObservable(IsHexColumnVisibleProperty)
                .Subscribe(x => HexColumn.IsVisible = x);

            this.GetObservable(IsBinaryColumnVisibleProperty)
                .Subscribe(x => BinaryColumn.IsVisible = x);

            this.GetObservable(IsAsciiColumnVisibleProperty)
                .Subscribe(x => AsciiColumn.IsVisible = x);
        }       

        #region Fields
        private readonly int _labelsFontSize = 10;
        private readonly int _defaultBytesPerLine = 8;
        private readonly RangesHighlighter _changesHighlighter;
        private readonly ZeroesHighlighter _zeroesHighlighter;
        private readonly InvalidRangesHighlighter _invalidRangesHighlighter;
        private DynamicBinaryDocument? _document;
        private Rect? _lineBounds = null;
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

        public static readonly StyledProperty<int?> BytesPerLineProperty = AvaloniaProperty.Register<HexBoxView, int?>(nameof(BytesPerLine), null);
        public int? BytesPerLine
        {
            get => GetValue(BytesPerLineProperty);
            set => SetValue(BytesPerLineProperty, value);
        }

        public static readonly StyledProperty<uint> BytesNumProperty = AvaloniaProperty.Register<HexBoxView, uint>(nameof(BytesNum), 0);
        public uint BytesNum
        {
            get => GetValue(BytesNumProperty);
            set => SetValue(BytesNumProperty, value);
        }

        public static readonly StyledProperty<bool> IsCyclicProperty = AvaloniaProperty.Register<HexBoxView, bool>(nameof(IsCyclic));
        public bool IsCyclic
        {
            get => GetValue(IsCyclicProperty);
            set => SetValue(IsCyclicProperty, value);
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

        public static readonly StyledProperty<bool> IsLabelPositionVisibleProperty =
            AvaloniaProperty.Register<HexBoxView, bool>(nameof(IsLabelPositionVisible), false);
        public bool IsLabelPositionVisible
        {
            get => GetValue(IsLabelPositionVisibleProperty);
            set => SetValue(IsLabelPositionVisibleProperty, value);
        }

        public static readonly StyledProperty<bool> IsLabelModeVisibleProperty =
            AvaloniaProperty.Register<HexBoxView, bool>(nameof(IsLabelModeVisible), false);
        public bool IsLabelModeVisible
        {
            get => GetValue(IsLabelModeVisibleProperty);
            set => SetValue(IsLabelModeVisibleProperty, value);
        }

        public static readonly StyledProperty<bool> IsMenuVisibleProperty =
            AvaloniaProperty.Register<HexBoxView, bool>(nameof(IsMenuVisible), false);
        public bool IsMenuVisible
        {
            get => GetValue(IsMenuVisibleProperty);
            set => SetValue(IsMenuVisibleProperty, value);
        }

        #endregion

        /// <summary>
        /// When the view is loaded
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            HexBox.Document = new DynamicBinaryDocument();

            // Create the document first!
            UpdateText(Text);

            //MenuBytesPerLine.Value = BytesPerLine ?? 8;

            MenuShowOffset.IsChecked = IsOffsetColumnVisible;
            MenuShowHex.IsChecked = IsHexColumnVisible;
            MenuShowBinary.IsChecked = IsBinaryColumnVisible;
            MenuShowAscii.IsChecked = IsAsciiColumnVisible;
            MenuShowMode.IsChecked = IsLabelModeVisible;
            MenuShowPosition.IsChecked = IsLabelPositionVisible;

            LabelPosition.FontSize = _labelsFontSize;
            LabelMode.FontSize = _labelsFontSize;
        }

        private void OnMenuBytesPerLineChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (sender is NumericUpDown numericUpDown && e.Property.Name == "Value" && numericUpDown?.Value != null)
                BytesPerLine = (int)numericUpDown.Value!;
            else if (sender is CheckBox checkBox && e.Property.Name == "IsChecked")
                BytesPerLine = (bool)checkBox.IsChecked! ? null : ((int?)MenuBytesPerLine?.Value ?? _defaultBytesPerLine);
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
            if ((_bytesNum ?? 0) > 0 && _document?.Length > 0)
            {
                var bytesNum = _bytesNum!.Value;
                if (bytesNum > _document.Length)
                {
                    var padding = new byte[bytesNum - _document.Length];
                    Array.Fill(padding, Convert.ToByte(HexBox.FillChar.ToString(), 16));
                    _document.InsertBytes(_document.Length, padding);
                }
                else if (bytesNum < _document.Length)
                {
                    _document.RemoveBytes(bytesNum - 1, _document.Length - bytesNum);
                }
            }
            UpdateLabels();
        }

        private void UpdateLabels([CallerMemberName] string caller = "")
        {
            if (IsLabelPositionVisible)
            {
                if (HexBox.Selection.Range.ByteLength > 1)
                    LabelPosition.Text = $"Selected {HexBox.Selection.Range.ByteLength} bytes [{HexBox.Selection.Range.Start.ByteIndex + 1} - {HexBox.Selection.Range.End.ByteIndex}]";
                else if (HexBox.Selection.Range.ByteLength > 0)
                    LabelPosition.Text = $"Byte {HexBox.Caret.Location.ByteIndex + 1}/{Math.Max(HexBox.Document!.Length, HexBox.Caret.Location.ByteIndex + 1)}";
            }

            if (IsLabelModeVisible)
                LabelMode.Text = HexBox.Caret.Mode == EditingMode.Insert ? "INS" : "OVR";
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

        private void OnScrollInvalidated(object? sender, EventArgs e)
        {
            if (HexBox.IsLoaded)
            {
                if (sender is HexView view)
                {
                    Debug.Print($"Scroll invalidated. Height: {view.Extent.Height}");
                    if (_lineBounds == null && HexBox.HexView.VisualLines.Any())
                        _lineBounds = HexBox.HexView.VisualLines[0].Bounds;

                    HexBox.HexView.Height = Math.Max(view.Extent.Height, 1) * _lineBounds!.Value.Height;
                }
            }
        }

        private void CaretOnLocationChanged(object? sender, EventArgs e) => UpdateLabels();
        private void SelectionOnRangeChanged(object? sender, EventArgs e) => UpdateLabels();
        private void CaretOnModeChanged(object? sender, EventArgs e) => UpdateLabels();
    }
}