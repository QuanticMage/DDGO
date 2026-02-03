using System.Diagnostics.Metrics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DDUP
{
	public class ExpressionParser
	{
		// ----------------------------------------------------
		// 1) Lexer
		// ----------------------------------------------------

		private enum TokKind
		{
			End,
			LParen, RParen,
			Comma,

			// Boolean keywords/operators
			And, Or,
			Bang,        // !

			// Arithmetic
			Plus, Minus, Star, Slash,

			// Comparisons
			Lt, Gt, Le, Ge,
			Eq, EqEq, Ne,
			Colon, NotColon, // :  !:

			// Literals/identifiers
			Number,
			String,
			Ident
		}

		private readonly struct Tok
		{
			public readonly TokKind Kind;
			public readonly string Text;   // raw text or string literal content (trimmed)
			public readonly double Number; // parsed number (clamped)

			public Tok(TokKind kind, string text, double number = 0)
			{
				Kind = kind;
				Text = text;
				Number = number;
			}

			public override string ToString() => $"{Kind} '{Text}'";
		}

		private sealed class Lexer
		{
			private readonly string _s;
			private int _i;

			public Lexer(string s) => _s = s ?? "";

			public Tok Next()
			{
				SkipWs();
				if (_i >= _s.Length) return new Tok(TokKind.End, "");

				char c = _s[_i];
				if (StartsWithUltPlus(out int len))
				{
					string text = _s.Substring(_i, len);
					_i += len;
					return new Tok(TokKind.Ident, text);
				}

				// --- Boolean symbolic operators ---
				if (c == '&' && PeekAt("&&"))
				{
					_i += 2;
					return new Tok(TokKind.And, "&&");
				}
				if (c == '|' && PeekAt("||"))
				{
					_i += 2;
					return new Tok(TokKind.Or, "||");
				}

				// Single char tokens
				if (c == '(') { _i++; return new Tok(TokKind.LParen, "("); }
				if (c == ')') { _i++; return new Tok(TokKind.RParen, ")"); }
				if (c == ',') { _i++; return new Tok(TokKind.Comma, ","); }
				if (c == '+') { _i++; return new Tok(TokKind.Plus, "+"); }
				if (c == '-') { _i++; return new Tok(TokKind.Minus, "-"); }
				if (c == '*') { _i++; return new Tok(TokKind.Star, "*"); }
				if (c == '/') { _i++; return new Tok(TokKind.Slash, "/"); }

				// Two-char ops / comparisons
				if (c == '!')
				{
					if (PeekAt("!:")) { _i += 2; return new Tok(TokKind.NotColon, "!:"); }
					if (PeekAt("!=")) { _i += 2; return new Tok(TokKind.Ne, "!="); }
					_i++;
					return new Tok(TokKind.Bang, "!");
				}

				if (c == ':') { _i++; return new Tok(TokKind.Colon, ":"); }

				if (c == '<')
				{
					if (PeekAt("<=")) { _i += 2; return new Tok(TokKind.Le, "<="); }
					_i++; return new Tok(TokKind.Lt, "<");
				}
				if (c == '>')
				{
					if (PeekAt(">=")) { _i += 2; return new Tok(TokKind.Ge, ">="); }
					_i++; return new Tok(TokKind.Gt, ">");
				}
				if (c == '=')
				{
					if (PeekAt("==")) { _i += 2; return new Tok(TokKind.EqEq, "=="); }
					_i++; return new Tok(TokKind.Eq, "=");
				}

				// Quoted string: "..."`
				if (c == '"')
				{
					_i++; // skip opening quote
					var sb = new StringBuilder();
					while (_i < _s.Length)
					{
						char ch = _s[_i++];
						if (ch == '"') break;

						if (ch == '\\' && _i < _s.Length)
						{
							char next = _s[_i];
							if (next == '"' || next == '\\')
							{
								sb.Append(next);
								_i++;
								continue;
							}
						}
						sb.Append(ch);
					}
					string str = sb.ToString().Trim();
					return new Tok(TokKind.String, str);
				}

				// Number (integer/decimal/exponent). Sign is handled by the parser (unary '-').
				if (char.IsDigit(c) || (c == '.' && _i + 1 < _s.Length && char.IsDigit(_s[_i + 1])))
				{
					int start = _i;

					// integer part
					while (_i < _s.Length && char.IsDigit(_s[_i])) _i++;

					// fractional part
					if (_i < _s.Length && _s[_i] == '.')
					{
						_i++;
						while (_i < _s.Length && char.IsDigit(_s[_i])) _i++;
					}

					// exponent part (e.g. 1e3, 1.2E-4)
					if (_i < _s.Length && (_s[_i] == 'e' || _s[_i] == 'E'))
					{
						int expStart = _i;
						_i++; // consume e/E

						if (_i < _s.Length && (_s[_i] == '+' || _s[_i] == '-')) _i++;

						int expDigitsStart = _i;
						while (_i < _s.Length && char.IsDigit(_s[_i])) _i++;

						// If there were no exponent digits, roll back (treat 'e' as start of ident)
						if (expDigitsStart == _i)
							_i = expStart;
					}

					string raw = _s.Substring(start, _i - start);

					if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
						d = 0;

					return new Tok(TokKind.Number, raw, ClampNumber(d));
				}
				// Ident / keyword
				{
					int start = _i;
					while (_i < _s.Length &&
						   !char.IsWhiteSpace(_s[_i]) &&
						   "() ,+-*/<>=!:\"&|".IndexOf(_s[_i]) < 0) // <-- add &|
					{
						_i++;
					}

					string text = _s.Substring(start, _i - start).Trim();
					if (text.Length == 0) return Next();

					if (text.Equals("and", StringComparison.OrdinalIgnoreCase))
						return new Tok(TokKind.And, text);
					if (text.Equals("or", StringComparison.OrdinalIgnoreCase))
						return new Tok(TokKind.Or, text);

					if (text.Equals("not", StringComparison.OrdinalIgnoreCase))
						return new Tok(TokKind.Bang, "!");

					return new Tok(TokKind.Ident, text);
				}
			}

			private bool StartsWithUltPlus(out int len)
			{
				len = 0;

				// Prefer longer match first
				if (MatchWord("Ult++", out len)) return true;
				if (MatchWord("Ult+", out len)) return true;

				return false;
			}

			private bool MatchWord(string lit, out int len)
			{
				len = 0;

				if (_i + lit.Length > _s.Length) return false;
				if (!string.Equals(_s.Substring(_i, lit.Length), lit, StringComparison.OrdinalIgnoreCase))
					return false;

				// Optional: ensure it's bounded (not part of a longer token)
				int end = _i + lit.Length;
				if (end < _s.Length)
				{
					char next = _s[end];
					// If next char could be part of an identifier, don't match
					if (!char.IsWhiteSpace(next) && "() ,+-*/<>=!:\"&|".IndexOf(next) < 0)
						return false;
				}

				len = lit.Length;
				return true;
			}

			private void SkipWs()
			{
				while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) _i++;
			}

			private bool PeekAt(string lit, int offset = 0)
			{
				int j = _i + offset;
				if (j + lit.Length > _s.Length) return false;

				for (int k = 0; k < lit.Length; k++)
					if (_s[j + k] != lit[k]) return false;

				return true;
			}

			private static double ClampNumber(double d)
			{
				const double min = -1_000_000;
				const double max = 1_000_000;
				if (d < min) return min;
				if (d > max) return max;
				return d;
			}
		}

		// ----------------------------------------------------
		// 2) AST nodes
		// ----------------------------------------------------

		public abstract class BoolNode
		{
			public abstract bool Eval(ItemViewRow r);
		}

		private abstract class ValueNode
		{
			public abstract Value Eval(ItemViewRow r);

			// NEW: allows extracting a constant value without a row (if possible)
			public virtual bool TryGetConst(out Value v)
			{
				v = Value.FromStr("");
				return false;
			}
		}
		private readonly struct Value
		{
			public readonly bool IsNumber;
			public readonly double Num;
			public readonly string Str;

			private Value(double n) { IsNumber = true; Num = n; Str = ""; }
			private Value(string s) { IsNumber = false; Num = 0; Str = s ?? ""; }

			public static Value FromNum(double n) => new Value(n);
			public static Value FromStr(string s) => new Value((s ?? "").Trim());

			public override string ToString() => IsNumber
				? Num.ToString(CultureInfo.InvariantCulture)
				: Str;
		}

		private sealed class BoolUnary : BoolNode
		{
			private readonly BoolNode _rhs;
			public BoolUnary(BoolNode rhs) => _rhs = rhs;

			public override bool Eval(ItemViewRow r) => !_rhs.Eval(r);
		}

		private sealed class BoolBinary : BoolNode
		{
			private readonly TokKind _op;
			private readonly BoolNode _a, _b;

			public BoolBinary(TokKind op, BoolNode a, BoolNode b)
			{
				_op = op; _a = a; _b = b;
			}

			public override bool Eval(ItemViewRow r)
			{
				return _op switch
				{
					TokKind.And => _a.Eval(r) && _b.Eval(r),
					TokKind.Or => _a.Eval(r) || _b.Eval(r),
					_ => true
				};
			}
		}

		private sealed class ComparisonNode : BoolNode
		{
			private readonly TokKind _op;
			private readonly ValueNode _left;
			private readonly ValueNode _right;

			public ComparisonNode(TokKind op, ValueNode left, ValueNode right)
			{
				_op = op; _left = left; _right = right;
			}

			public override bool Eval(ItemViewRow r)
			{
				var a = _left.Eval(r);
				var b = _right.Eval(r);

				// Numeric compare if both are numeric
				if (a.IsNumber && b.IsNumber)
				{
					double x = a.Num, y = b.Num;
					return _op switch
					{
						TokKind.Lt => x < y,
						TokKind.Le => x <= y,
						TokKind.Gt => x > y,
						TokKind.Ge => x >= y,
						TokKind.Eq => x == y,
						TokKind.EqEq => x == y,
						TokKind.Ne => x != y,
						_ => false
					};
				}

				// Otherwise string compare (case-insensitive, trimmed)
				string xs = a.IsNumber ? a.Num.ToString(CultureInfo.InvariantCulture) : (a.Str ?? "");
				string ys = b.IsNumber ? b.Num.ToString(CultureInfo.InvariantCulture) : (b.Str ?? "");
				xs = xs.Trim();
				ys = ys.Trim();

				return _op switch
				{
					TokKind.Colon => xs.Contains(ys, StringComparison.InvariantCultureIgnoreCase),
					TokKind.NotColon => !xs.Contains(ys, StringComparison.InvariantCultureIgnoreCase),

					TokKind.Eq => xs.Equals(ys, StringComparison.InvariantCultureIgnoreCase),
					TokKind.EqEq => xs.Equals(ys, StringComparison.InvariantCultureIgnoreCase),
					TokKind.Ne => !xs.Equals(ys, StringComparison.InvariantCultureIgnoreCase),

					_ => true
				};
			}
		}

		private sealed class NameContainsNode : BoolNode
		{
			private readonly string _term;
			private readonly bool _negate;

			public NameContainsNode(string term, bool negate)
			{
				_term = (term ?? "").Trim();
				_negate = negate;
			}

			public override bool Eval(ItemViewRow r)
			{
				string name = r.Name ?? "";
				bool ok = name.Contains(_term, StringComparison.InvariantCultureIgnoreCase);
				return _negate ? !ok : ok;
			}
		}

		private sealed class NumLiteral : ValueNode
		{
			private readonly double _n;
			public NumLiteral(double n) => _n = n;
			public override Value Eval(ItemViewRow r) => Value.FromNum(_n);
			public override bool TryGetConst(out Value v)
			{
				v = Value.FromNum(_n);
				return true;
			}
		}

		private sealed class StrLiteral : ValueNode
		{
			private readonly string _s;
			public StrLiteral(string s) => _s = (s ?? "").Trim();
			public override Value Eval(ItemViewRow r) => Value.FromStr(_s);
			public override bool TryGetConst(out Value v)
			{
				v = Value.FromStr(_s);
				return true;
			}
		}

		private sealed class TagValueNode : ValueNode
		{
			public readonly string Tag;
			public TagValueNode(string tag) => Tag = tag;

			public override Value Eval(ItemViewRow r)
			{
				if (TryGetNumber(r, Tag, out var n)) return Value.FromNum(n);
				if (TryGetString(r, Tag, out var s)) return Value.FromStr(s);
				return Value.FromStr("");
			}
		}

		private sealed class NumUnary : ValueNode
		{
			private readonly ValueNode _rhs;
			public NumUnary(ValueNode rhs) => _rhs = rhs;

			public override Value Eval(ItemViewRow r)
			{
				var v = _rhs.Eval(r);
				if (!v.IsNumber) return Value.FromNum(0);
				return Value.FromNum(-v.Num);
			}

			public override bool TryGetConst(out Value v)
			{
				if (_rhs.TryGetConst(out var rhs) && rhs.IsNumber)
				{
					v = Value.FromNum(-rhs.Num);
					return true;
				}
				v = Value.FromNum(0);
				return false;
			}
		}

		private sealed class NumBinary : ValueNode
		{
			private readonly TokKind _op;
			private readonly ValueNode _a, _b;

			public NumBinary(TokKind op, ValueNode a, ValueNode b)
			{
				_op = op; _a = a; _b = b;
			}

			public override Value Eval(ItemViewRow r)
			{
				var va = _a.Eval(r);
				var vb = _b.Eval(r);
				if (!va.IsNumber || !vb.IsNumber) return Value.FromNum(0);

				double x = va.Num, y = vb.Num;
				return _op switch
				{
					TokKind.Plus => Value.FromNum(x + y),
					TokKind.Minus => Value.FromNum(x - y),
					TokKind.Star => Value.FromNum(x * y),
					TokKind.Slash => Value.FromNum(y == 0 ? 0 : x / y),
					_ => Value.FromNum(0)
				};
			}

			public override bool TryGetConst(out Value v)
			{
				if (_a.TryGetConst(out var a) && _b.TryGetConst(out var b) && a.IsNumber && b.IsNumber)
				{
					double x = a.Num, y = b.Num;
					v = _op switch
					{
						TokKind.Plus => Value.FromNum(x + y),
						TokKind.Minus => Value.FromNum(x - y),
						TokKind.Star => Value.FromNum(x * y),
						TokKind.Slash => Value.FromNum(y == 0 ? 0 : x / y),
						_ => Value.FromNum(0)
					};
					return true;
				}
				v = Value.FromNum(0);
				return false;
			}
		}

		// ----------------------------------------------------
		// 3) Parser (recursive descent)
		// ----------------------------------------------------
		public sealed class Parser
		{
			private readonly Lexer _lex;
			private Tok _cur;
			private readonly Func<string, string> _normalizeTag;
			private string _error = "";
			public string Error => _error;

			public Parser(string input, Func<string, string> normalizeTag)
			{
				_lex = new Lexer(input);
				_normalizeTag = normalizeTag;
				_cur = _lex.Next();
			}
			private void SetErrorOnce(string msg)
			{
				if (string.IsNullOrEmpty(_error))
					_error = msg ?? "Invalid filter.";
			}
			// expr := commaAnd
			public BoolNode ParseExpression()
			{
				var node = ParseOr();
				while (_cur.Kind == TokKind.Comma)
				{
					Consume(TokKind.Comma);
					var rhs = ParseOr();
					node = new BoolBinary(TokKind.And, node, rhs);
				}
				return node;
			}

			public BoolNode ParseAll()
			{
				var node = ParseExpression();

				if (_cur.Kind != TokKind.End)
					SetErrorOnce($"Unexpected token '{_cur.Text}'.");

				return node;
			}

			// orExpr := andExpr (or andExpr)*
			private BoolNode ParseOr()
			{
				var node = ParseAnd();
				while (_cur.Kind == TokKind.Or)
				{
					Consume(TokKind.Or);
					var rhs = ParseAnd();
					node = new BoolBinary(TokKind.Or, node, rhs);
				}
				return node;
			}

			// andExpr := unaryBool (and unaryBool)*
			private BoolNode ParseAnd()
			{
				var node = ParseUnaryBool();
				while (_cur.Kind == TokKind.And)
				{
					Consume(TokKind.And);
					var rhs = ParseUnaryBool();
					node = new BoolBinary(TokKind.And, node, rhs);
				}
				return node;
			}

			// unaryBool := ('!' | '-') unaryBool | primaryBool
			// EDIT: leading '-' acts like NOT alias (boolean context only)
			private BoolNode ParseUnaryBool()
			{
				if (_cur.Kind == TokKind.Bang)
				{
					Consume(TokKind.Bang);
					return new BoolUnary(ParseUnaryBool());
				}
				return ParsePrimaryBool();
			}

			// primaryBool := '(' expr ')' | comparisonOrBare
			private BoolNode ParsePrimaryBool()
			{
				if (_cur.Kind == TokKind.LParen)
				{
					Consume(TokKind.LParen);
					var inner = ParseExpression();
					Consume(TokKind.RParen);
					return inner;
				}

				// Parse left side as value expression first (supports arithmetic)
				ValueNode left = ParseValueExpr();

				// Explicit comparison op?
				if (IsCompareOp(_cur.Kind))
				{
					TokKind op = _cur.Kind;
					Consume(op);
					ValueNode right = ParseValueExpr();
					return new ComparisonNode(op, left, right);
				}

				// EDIT: Implicit '=' for "tag value"
				// If left is a tag reference and next token can start a value, treat as Tag = Value
				if (left is TagValueNode && CanStartValue(_cur.Kind))
				{
					ValueNode right = ParseValueExpr();
					return new ComparisonNode(TokKind.Eq, left, right);
				}

				// No compare op and no implicit tag-value:
				// Legacy free token(s): treat left as a term against name contains.
				// If left is a string literal, use it as term; if number literal, stringify it.
				// If left is a tag by itself: we’ll treat it as "tag != 0" (often useful).
				if (left is TagValueNode)
				{
					return new ComparisonNode(TokKind.Ne, left, new NumLiteral(0));
				}

				if (left.TryGetConst(out var v))
				{
					string term = v.IsNumber
						? v.Num.ToString(CultureInfo.InvariantCulture)
						: v.Str;

					if (string.IsNullOrWhiteSpace(term))
						return new NameContainsNode("", negate: false); // no-op

					return new NameContainsNode(term, negate: false);
				}

				// Not a constant and not a tag compare => safest fallback is no-op (or you can name-contains "")
				return new NameContainsNode("", negate: false);
			}

			// valueExpr := addSub
			private ValueNode ParseValueExpr() => ParseAddSub();

			// addSub := mulDiv (('+'|'-') mulDiv)*
			private ValueNode ParseAddSub()
			{
				var node = ParseMulDiv();
				while (_cur.Kind == TokKind.Plus || _cur.Kind == TokKind.Minus)
				{
					TokKind op = _cur.Kind;
					Consume(op);
					var rhs = ParseMulDiv();
					node = new NumBinary(op, node, rhs);
				}
				return node;
			}

			// mulDiv := unaryValue (('*'|'/') unaryValue)*
			private ValueNode ParseMulDiv()
			{
				var node = ParseUnaryValue();
				while (_cur.Kind == TokKind.Star || _cur.Kind == TokKind.Slash)
				{
					TokKind op = _cur.Kind;
					Consume(op);
					var rhs = ParseUnaryValue();
					node = new NumBinary(op, node, rhs);
				}
				return node;
			}

			// unaryValue := '-' unaryValue | primaryValue
			private ValueNode ParseUnaryValue()
			{
				if (_cur.Kind == TokKind.Minus)
				{
					Consume(TokKind.Minus);
					return new NumUnary(ParseUnaryValue());
				}
				return ParsePrimaryValue();
			}

			private ValueNode ParsePrimaryValue()
			{
				if (_cur.Kind == TokKind.LParen)
				{
					Consume(TokKind.LParen);
					var inner = ParseValueExpr();
					Consume(TokKind.RParen);
					return inner;
				}

				if (_cur.Kind == TokKind.Number)
				{
					double n = _cur.Number;
					Consume(TokKind.Number);
					return new NumLiteral(n);
				}

				if (_cur.Kind == TokKind.String)
				{
					string s = (_cur.Text ?? "").Trim();
					Consume(TokKind.String);
					return new StrLiteral(s);
				}

				if (_cur.Kind == TokKind.Ident)
				{
					string raw = _cur.Text ?? "";
					Consume(TokKind.Ident);

					string tag = _normalizeTag(raw);
					if (!string.IsNullOrEmpty(tag))
						return new TagValueNode(tag);

					// unknown identifiers behave like free-text (string literal)
					return new StrLiteral(raw.Trim());
				}

				// Unexpected token -> empty string literal
				return new StrLiteral("");
			}

			private static bool IsCompareOp(TokKind k) =>
				k == TokKind.Lt || k == TokKind.Le || k == TokKind.Gt || k == TokKind.Ge ||
				k == TokKind.Eq || k == TokKind.EqEq || k == TokKind.Ne ||
				k == TokKind.Colon || k == TokKind.NotColon;

			private static bool CanStartValue(TokKind k) =>
				k == TokKind.Ident || k == TokKind.String || k == TokKind.Number ||
				k == TokKind.LParen || k == TokKind.Minus;

			private void Consume(TokKind expected)
			{
				if (_cur.Kind == expected)
				{
					_cur = _lex.Next();
					return;
				}

				SetErrorOnce($"Expected {expected} but found {_cur.Kind} near '{_cur.Text}'.");
				_cur = _lex.Next(); // basic recovery
			}
		}

		// ----------------------------------------------------
		// 4) Row field access (tag -> value)
		// ----------------------------------------------------

		private static bool TryGetString(ItemViewRow r, string tag, out string value)
		{
			value = "";
			if (r == null) return false;

			switch (tag)
			{
				case "location": value = r.Location ?? ""; return true;
				case "quality": value = r.Quality ?? ""; return true;
				case "set": value = r.Set ?? ""; return true;
				case "type": value = r.Type ?? ""; return true;
				case "best": value = r.BestFor ?? ""; return true;
				case "name": value = r.Name ?? ""; return true;
				default: return false;
			}
		}

		private static bool TryGetNumber(ItemViewRow r, string tag, out double value)
		{
			value = 0;
			if (r == null) return false;

			switch (tag)
			{
				case "thp": value = r.Stats[(int)DDStat.TowerHealth]; return true;
				case "tdmg": value = r.Stats[(int)DDStat.TowerDamage]; return true;
				case "trate": value = r.Stats[(int)DDStat.TowerRate]; return true;
				case "trange": value = r.Stats[(int)DDStat.TowerRange]; return true;

				case "hhp": value = r.Stats[(int)DDStat.HeroHealth]; return true;
				case "hdmg": value = r.Stats[(int)DDStat.HeroDamage]; return true;
				case "hrate": value = r.Stats[(int)DDStat.HeroCastRate]; return true;
				case "hspd": value = r.Stats[(int)DDStat.HeroSpeed]; return true;

				case "ab1": value = r.Stats[(int)DDStat.HeroAbility1]; return true;
				case "ab2": value = r.Stats[(int)DDStat.HeroAbility2]; return true;

				case "resg": value = r.Resists[0]; return true;
				case "resp": value = r.Resists[1]; return true;
				case "resf": value = r.Resists[2]; return true;
				case "resl": value = r.Resists[3]; return true;

				case "ressum": value = r.Resists[0] + r.Resists[1] + r.Resists[2] + r.Resists[3]; return true;
				case "resavg": value = (r.Resists[0] + r.Resists[1] + r.Resists[2] + r.Resists[3]) / 4.0; return true;

				case "rating": value = r.Rating; return true;
				case "sides": value = r.Sides; return true;
				case "lvl": value = r.Level; return true;
				case "maxlvl": value = r.MaxLevel; return true;
				case "value": value = r.Value; return true;

				default: return false;
			}
		}

		// ----------------------------------------------------
		// 5) Integration: ApplySearch
		// ----------------------------------------------------
		
		static string[] Tokenize(string input)
		{
			var tokens = new List<string>();

			var regex = new Regex(
				@"(?<token>-?\w+(?:<=|>=|!=|==|<|>|:)=?""[^""]*"")|""[^""]*""|\S+",
				RegexOptions.Compiled);

			foreach (Match m in regex.Matches(input))
			{
				string token = m.Value;

				// Strip quotes ONLY around the value, not the whole token
				if (token.Contains(":\""))
				{
					int i = token.IndexOf(":\"", StringComparison.Ordinal);
					token = token.Substring(0, i + 1) + token.Substring(i + 2, token.Length - i - 3);
				}
				else if (token.StartsWith("\"") && token.EndsWith("\""))
				{
					token = token.Substring(1, token.Length - 2);
				}

				tokens.Add(token);
			}

			return tokens.ToArray();
		}
	}
	}
