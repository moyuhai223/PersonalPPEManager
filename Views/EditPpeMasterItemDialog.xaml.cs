// Views/EditPpeMasterItemDialog.xaml.cs
using PersonalPPEManager.ViewModels;
using System;
using System.Diagnostics;
using System.Windows;

namespace PersonalPPEManager.Views
{
    public partial class EditPpeMasterItemDialog : Window
    {
        public EditPpeMasterItemDialogViewModel ViewModel => DataContext as EditPpeMasterItemDialogViewModel;

        public EditPpeMasterItemDialog()
        {
            InitializeComponent();
            Debug.WriteLine("DEBUG: EditPpeMasterItemDialog parameterless constructor called.");
        }

        private void EditPpeMasterItemDialog_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Debug.WriteLine("DEBUG: EditPpeMasterItemDialog.DataContextChanged Fired.");
            if (e.OldValue is EditPpeMasterItemDialogViewModel oldVm)
            {
                oldVm.RequestCloseDialog -= HandleRequestCloseDialog;
                Debug.WriteLine("DEBUG: EditPpeMasterItemDialog.DataContextChanged: Unsubscribed from old ViewModel's RequestCloseDialog.");
            }
            if (e.NewValue is EditPpeMasterItemDialogViewModel newVm)
            {
                newVm.RequestCloseDialog += HandleRequestCloseDialog;
                Debug.WriteLine("DEBUG: EditPpeMasterItemDialog.DataContextChanged: Subscribed to new ViewModel's RequestCloseDialog.");
            }
        }

        private void HandleRequestCloseDialog(bool? dialogResult)
        {
            Debug.WriteLine($"DEBUG: EditPpeMasterItemDialog.HandleRequestCloseDialog: Received request to close with DialogResult: {dialogResult}");
            try
            {
                if (System.Windows.Interop.ComponentDispatcher.IsThreadModal && IsLoaded)
                {
                    this.DialogResult = dialogResult;
                }
                else if (IsLoaded)
                {
                    this.Close();
                }
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"DEBUG: EditPpeMasterItemDialog.HandleRequestCloseDialog: InvalidOperationException: {ex.Message}");
                if (IsLoaded) this.Close();
            }
        }
    }
}