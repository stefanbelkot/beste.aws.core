using System;

namespace Beste.Core.Models
{
    public interface IResponse<T> where T : IComparable
    {
        T Result { get; }
    }

}
