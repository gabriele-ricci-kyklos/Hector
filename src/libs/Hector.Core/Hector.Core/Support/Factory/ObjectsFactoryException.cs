using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Hector.Core.Support.Factory
{
    public class ObjectsFactoryException : Exception
    {
        public ObjectsFactoryException()
            : base()
        {
        }

        public ObjectsFactoryException(string message)
            : base(message)
        {
        }

        public ObjectsFactoryException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ObjectsFactoryException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
