using System;
using System.Reflection;
using System.Windows;

namespace Void.EXStremio.WPF {
    public class WindowBase : Window {
        public WindowBase() : base() {
            DataContextChanged += OnDataContextChanged;
        }

        void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (e.OldValue is ICanRequestClose oldVM) {
                oldVM.CloseRequest -= OnCloseRequested;
            }

            if (e.NewValue is ICanRequestClose newVM) {
                newVM.CloseRequest += OnCloseRequested;
            }
        }

        void OnCloseRequested(bool? value) {
            if (IsModal()) {
                DialogResult = value;
            }
            Close();
        }

        bool IsModal() {
            return (bool)typeof(Window).GetField("_showingAsDialog", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);
        }
    }

    interface ICanRequestClose {
        event Action<bool?> CloseRequest;
    }
}
