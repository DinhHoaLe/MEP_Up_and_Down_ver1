using Autodesk.Revit.UI;
using MEP_Up_and_Down.UpDownTool.DuctCutEvent;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MEP_Up_and_Down.UpDownTool
{
    public partial class UpAndDown : Window
    {
        private readonly MEPDuctCutEvent handler;
        private readonly ExternalEvent externalEvent;
        private bool ready, busy;
        public bool IsClosed { get; private set; }
        private string Kind => (tabControl.SelectedItem as TabItem)?.Header.ToString() ?? "Duct";
        public UpAndDown()
        {
            InitializeComponent();
            cbbOption.ItemsSource = new[] { "Cut Up", "Cut Down", "Move Up", "Move Down" };
            cbbAngle.ItemsSource = new[] { "45°", "90°" };
            cbbOption.SelectedIndex = cbbAngle.SelectedIndex = 0;
            handler = new MEPDuctCutEvent { window = this };
            externalEvent = ExternalEvent.Create(handler);
            ready = true;
            UpdatePreview();
        }
        private void SelectionChanged(object sender, SelectionChangedEventArgs e) { if (ready) UpdatePreview(); }
        private void OffsetChanged(object sender, TextChangedEventArgs e) { if (ready) UpdatePreview(); }
        private void UpdatePreview()
        {
            string option = cbbOption.SelectedItem.ToString();
            bool move = option.StartsWith("Move"), up = option.EndsWith("Up"), diagonal = cbbAngle.SelectedIndex == 0;
            cbbAngle.IsEnabled = !move && !busy;
            previewTitle.Text = Kind + " · " + option + (move ? "" : " · " + cbbAngle.SelectedItem);
            previewLegend.Text = "Dashed = existing route · Color = result · Arrow = vertical offset";
            hint.Text = move ? "Select elements and connected fittings, then Finish. Move rolls back if existing connections would be lost."
                : "Pick two points on one horizontal element. Compatible elbow fittings must be configured. Preview is schematic.";
            img.Source = MEPSymbols.Create(Kind, option, diagonal, tbOffset.Text);
        }
        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (busy) return;
            if (!double.TryParse(tbOffset.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double value)
                || double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
            { SetStatus("Enter a finite positive offset in millimeters."); tbOffset.Focus(); tbOffset.SelectAll(); return; }
            handler.Request = new MEPRequest(Kind, cbbOption.SelectedItem.ToString(), cbbAngle.SelectedIndex == 0, value);
            var result = externalEvent.Raise();
            if (result != ExternalEventRequest.Accepted) { handler.Request = null; SetStatus("Revit is busy. Try again."); return; }
            busy = true;
            btnOk.IsEnabled = btnCancel.IsEnabled = tabControl.IsEnabled = cbbOption.IsEnabled = tbOffset.IsEnabled = false;
            cbbAngle.IsEnabled = false;
            SetStatus("Waiting for Revit selection…");
        }
        public void SetStatus(string text) { status.Text = text; }
        public void CompleteRequest()
        {
            busy = false;
            btnOk.IsEnabled = btnCancel.IsEnabled = tabControl.IsEnabled = cbbOption.IsEnabled = tbOffset.IsEnabled = true;
            UpdatePreview();
            if (IsClosed) externalEvent.Dispose();
        }
        private void btnCancel_Click(object sender, RoutedEventArgs e) { Close(); }
        private void Minimize_Click(object sender, RoutedEventArgs e) { WindowState = WindowState.Minimized; }
        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (maximizeIcon == null) return;
            bool maximized = WindowState == WindowState.Maximized;
            maximizeIcon.Data = Geometry.Parse(maximized
                ? "M3,1 L11,1 11,8 M1,3 L9,3 9,11 1,11 Z" : "M1,1 L11,1 11,10 1,10 Z");
            maximizeButton.ToolTip = maximized ? "Restore" : "Maximize";
            System.Windows.Automation.AutomationProperties.SetName(maximizeButton, maximized ? "Restore" : "Maximize");
            windowBorder.Padding = maximized ? new Thickness(6) : new Thickness(0);
        }
        private void Window_Closed(object sender, EventArgs e) { IsClosed = true; if (!busy) externalEvent.Dispose(); }
    }
}
