using System;
using System.Collections.Generic;
using System.Text;

namespace Mensa
{
    internal class Canteen
    {
        public readonly string Name;
        public readonly Uri Uri;

        public Canteen(string name, Uri uri)
        {
            Name = name;
            Uri = uri;
        }
    }
}
