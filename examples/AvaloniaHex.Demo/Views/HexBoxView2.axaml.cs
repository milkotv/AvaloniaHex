using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaHex.Document;
using AvaloniaHex.Editing;
using AvaloniaHex.Rendering;
using System;
using System.ComponentModel.DataAnnotations;
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

            HexBox.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;

            HexBox.HexView.GotFocus += (sender, e) =>
            {
                _hasFocus = true;
                UpdateLabels();
            };
            HexBox.HexView.LostFocus += (sender, e) =>
            {
                _hasFocus = false;
                UpdateLabels();
            };

            HexBox.DocumentChanged += HexBoxOnDocumentChanged;
            HexBox.Selection.RangeChanged += SelectionOnRangeChanged;
            HexBox.Caret.ModeChanged += CaretOnModeChanged;
            HexBox.Caret.LocationChanged += CaretOnLocationChanged;

            MenuBytesPerLineAuto.PropertyChanged += OnMenuBytesPerLineChanged;
            MenuBytesPerLine.PropertyChanged += OnMenuBytesPerLineChanged;         
        }      

        #region Fields
        private readonly int _labelsFontSize = 10;
        private readonly int _defaultBytesPerLine = 8;
        private readonly RangesHighlighter _changesHighlighter;
        private readonly ZeroesHighlighter _zeroesHighlighter;
        private readonly InvalidRangesHighlighter _invalidRangesHighlighter;
        private bool _hasFocus = false;
        #endregion

        #region Properties
        public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<HexBoxView, string>(nameof(Text), string.Empty);
        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DirectProperty<HexBoxView, string?> HexProperty = AvaloniaProperty.RegisterDirect<HexBoxView, string?>(nameof(Hex), o => o.Hex);
        public string Hex => Text.Replace(" ", string.Empty) ?? string.Empty;

        public static readonly StyledProperty<string> FillCharProperty = AvaloniaProperty.Register<HexBoxView, string>(nameof(FillChar), "0");
        [RegularExpression("^[0-9a-fA-F]$", ErrorMessage = "FillChar must be 1 HEX character.")]
        public string FillChar
        {
            get => GetValue(FillCharProperty);
            set => SetValue(FillCharProperty, value);
        }        

        public static readonly StyledProperty<int?> BytesPerLineProperty = AvaloniaProperty.Register<HexBoxView, int?>(nameof(BytesPerLine), null);
        public int? BytesPerLine
        {
            get => GetValue(BytesPerLineProperty);
            set => SetValue(BytesPerLineProperty, value);
        }

        public static readonly StyledProperty<int?> BytesNumProperty = AvaloniaProperty.Register<HexBoxView, int?>(nameof(BytesNum), null);
        public int? BytesNum
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
        /// </summary>                                          -
        /// <param name="e"></param>
        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            // Create the document first!
            UpdateDocument();

            MenuShowOffset.IsChecked = IsOffsetColumnVisible;
            MenuShowHex.IsChecked = IsHexColumnVisible;
            MenuShowBinary.IsChecked = IsBinaryColumnVisible;
            MenuShowAscii.IsChecked = IsAsciiColumnVisible;
            MenuShowMode.IsChecked = IsLabelModeVisible;
            MenuShowPosition.IsChecked = IsLabelPositionVisible;

            LabelPosition.FontSize = _labelsFontSize;
            LabelMode.FontSize = _labelsFontSize;
            UpdateLabels();

            SetupSubscriptions();
        }

        private void SetupSubscriptions()
        {
            this.GetObservable(TextProperty)
                .Subscribe(x =>
                {
                    UpdateDocument();
                    RaisePropertyChanged(HexProperty, null, x);
                });

            this.GetObservable(BytesNumProperty)
                .Where(x => HexBox.HexView.Document != null)
                .Subscribe(x =>
                {
                    UpdateDocument();
                    if ((x ?? 0) > 0)
                        HexBox.Caret.Mode = EditingMode.Overwrite;
                });

            this.GetObservable(IsReadOnlyProperty)
                .Where(x => HexBox.HexView.Document != null)
                .Subscribe(x => UpdateDocument());

            this.GetObservable(IsCyclicProperty)
              .Subscribe(x => HexBox.IsCyclic = x);

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

        private void OnMenuBytesPerLineChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (sender is NumericUpDown numericUpDown && e.Property.Name == "Value" && numericUpDown?.Value != null)
                BytesPerLine = (int)numericUpDown.Value!;
            else if (sender is CheckBox checkBox && e.Property.Name == "IsChecked")
                BytesPerLine = (bool)checkBox.IsChecked! ? null : ((int?)MenuBytesPerLine?.Value ?? _defaultBytesPerLine);
        }

        private void UpdateDocument()
        {
            var bytesNum = (BytesNum ?? 0);
            var hex = Text ?? string.Empty;

            // Try padding of shrinking to bytesNum if not even
            if (hex.Length % 2 != 0)
            {
                if(bytesNum == 0)
                    return;
                
                hex = hex.Length < bytesNum ? 
                    hex.PadRight(bytesNum, '0') :
                    hex[..bytesNum];
            }
            var bytes = Convert.FromHexString(hex);
            
            HexBox.HexView.Document = (bytesNum > 0 || IsReadOnly) ?
                new MemoryBinaryDocument(bytes) : new DynamicBinaryDocument(bytes);

            if (!HexBox.CanResize)
                HexBox.Caret.Mode = EditingMode.Overwrite;

            UpdateLabels();
        }

        private void UpdateLabels()
        {
            LabelPosition.IsVisible = _hasFocus;
            LabelMode.IsVisible = _hasFocus;

            if (!_hasFocus)
                return;

            if (IsLabelPositionVisible)
            {
                if (HexBox.Selection.Range.ByteLength > 1)
                    LabelPosition.Text = $"Selected {HexBox.Selection.Range.ByteLength} bytes [{HexBox.Selection.Range.Start.ByteIndex + 1} - {HexBox.Selection.Range.End.ByteIndex}]";
                else 
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
            UpdateLabels();
        }

        private void CaretOnLocationChanged(object? sender, EventArgs e) => UpdateLabels();
        private void SelectionOnRangeChanged(object? sender, EventArgs e) => UpdateLabels();
        private void CaretOnModeChanged(object? sender, EventArgs e) => UpdateLabels();
    }
}