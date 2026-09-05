using System;
using System.Collections.Generic;
namespace MmoTemplate.Rpg
{
    /// <summary>Local game value that only notifies presentation when it changes.</summary>
    public sealed class ObservableValue<T>
    {
        private T value;
        public event Action<T,T> OnValueChanged;
        public ObservableValue(T initial) => value=initial;
        public T Value
        {
            get=>value;
            set { if(EqualityComparer<T>.Default.Equals(this.value,value))return;T previous=this.value;this.value=value;OnValueChanged?.Invoke(previous,value); }
        }
    }
}
