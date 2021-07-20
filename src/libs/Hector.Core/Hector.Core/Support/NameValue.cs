using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hector.Core.Support
{
    public class NameValue<TName, TValue>
    {
        public TName Name { get; }
        public TValue Value { get { return _valueFunction(); } }

        private readonly Func<TValue> _valueFunction;

        public NameValue(TName name, Func<TValue> valueFunction)
        {
            Name = name;
            _valueFunction = valueFunction;
        }
    }
}
