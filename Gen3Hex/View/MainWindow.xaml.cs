﻿using HavenSoft.Gen3Hex.Model;
using HavenSoft.Gen3Hex.ViewModel;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace HavenSoft.Gen3Hex.View {
   public partial class MainWindow {
      public EditorViewModel ViewModel { get; }

      public MainWindow(EditorViewModel viewModel) {
         InitializeComponent();
         ViewModel = viewModel;
         DataContext = viewModel;
      }

      protected override void OnDrop(DragEventArgs e) {
         base.OnDrop(e);

         if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var fileName in files) {
               var data = File.ReadAllBytes(fileName);
               ViewModel.Open.Execute(new LoadedFile(fileName, data));
            }
         }
      }

      protected override void OnClosing(CancelEventArgs e) {
         base.OnClosing(e);
         ViewModel.CloseAll.Execute();
         if (ViewModel.Count != 0) e.Cancel = true;
      }

      private static FrameworkElement GetChild(DependencyObject depObj, string name, object dataContext) {
         for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++) {
            var child = VisualTreeHelper.GetChild(depObj, i);
            var childContext = child.GetValue(DataContextProperty);
            var childName = child.GetValue(NameProperty);
            if (childContext == dataContext && name == childName.ToString()) return (FrameworkElement)child;
            var next = GetChild(child, name, dataContext);
            if (next != null) return next;
         }

         return null;
      }

      #region Tab Mouse Events

      private void TabMouseDown(object sender, MouseButtonEventArgs e) {
         var element = (FrameworkElement)sender;
         if (e.LeftButton != MouseButtonState.Pressed) return;
         if (e.ChangedButton != MouseButton.Left) return;

         element.CaptureMouse();
      }

      /// <summary>
      /// If the mouse has dragged the tab through more than half of the next tab, swap the tabs horizontally.
      /// </summary>
      /// <remarks>
      /// The "more than half through the next tab" metric was chosen to deal with disparity between widths of tabs.
      /// A smaller number would cause tabs to flicker when a narrow tab is dragged past a wide tab.
      /// </remarks>
      private void TabMouseMove(object sender, MouseEventArgs e) {
         var element = (FrameworkElement)sender;
         if (!element.IsMouseCaptured) return;

         var index = ViewModel.SelectedIndex;
         var leftWidth = index > 0 ? GetChild(Tabs, "TabTextBlock", ViewModel[index - 1]).ActualWidth : double.PositiveInfinity;
         var rightWidth = index < ViewModel.Count - 1 ? GetChild(Tabs, "TabTextBlock", ViewModel[index + 1]).ActualWidth : double.PositiveInfinity;
         var offset = e.GetPosition(element).X;

         if (offset < -leftWidth / 2) {
            ViewModel.SwapTabs(index, index - 1);
         } else if (offset > element.ActualWidth + rightWidth / 2) {
            ViewModel.SwapTabs(index, index + 1);
         }
      }

      private void TabMouseUp(object sender, MouseButtonEventArgs e) {
         var element = (FrameworkElement)sender;
         if (!element.IsMouseCaptured) return;
         if (e.LeftButton != MouseButtonState.Released) return;
         if (e.ChangedButton != MouseButton.Left) return;

         e.Handled = true;
         element.ReleaseMouseCapture();
      }

      #endregion

      private void ToggleTheme(object sender, EventArgs e) {
         Solarized.Theme.CurrentVariant = 1 - Solarized.Theme.CurrentVariant;
      }

      private void ExitClicked(object sender, EventArgs e) {
         ViewModel.CloseAll.Execute();
         if (ViewModel.Count == 0) Close();
      }

      private void WikiClick(object sender, EventArgs e) => System.Diagnostics.Process.Start("https://github.com/haven1433/gen3hex/wiki");
      private void ReportIssueClick(object sender, EventArgs e) => System.Diagnostics.Process.Start("https://github.com/haven1433/gen3hex/issues");
      private void AboutClick(object sender, EventArgs e) => new AboutWindow().ShowDialog();

      private void GotoBoxVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e) {
         if (GotoBox.IsVisible) {
            GotoBox.SelectAll();
            Keyboard.Focus(GotoBox);
         } else {
            if (ViewModel.SelectedIndex == -1) return;
            var selectedElement = (HexContent)GetChild(Tabs, "HexContent", ViewModel[ViewModel.SelectedIndex]);
            Keyboard.Focus(selectedElement);
         }
      }
   }
}
