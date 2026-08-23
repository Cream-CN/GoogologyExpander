// LPrssEngine.cs - LPrSS 引擎 (基于定义 14.1 和 14.2)
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogologyExpander
{
	public class LPrssEngine
	{
		public string Expand(List<int> sequence, int steps)
		{
			var current = new List<int>(sequence);

			if (current.Count == 0)
				return "";

			for (int step = 1; step <= steps; step++)
			{
				if (current.Count == 0)
					break;

				current = ExpandOneStep(current);

				if (current.SequenceEqual(sequence) && step > 1)
					break;
			}

			return LPrssParser.FormatPlain(current);
		}

		public List<int> ExpandOneStep(List<int> sequence)
		{
			if (sequence == null || sequence.Count == 0)
				return new List<int>();

			var seq = new List<int>(sequence);
			int m = seq.Count - 1;
			int last = seq[m];

			if (last == 1)
			{
				seq.RemoveAt(m);
				return seq;
			}

			int badRootIndex = FindBadRoot(seq, m);

			if (badRootIndex == -1)
			{
				seq.RemoveAt(m);
				return seq;
			}

			var goodPart = seq.Take(badRootIndex).ToList();
			var badPart = seq.Skip(badRootIndex).Take(m - badRootIndex).ToList();

			int delta = last - seq[badRootIndex];
			int increment = delta - 1;

			var result = new List<int>(goodPart);
			result.AddRange(badPart);

			var badCopy = new List<int>(badPart);
			for (int i = 0; i < badCopy.Count; i++)
			{
				badCopy[i] += increment;
			}
			result.AddRange(badCopy);

			return result;
		}

		private int FindBadRoot(List<int> seq, int m)
		{
			int last = seq[m];
			for (int i = m - 1; i >= 0; i--)
			{
				if (seq[i] < last)
					return i;
			}
			return -1;
		}

		public bool IsEmpty(List<int> sequence) => sequence == null || sequence.Count == 0;

		public int GetValue(List<int> sequence)
		{
			if (sequence == null || sequence.Count == 0)
				return 0;

			int steps = 0;
			var current = new List<int>(sequence);
			while (current.Count > 0)
			{
				current = ExpandOneStep(current);
				steps++;
				if (steps > 10000)
					break;
			}
			return steps;
		}

		public int ExpandToEmpty(List<int> sequence)
		{
			int steps = 0;
			var current = new List<int>(sequence);
			while (current.Count > 0)
			{
				current = ExpandOneStep(current);
				steps++;
				if (steps > 10000)
					break;
			}
			return steps;
		}

		public List<List<int>> ExpandWithHistory(List<int> sequence, int maxSteps)
		{
			var history = new List<List<int>>();
			var current = new List<int>(sequence);
			history.Add(new List<int>(current));

			for (int i = 0; i < maxSteps && current.Count > 0; i++)
			{
				current = ExpandOneStep(current);
				history.Add(new List<int>(current));
			}
			return history;
		}
	}
}