using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryManager.VM.Helpers
{
    public class LimitedStack<T>
    {
        private List<T> Stack;
        public int Count => Stack.Count;

        private int Limit;
        /// <summary>If the previous limit is greater than <paramref name="Value"/>, the oldest items will be removed.</summary>
        /// <param name="Value"></param>
        public void SetLimit(int Value)
        {
            if (Value <= 0)
                throw new ArgumentOutOfRangeException($"{nameof(LimitedStack<T>)}.{nameof(Limit)} cannot be <= 0.");

            if (Value != Limit)
            {
                if (Value > Count)
                {
                    Limit = Value;
                }
                else
                {
                    Stack = new(Stack.Skip(Count - Value));
                }
            }
        }

        public LimitedStack(int Limit)
        {
            this.Limit = Limit;
            Stack = new(Limit);
        }

        public void Clear() => Stack.Clear();

        public T Peek() => Stack[^1];

        public void Push(T Item)
        {
            if (Stack.Count == Limit)
                Stack.RemoveAt(0);
            Stack.Add(Item);
        }

        public T Pop()
        {
            T Item = Peek();
            Stack.RemoveAt(Stack.Count - 1);
            return Item;
        }

        public bool TryPop(out T Item)
        {
            if (Stack.Count > 0)
            {
                Item = Pop();
                return true;
            }
            else
            {
                Item = default;
                return false;
            }
        }

        public override string ToString() => $"{nameof(LimitedStack<T>)}: {Count} / {Limit} (Top={Peek})";
    }
}
