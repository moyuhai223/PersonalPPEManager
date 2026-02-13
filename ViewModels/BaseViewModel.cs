// ViewModels/BaseViewModel.cs
using System.ComponentModel;
using System.Runtime.CompilerServices; // Required for CallerMemberName

namespace PersonalPPEManager.ViewModels
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed. 
        /// This is optional and can be provided automatically when invoked from compilers that support CallerMemberName.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets the property if its value has changed and raises the PropertyChanged event.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="storage">Reference to the backing field of the property.</param>
        /// <param name="value">The new value for the property.</param>
        /// <param name="propertyName">The name of the property.
        /// This is optional and can be provided automatically when invoked from compilers that support CallerMemberName.</param>
        /// <returns>True if the value was changed, false otherwise.</returns>
        protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false; // Value hasn't changed
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}