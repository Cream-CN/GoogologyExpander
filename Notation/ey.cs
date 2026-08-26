using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GoogologyExpander
{
	public class Element
	{
		public int Value { get; set; }
		public List<int> Row { get; set; } = new List<int>();
		public int Cloumn { get; set; }
		public int Idx { get; set; }
		public int No { get; set; }
		public int Id { get; set; }
		public Element Parent { get; set; }
		public Element Head { get; set; }
		public Element Foot { get; set; }
		public Element Ref { get; set; }
		public List<List<int>> Offset { get; set; }
	}

	public class SeqItem
	{
		public int Value { get; set; }
		public int Cloumn { get; set; }
		public SeqItem Parent { get; set; }
	}

	public class Expander
	{
		private static Regex lineBreakRegex = new Regex(@"\r?\n");
		private static Regex itemSeparatorRegex = new Regex(@"[\t ,]");

		public string ExpandXY(string input, string inputn, string inputd, bool inputm)
		{
			string mt = "";
			var lines = lineBreakRegex.Split(input);
			var results = new List<string>();
			foreach (var line in lines)
			{
				results.Add(ExpandMultiLimited(line, inputn, inputd, ref mt));
			}
			return string.Join("\n", results);
		}

		private static void DisplayMt(List<List<Element>> m, ref string mt)
		{
			var index = new List<int>();
			for (int i = 0; i < m.Count; i++) index.Add(0);
			mt += "<p></p><table>";
			while (true)
			{
				var row = new List<int>();
				for (int i = 0; i < m.Count; i++)
				{
					if (m[i].Count > index[i] && (row.Count == 0 || CompareRow(m[i][index[i]].Row, row) < 0))
						row = m[i][index[i]].Row;
				}
				if (row.Count == 0) break;
				mt += "<tr>";
				mt += "<td align=\"center\" width=\"80\" bgColor=\"#eee0e0\">" + string.Join(",", row) + "</td>";
				for (int i = 0; i < m.Count; i++)
				{
					if (m[i].Count > index[i] && CompareRow(m[i][index[i]].Row, row) == 0)
						mt += "<td align=\"center\" width=\"80\" bgColor=\"#e0eee0\">" + m[i][index[i]++].Value + "</td>";
					else
						mt += "<td align=\"center\" width=\"80\" bgColor=\"#e0eee0\"></td>";
				}
				mt += "</tr>";
			}
			mt += "</table>";
		}

		private static bool IsDimensionLimited(Element it, List<int> d)
		{
			if (Proc(d).Count == 1 && Proc(d)[0] != 0 && GetFootRow(it, d)[1] > Proc(d)[0] ||
				d.Count == 2 && d[0] == 0 && d[1] > 0 && GetFootRow(it, d).Count > d[1])
				return true;
			return false;
		}

		private static List<int> Proc(List<int> d)
		{
			if (d.Count > 2 && d[0] == 0 && d[1] > 0 && Divide(d).Count > 1)
				return new List<int> { Divide(d)[0].Count - 1 };
			if (d.Count > 2 && d[0] == 0 && d[1] > 0 && Divide(d).Count == 1 && Divide(d)[0].Count > 2)
				return new List<int> { Divide(d)[0].Count - 2 };
			return d;
		}

		private static List<int> RowGenerator(int n)
		{
			var row = new List<int> { 1 };
			for (int i = 1; i < n; i++) row.Add(0);
			return row;
		}

		private static List<List<int>> Divide(List<int> d)
		{
			var dd = d.Skip(1).ToList();
			var ret = new List<List<int>>();
			for (int i = 0; i < dd.Count - 1; i++)
			{
				while (dd[i]-- > 0)
					ret.Add(RowGenerator(dd.Count - i));
			}
			if (dd[dd.Count - 1] != 0)
				ret.Add(new List<int> { dd[dd.Count - 1] });
			return ret;
		}

		private static List<int> Merge(List<List<int>> d)
		{
			var ret = RowGenerator(d[0].Count);
			ret[0] -= 1;
			for (int i = 0; i < d.Count - 1; i++)
				ret[ret.Count - d[i].Count]++;
			ret[ret.Count - d[d.Count - 1].Count] += d[d.Count - 1][0];
			ret.Insert(0, 0);
			return ret;
		}

		private static int CompareRow(List<int> r1, List<int> r2)
		{
			int i = 0;
			for (; i < r1.Count && i < r2.Count; i++)
			{
				if (r1[i] > r2[i]) return 1;
				if (r1[i] < r2[i]) return -1;
			}
			if (r1.Count > i) return 1;
			if (r2.Count > i) return -1;
			return 0;
		}

		private static int CompareDimension(List<int> r1, List<int> r2)
		{
			return (r1.Count <= 1 ? 1 : r1[1]) - (r2.Count <= 1 ? 1 : r2[1]);
		}

		private static List<int> RowAddition(List<int> r1, List<int> r2)
		{
			if (r2.Count <= 1 || r2[1] == 1) return r1.Concat(r2).ToList();
			if (r1.Count <= 1 || r1[1] < r2[1]) return r2;
			int i = 1;
			while (++i < r1.Count)
				if (r1[i] < r2[1]) break;
			return r1.Take(i).Concat(r2.Skip(1)).ToList();
		}

		private static List<int> RowDifference(List<int> r1, List<int> r2)
		{
			if (CompareRow(r1, r2) <= 0) return new List<int>();
			if (r1[1] == 1) return r1.Take(r1.Count - r2.Count).ToList();
			int i = 0;
			for (; i < r1.Count && i < r2.Count; i++)
				if (r1[i] > r2[i]) break;
			if (r1[i] == 1) return r1.Skip(i).ToList();
			return new List<int> { 1 }.Concat(r1.Skip(i)).ToList();
		}

		private static List<int> GetFootRow(Element it, List<int> d)
		{
			var row = RowDifference(it.Row, it.Parent.Row);
			if (row.Count == 0 || d.Count == 0 || d.Count == 1 && d[0] == 0 ||
				d.Count == 2 && d[0] == 0 || d.Count == 3 && d[0] == 0 && d[1] == 1 && d[2] == 0)
				return RowAddition(it.Row, new List<int> { 1 });
			if (row.Count == 1) return RowAddition(it.Row, new List<int> { 1, 2 });
			return RowAddition(it.Row, new List<int> { 1, row[1] + 1 });
		}

		private static void SetElementRefrence(List<List<Element>> m)
		{
			for (int i = 0; i < m.Count; i++)
			{
				for (int j = 0; j < m[i].Count; j++)
				{
					if (m[i][j].Row.Count <= 1 && m[i][j].Value <= 1) continue;
					if (CompareRow(m[i][j].Row, m[i][j].Parent.Row) == 0)
						m[i][j].Ref = m[i][j].Parent;
					else if (m[i][j].Head.Parent.Foot != null && CompareRow(m[i][j].Head.Parent.Foot.Row, m[i][j].Row) <= 0)
					{
						m[i][j].Ref = m[i][j].Head.Parent.Foot;
						while (CompareRow(m[i][j].Ref.Row, m[i][j].Row) == 0)
							m[i][j].Ref = m[i][j].Ref.Ref;
					}
					else
						m[i][j].Ref = m[i][j].Head.Parent;
				}
			}
		}

		private static void SetElementNo(List<List<Element>> m, Element b)
		{
			int id = 0;
			for (int i = 0; i < m.Count; i++)
			{
				for (int j = 0; j < m[i].Count; j++)
				{
					if (i == b.Cloumn)
						m[i][j].No = j + 1;
					else if (i < b.Cloumn || (m[i][j].Value <= 1 && j == 0))
						m[i][j].No = 0;
					else
						m[i][j].No = m[i][j].Ref.No;
					if (i > b.Cloumn) m[i][j].Id = id++;
					if (i == b.Cloumn || i == m.Count - 1) m[i][j].Id = m[i][j].No;
				}
			}
		}

		private static List<Element> GetReferenceChain(Element it)
		{
			var c = new List<Element>();
			while (true)
			{
				c.Insert(0, it);
				if (it.Value <= 1 && it.Row.Count == 1) break;
				it = it.Ref;
			}
			return c;
		}

		private static List<List<Element>> DrawMountain(List<SeqItem> s, List<int> d)
		{
			var m = new List<List<Element>>();
			foreach (var e in s)
			{
				var parent = e.Parent.Cloumn < 0
					? new Element { Row = new List<int> { 1 }, Cloumn = -1 }
					: m[e.Parent.Cloumn][0];
				m.Add(new List<Element> { new Element { Value = e.Value, Row = new List<int> { 1 }, Cloumn = e.Cloumn, Idx = 0, Parent = parent } });
			}
			for (int i = 0; i < m.Count; i++)
			{
				var it = m[i][0];
				while (it.Value > 1)
				{
					if (IsDimensionLimited(it, d)) break;
					it.Foot = new Element
					{
						Value = it.Value - it.Parent.Value,
						Row = GetFootRow(it, d),
						Cloumn = i,
						Idx = it.Idx + 1,
						Head = it
					};
					m[i].Add(it.Foot);
					var p = it.Parent;
					if (p.Foot != null && CompareRow(p.Foot.Row, it.Foot.Row) <= 0) p = p.Foot;
					while (p.Value >= it.Foot.Value) p = p.Parent;
					it.Foot.Parent = p;
					it = it.Foot;
				}
			}
			return m;
		}

		private static List<SeqItem> GetOds(List<List<Element>> m)
		{
			var o = new List<SeqItem>();
			foreach (var e in m)
			{
				var parent = e[e.Count - 1].Value <= 1
					? new SeqItem { Value = 0, Cloumn = -1, Parent = null }
					: o[e[e.Count - 1].Parent.Cloumn];
				o.Add(new SeqItem { Value = e[e.Count - 1].Value, Cloumn = e[0].Cloumn, Parent = parent });
			}
			return o;
		}

		// 修正：返回 List<SeqItem> 而非 List<int>
		private static List<SeqItem> GetMds(List<List<Element>> m)
		{
			var chain = GetReferenceChain(m[m.Count - 1][m[m.Count - 1].Count - 1]);
			return ToSequenceItems(chain.Select(e => e.Row.Count <= 1 ? 1 : e.Row[1]).ToList());
		}

		private static int[] GetBootIndex(List<SeqItem> s, List<int> d)
		{
			var m = DrawMountain(s, d);
			SetElementRefrence(m);
			var t = m[m.Count - 1][m[m.Count - 1].Count - 1];
			if (t.Value == 1) t = t.Head;
			var b = t.Parent;
			if (t.Value - b.Value > 1 && Proc(d).Count == 1)
			{
				var o = GetOds(m);
				var c = GetBootIndex(o, d.Count == 1 || Divide(d).Count == 1 ? d : Merge(Divide(d).Skip(1).ToList()))[0];
				return new int[] { c, m[c].Count - 1 };
			}
			if (CompareDimension(b.Row, t.Row) < 0)
			{
				var ch = GetReferenceChain(t);
				var dd = new List<int> { 0, 1 };
				if (d.Count > 2 && d[0] == 0 && d[1] == 0) dd = d.Skip(2).ToList();
				if (d.Count == 3 && d[0] == 1 && d[1] == 0 && d[2] == 1) dd = d;
				var c = ch[GetBootIndex(GetMds(m), dd)[0]].Cloumn;
				return new int[] { c, m[c][m[c].Count - 1].Idx };
			}
			return new int[] { b.Cloumn, b.Idx };
		}

		private static void CopyElement(List<List<Element>> m, Element b, Element t, Element it,
			List<Element> op, int i, List<int> d)
		{
			if (it.Value <= 1 && it.Row.Count > 1)
			{
				it.Parent = it.Ref;
				while (it.Parent.Value > 1) it.Parent = it.Parent.Ref;
			}
			var min_row = it.Row.Count > 1 ? GetFootRow(op[op.Count - 1], d) : new List<int> { 1 };
			var max_row = it.Row;
			if (it.No > 0)
			{
				var c = it.Ref.Cloumn + (t.Cloumn - b.Cloumn) * (i + 1);
				var r = m[c][0];
				while (r.Foot != null && r.Foot.Id <= it.Ref.Id) r = r.Foot;
				if (it.Offset != null)
					max_row = RowAddition(r.Row, it.Offset[i]);
				else
					max_row = RowAddition(r.Row, RowDifference(it.Row, it.Ref.Row));
			}
			if (d.Count == 0 || d.Count == 1 && d[0] == 0 || d.Count == 2 && d[0] == 0 ||
				d.Count == 3 && d[0] == 0 && d[1] == 1 && d[2] == 0)
				max_row = it.Row;
			if (CompareRow(min_row, max_row) > 0)
				throw new Exception("collapsed!");
			var row = min_row;
			while (CompareRow(row, max_row) <= 0)
			{
				op.Add(new Element { Value = it.Value, Row = row, Cloumn = m.Count - 1, Idx = op.Count, No = it.No, Id = it.Id });
				if (op.Count > 1)
				{
					op[op.Count - 1].Head = op[op.Count - 2];
					op[op.Count - 2].Foot = op[op.Count - 1];
				}
				var pc = it.Parent.Cloumn >= b.Cloumn ? m.Count - 1 + it.Parent.Cloumn - it.Cloumn : it.Parent.Cloumn;
				var p = pc >= 0 ? m[pc][m[pc].Count - 1] : new Element { Row = new List<int> { 1 }, Cloumn = -1 };
				while (CompareRow(p.Row, row) > 0) p = p.Head;
				op[op.Count - 1].Parent = p;
				row = GetFootRow(op[op.Count - 1], d);
			}
		}

		private static void CopyCloumn(List<List<Element>> m, Element b, Element t, int c,
			int i, List<int> ex, List<int> d)
		{
			var it = m[c][0];
			m.Add(new List<Element>());
			while (true)
			{
				CopyElement(m, b, t, it, m[m.Count - 1], i, d);
				if (it.Foot != null) it = it.Foot;
				else break;
			}
			m[m.Count - 1][m[m.Count - 1].Count - 1].Value = ex.Count > 0 ? ex[m.Count - 1] : it.Value;
			it = m[m.Count - 1][m[m.Count - 1].Count - 1];
			while (it.Head != null)
			{
				it.Head.Value = it.Value + it.Head.Parent.Value;
				it = it.Head;
			}
		}

		private static void ExpandDimensionSequnece(List<List<Element>> m, Element b, Element t,
			int n, List<int> d)
		{
			if (CompareDimension(b.Row, t.Row) >= 0) return;
			var dd = new List<int> { 0, 1 };
			if (d.Count > 2 && d[0] == 0 && d[1] == 0) dd = d.Skip(2).ToList();
			if (d.Count == 3 && d[0] == 1 && d[1] == 0 && d[2] == 1) dd = d;
			var s = GetMds(m);
			var c = GetBootIndex(s, dd)[0];
			for (int i = b.Cloumn + 1; i <= t.Cloumn; i++)
			{
				for (int j = 0; j < m[i].Count; j++)
				{
					if (i == t.Cloumn && j == m[t.Cloumn].Count - 1) return;
					var it = m[i][j];
					var ds = new List<SeqItem>(s);
					int pos = 1;
					if (it.No != b.No || CompareDimension(RowDifference(it.Row, it.Ref.Row), b.Row) <= 0) continue;
					ds.Insert(c + 1, new SeqItem
					{
						Value = RowDifference(it.Row, it.Ref.Row).Count < 2 ? 1 : RowDifference(it.Row, it.Ref.Row)[1],
						Parent = ds[c]
					});
					while (it.Ref.Cloumn > b.Cloumn)
					{
						it = it.Ref;
						if (CompareDimension(RowDifference(it.Row, it.Ref.Row), b.Row) <= 0) continue;
						ds.Insert(c + 1, new SeqItem
						{
							Value = RowDifference(it.Row, it.Ref.Row).Count < 2 ? 1 : RowDifference(it.Row, it.Ref.Row)[1],
							Parent = ds[c]
						});
						ds[c + 2].Parent = ds[c + 1];
						pos++;
					}
					for (int k = 0; k < ds.Count; k++)
					{
						ds[k].Cloumn = k;
						while (ds[k].Value <= ds[k].Parent.Value) ds[k].Parent = ds[k].Parent.Parent;
					}
					it = m[i][j];
					it.Offset = new List<List<int>>();
					if (d.Count == 2 && d[0] == 2)
					{
						var lift = ds[ds.Count - 1].Value - 1 - ds[ds.Count - 1].Parent.Value;
						if (d[1] > 0 && lift > d[1]) lift = d[1];
						for (int ii = 0; ii < n; ii++)
						{
							it.Offset.Add(new List<int> { 1, RowDifference(it.Row, it.Ref.Row)[1] + (1 + ii) * lift });
						}
						continue;
					}
					var len = ds.Count - 1 - c;
					var ex = Expand(ds, n, dd, false);
					for (int ii = 0; ii < n; ii++)
					{
						it.Offset.Add(new List<int> { 1, ex[c + len * (ii + 1) + pos] });
					}
				}
			}
		}

		private static List<int> Expand(List<SeqItem> s, int n, List<int> d, bool f = true)
		{
			string mt = "";
			if (s[s.Count - 1].Value <= 1) return s.Take(s.Count - 1).Select(e => e.Value).ToList();
			var idx = GetBootIndex(s, d);
			var m = DrawMountain(s, d);
			var b = m[idx[0]][idx[1]];
			var t = m[m.Count - 1][m[m.Count - 1].Count - 1];
			var ex = new List<int>();
			if (t.Value == 1) t = t.Head;
			if (f) DisplayMt(m, ref mt);
			SetElementRefrence(m);
			SetElementNo(m, b);
			ExpandDimensionSequnece(m, b, t, n, d);
			if (t.Value - b.Value > 1 && Proc(d).Count == 1)
			{
				var o = GetOds(m);
				ex = Expand(o, n, d.Count == 1 || Divide(d).Count == 1 ? d : Merge(Divide(d).Skip(1).ToList()), f);
			}
			else if (t.Foot != null)
			{
				m[m.Count - 1].RemoveAt(m[m.Count - 1].Count - 1);
				t.Foot = null;
				t.Parent = t.Parent.Parent;
			}
			for (int i = 0; i < m[m.Count - 1].Count; i++) m[m.Count - 1][i].Value--;
			for (int i = b.No; i < m[b.Cloumn].Count; i++)
			{
				var idx2 = t.Idx;
				var newElem = new Element
				{
					Value = m[b.Cloumn][i].Value,
					Row = m[b.Cloumn][i].Row,
					Cloumn = t.Cloumn,
					Idx = idx2++,
					No = m[b.Cloumn][i].No,
					Id = m[b.Cloumn][i].No,
					Parent = m[b.Cloumn][i].Parent,
					Head = m[t.Cloumn][m[t.Cloumn].Count - 1],
					Ref = m[b.Cloumn][i]
				};
				m[t.Cloumn].Add(newElem);
				m[t.Cloumn][m[t.Cloumn].Count - 2].Foot = m[t.Cloumn][m[t.Cloumn].Count - 1];
			}
			for (int i = 0; i < n; i++)
			{
				for (int j = b.Cloumn + 1; j <= t.Cloumn; j++)
				{
					CopyCloumn(m, b, t, j, i, ex, d);
				}
			}
			if (f) DisplayMt(m, ref mt);
			return m.Select(e => e[0].Value).ToList();
		}

		// 将整数列表转换为 SeqItem 列表
		private static List<SeqItem> ToSequenceItems(List<int> s)
		{
			var seq = new List<SeqItem>();
			for (int i = 0; i < s.Count; i++)
			{
				if (s[i] <= 1)
				{
					seq.Add(new SeqItem { Value = s[i], Cloumn = i, Parent = new SeqItem { Value = 0, Cloumn = -1 } });
					continue;
				}
				for (int j = i - 1; j >= 0; j--)
				{
					if (s[j] < s[i])
					{
						seq.Add(new SeqItem { Value = s[i], Cloumn = i, Parent = seq[j] });
						break;
					}
				}
			}
			return seq;
		}

		private static string ExpandMultiLimited(string s, string nstring, string dstring, ref string mt)
		{
			var parts = itemSeparatorRegex.Split(s.Trim()).Where(e => e.Length > 0).Select(e => int.Parse(e)).ToList();
			var result = new List<int>(parts);
			foreach (var nStr in nstring.Split(','))
			{
				var n = Math.Min(int.Parse(nStr.Trim()), 10);
				var d = dstring.Split(',').Select(e => int.Parse(e.Trim())).ToList();
				var seq = ToSequenceItems(result);
				result = Expand(seq, n, d, false);
			}
			return string.Join(",", result);
		}
	}
}