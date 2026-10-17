using System.Collections.Generic;

namespace AssetIconLibrary.Utilities
{
	// Mono's string comparers slow the first load.
	internal class IconNameComparer : IEqualityComparer<string>
	{
		public bool Equals(string x, string y)
		{
			if (ReferenceEquals(x, y))
			{
				return true;
			}

			if (x is null || y is null || x.Length != y.Length)
			{
				return false;
			}

			for (var i = 0; i < x.Length; i++)
			{
				if (x[i] != y[i] && Fold(x[i]) != Fold(y[i]))
				{
					return false;
				}
			}

			return true;
		}

		public int GetHashCode(string name)
		{
			// FNV-1a over the folded characters.
			var hash = 2166136261u;

			for (var i = 0; i < name.Length; i++)
			{
				hash = (hash ^ Fold(name[i])) * 16777619u;
			}

			return (int)hash;
		}

		private static char Fold(char c)
		{
			if (c > 127)
			{
				return char.ToUpperInvariant(c);
			}

			return c is >= 'a' and <= 'z' ? (char)(c - 32) : c;
		}
	}
}
