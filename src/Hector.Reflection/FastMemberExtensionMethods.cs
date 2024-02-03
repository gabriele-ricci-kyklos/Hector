using FastMember;

namespace Hector.Core.Reflection
{
    public static class FastMemberExtensionMethods
    {
        public static bool CanWrite(this Member m)
        {
            try
            {
                return m.CanWrite;
            }
            catch
            {
                return false;
            }
        }

        public static bool CanRead(this Member m)
        {
            try
            {
                return m.CanRead;
            }
            catch
            {
                return false;
            }
        }
    }
}
