using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hector.Core.Support.Collections.Comparers.Dynamic
{
    // credits: https://www.codeproject.com/Articles/12311/Dynamic-List-Sorting

    public struct SortProperty
    {
        private string name;
        public string Name
        {
            get { return name; }
            set
            {
                if (value == null || value.Trim().Length == 0)
                    throw new ArgumentException("A property cannot have an empty name.");

                name = value.Trim();
            }
        }
        public bool Descending { get; set; }

        public SortProperty(string propertyName)
        {
            if (propertyName == null || propertyName.Trim().Length == 0)
                throw new ArgumentException("A property cannot have an empty name.");

            name = propertyName.Trim();
            Descending = false;
        }

        public SortProperty(string propertyName, bool sortDescending)
        {
            if (propertyName == null || propertyName.Trim().Length == 0)
                throw new ArgumentException("A property cannot have an empty name.");

            name = propertyName.Trim();
            Descending = sortDescending;
        }
    }
}
