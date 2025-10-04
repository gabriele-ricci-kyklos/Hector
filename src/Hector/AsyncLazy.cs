using System;
using System.Threading.Tasks;

namespace Hector
{
    public sealed class AsyncLazy<T>(Func<Task<T>> factory)
    {
        private readonly Lazy<Task<T>> _instance = new(() => factory());

        public Task<T> Value => _instance.Value;
    }
}
