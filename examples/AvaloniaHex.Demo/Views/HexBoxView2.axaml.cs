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
    public partial class HexBoxView2 : UserControl
    {
        public HexBoxView2()
        {
            InitializeComponent();
            ConfigureContextMenu();

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
            HexBoxExt.HexView.LineTransformers.Add(_changesHighlighter);
            HexBoxExt.HexView.LineTransformers.Add(_invalidRangesHighlighter);

            // Divide each 8 bytes with a dashed line and separate colors.
            var layer = HexBoxExt.HexView.Layers.Get<CellGroupsLayer>();
            layer.BytesPerGroup = 8;
            layer.Backgrounds.Add(new SolidColorBrush(Colors.Gray, 0.1D));
            layer.Backgrounds.Add(null);
            layer.Border = new Pen(Brushes.Gray, dashStyle: DashStyle.Dash);

            HexBoxExt.DocumentChanged += HexBoxOnDocumentChanged;
            HexBoxExt.Selection.RangeChanged += SelectionOnRangeChanged;
            HexBoxExt.Caret.ModeChanged += CaretOnModeChanged;
            HexBoxExt.Caret.LocationChanged += CaretOnLocationChanged;

            this.GetObservable(TextProperty)
              .Subscribe(x => UpdateText(x));

            this.GetObservable(TextProperty)
                .Subscribe(_ => RaisePropertyChanged(HexProperty, null, Hex));

            this.GetObservable(BytesPerLineProperty)
                .Where(x => x > 0)
                .Subscribe(x => HexBoxExt.HexView.BytesPerLine = (int?)x);

            this.GetObservable(BytesNumProperty)
                .Subscribe(x =>
                {
                    HexBoxExt.CanResize = x == 0;
                    UpdateBytesNum(x);
                });

            this.GetObservable(IsReadOnlyProperty)
                .Where(x => _document != null)
                .Subscribe(x => _document!.IsReadOnly = x);

            this.GetObservable(IsCyclicProperty)
              .Subscribe(x => HexBoxExt.IsCyclic = x);

            this.GetObservable(IsLabelModeVisibleProperty)
                .Subscribe(_ => UpdateLabels());
            this.GetObservable(IsLabelBytesVisibleProperty)
               .Subscribe(_ => UpdateLabels());
            this.GetObservable(IsMenuVisibleProperty)
                .Subscribe(x => ContextMenu.IsVisible = x);

            this.GetObservable(IsOffsetColumnVisibleProperty)
                .Subscribe(x => ToggleColumn<OffsetColumn>(x));
            this.GetObservable(IsHexColumnVisibleProperty)
                .Subscribe(x => ToggleColumn<HexColumn>(x));
            this.GetObservable(IsBinaryColumnVisibleProperty)
                .Subscribe(x => ToggleColumn<BinaryColumn>(x));
            this.GetObservable(IsAsciiColumnVisibleProperty)
                .Subscribe(x => ToggleColumn<AsciiColumn>(x));
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

        public static readonly StyledProperty<uint> BytesPerLineProperty = AvaloniaProperty.Register<HexBoxView, uint>(nameof(BytesPerLine), 8);
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

        public static readonly StyledProperty<bool> IsLabelBytesVisibleProperty =
            AvaloniaProperty.Register<HexBoxView, bool>(nameof(IsLabelBytesVisible), false);
        public bool IsLabelBytesVisible
        {
            get => GetValue(IsLabelBytesVisibleProperty);
            set => SetValue(IsLabelBytesVisibleProperty, value);
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
            HexBoxExt.Document = new DynamicBinaryDocument();
            HexBoxExt.Caret.Mode = EditingMode.Insert;

            // Create the document first!
            UpdateText(Text);

            LabelBytes.FontSize = _labelsFontSize;
            LabelMode.FontSize = _labelsFontSize;
        }

        private void UpdateText(string hex)
        {
            _document = new DynamicBinaryDocument(Convert.FromHexString((hex ?? string.Empty)));
            HexBoxExt.HexView.Document = _document;
        }

        /// <summary>
        /// Extend or shrink the docuemnt data
        /// </summary>
        /// <param name="bytesNum"></param>
        private void UpdateBytesNum(uint? _bytesNum)
        {
            if((_bytesNum ?? 0) == 0 || _document == null || _document.Length == 0)
                return;

            var bytesNum = _bytesNum!.Value;
            if (bytesNum > _document.Length)
            {
                var padding = new byte[bytesNum - _document.Length];
                Array.Fill(padding, Convert.ToByte(HexBoxExt.FillChar.ToString(), 16));
                _document.InsertBytes(_document.Length, padding);
            }
            else if (bytesNum < _document.Length)
            {
                _document.RemoveBytes(bytesNum - 1, _document.Length - bytesNum);
            }
            UpdateLabels();
        }


        private void ToggleColumn<TColumn>(bool isVisible)
            where TColumn : Column
        {
            var column = HexBoxExt.Columns.Get<TColumn>();
            column.IsVisible = isVisible;
            column.InvalidateVisual();
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
            Debug.Print($"HexBox: {HexBoxExt.Width}, {HexBoxExt.Height}");

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
            LabelBytes.IsVisible = IsLabelBytesVisible;
            if (IsLabelBytesVisible)
            {
                if (HexBoxExt.Selection.Range.ByteLength > 1)
                    LabelBytes.Text = $"Selected {HexBoxExt.Selection.Range.ByteLength} bytes [{HexBoxExt.Selection.Range.Start.ByteIndex + 1} - {HexBoxExt.Selection.Range.End.ByteIndex}]";
                else
                    LabelBytes.Text = $"Byte {HexBoxExt.Caret.Location.ByteIndex + 1}/{Math.Max(HexBoxExt.Document!.Length, HexBoxExt.Caret.Location.ByteIndex + 1)}";
            }

            LabelMode.IsVisible = IsLabelModeVisible;
            if (IsLabelModeVisible)
                LabelMode.Text = HexBoxExt.Caret.Mode == EditingMode.Insert ? "INS" : "OVR";
        }


        private void ConfigureContextMenu()
        {
            var checkBoxes = new CheckBox[]
            {
                MenuShowOffset, MenuShowHex, MenuShowBinary, MenuShowAscii,
                MenuShowMode, MenuShowPosition
            };

            foreach (var cb in checkBoxes)
                cb.IsCheckedChanged += OnMenuCheckBoxChanged;

            MenuShowOffset.IsChecked = IsOffsetColumnVisible;
            MenuShowHex.IsChecked = IsHexColumnVisible;
            MenuShowBinary.IsChecked = IsBinaryColumnVisible;
            MenuShowAscii.IsChecked = IsAsciiColumnVisible;
            MenuShowMode.IsChecked = IsLabelModeVisible;
            MenuShowPosition.IsChecked = IsLabelBytesVisible;

            MenuBytesPerLine.PropertyChanged += OnMenuBytesPerLineChanged;
            MenuBytesPerLine.Value = BytesPerLine;
        }

        private void OnMenuCheckBoxChanged(object? sender, RoutedEventArgs e)
        {           
            if (sender is CheckBox checkBox)
            {
                var name = checkBox.Name;
                var isChecked = checkBox.IsChecked ?? false;

                switch (name)
                {
                    case "MenuShowOffset":
                        IsOffsetColumnVisible = isChecked;

                        break;

                    case "MenuShowHex":
                        IsHexColumnVisible = isChecked;
                        break;

                    case "MenuShowBinary":
                        IsBinaryColumnVisible = isChecked;
                        break;

                    case "MenuShowAscii":
                        IsAsciiColumnVisible = isChecked;
                        break;

                    case "MenuShowMode":
                        LabelMode.IsVisible = isChecked;
                        break;

                    case "MenuShowPosition":
                        LabelBytes.IsVisible = isChecked;
                        break;

                    default:
                        break;
                }
            }
        }

        private void OnMenuBytesPerLineChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (sender is NumericUpDown numericUpDown && e.Property.Name == "Value")
                BytesPerLine = (uint)numericUpDown.Value!;
            }
        }
}