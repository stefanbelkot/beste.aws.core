using System;

namespace Beste.Rights
{
    interface IRight
    {
        bool CheckRight(string context);
    }
    public abstract class Right : IRight
    {
        public abstract bool CheckRight(string context);
    }
    public class RightManager : Right
    {
        public override bool CheckRight(string context)
        {
            throw new NotImplementedException();
        }
    }
}
