using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MarkovSharp.Models
{
    public class SourceGrams<T>
    {
        public T[] Before { get; }

        public SourceGrams(params T[] args)
        {
            Before = args;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SourceGrams<T> x))
            {
                return false;
            }

            var equals = Before.OrderBy(a => a).ToArray().SequenceEqual(x.Before.OrderBy(a => a).ToArray());
            return equals;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                foreach (var member in Before.Where(a => !EqualityComparer<T>.Default.Equals(a, default(T))))
                {
                    hash = hash * 23 + member.GetHashCode();
                }
                return hash;
            }
        }

        public int GetHashCodeGenerated()
        {
            return -2044498930 + EqualityComparer<T[]>.Default.GetHashCode(Before);
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(Before);
        }
    }
}
