using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Hector.Core.Support.Factory
{
    public class FactoryObjectsCollection : KeyedCollection<string, FactoryObject>
    {
        protected override string GetKeyForItem(FactoryObject item)
        {
            return item.Name;
        }

        public FactoryObject GetValue(string key)
        {
            key.AssertNotNull("key");

            if (Contains(key))
            {
                return this[key];
            }

            return null;
        }
    }
}
