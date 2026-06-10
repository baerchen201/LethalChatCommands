using System.Collections.Generic;
using System.Text;

namespace ChatCommandAPI.Utils;

public static class Args
{
    public static IEnumerable<string> Parse(string args, uint? max = null, bool escape = true)
    {
        args = args.TrimEnd();
        var q = QuotationMarks.None;
        var sb = new StringBuilder();
        var escaped = false;
        var i = 0;
        foreach (var c in args)
        {
            ++i;
            switch (c)
            {
                case '"':
                    if (escaped)
                    {
                        sb.Append(c);
                        escaped = false;
                    }
                    else
                    {
                        switch (q)
                        {
                            case QuotationMarks.None:
                                if (sb.Length <= 0)
                                    q = QuotationMarks.Double;
                                else
                                    sb.Append(c);
                                break;
                            case QuotationMarks.Double:
                                yield return sb.ToString();
                                if (max != null && --max == 0)
                                    goto stop;
                                sb.Clear();
                                q = QuotationMarks.PostDouble;
                                break;
                            case QuotationMarks.Single:
                                sb.Append(c);
                                break;
                            default:
                                throw new InvalidArgumentsException();
                        }
                    }

                    break;
                case '\'':
                    if (escaped)
                    {
                        sb.Append(c);
                        escaped = false;
                    }
                    else
                    {
                        switch (q)
                        {
                            case QuotationMarks.None:
                                if (sb.Length <= 0)
                                    q = QuotationMarks.Single;
                                else
                                    sb.Append(c);
                                break;
                            case QuotationMarks.Single:
                                yield return sb.ToString();
                                if (max != null && --max == 0)
                                    goto stop;
                                sb.Clear();
                                q = QuotationMarks.PostSingle;
                                break;
                            case QuotationMarks.Double:
                                sb.Append(c);
                                break;
                            default:
                                throw new InvalidArgumentsException();
                        }
                    }

                    break;
                case '\\':
                    if (escaped || !escape)
                    {
                        sb.Append(c);
                        escaped = false;
                    }
                    else
                    {
                        escaped = escape;
                    }

                    break;
                default:
                    if (char.IsWhiteSpace(c))
                        switch (q)
                        {
                            case QuotationMarks.PostDouble:
                            case QuotationMarks.PostSingle:
                                q = QuotationMarks.None;
                                break;
                            case QuotationMarks.None:
                                if (escaped)
                                {
                                    sb.Append(c);
                                    escaped = false;
                                }
                                else if (sb.Length > 0)
                                {
                                    yield return sb.ToString();
                                    if (max != null && --max == 0)
                                        goto stop;
                                    sb.Clear();
                                }

                                break;
                            default:
                                sb.Append(c);
                                break;
                        }
                    else
                        switch (q)
                        {
                            case QuotationMarks.PostDouble:
                            case QuotationMarks.PostSingle:
                                throw new InvalidArgumentsException();
                            default:
                                if (escaped)
                                {
                                    sb.Append(
                                        c switch
                                        {
                                            '0' => '\0',
                                            'a' => '\a',
                                            'b' => '\b',
                                            'e' => '\e',
                                            'f' => '\f',
                                            'n' => '\n',
                                            'r' => '\r',
                                            't' => '\t',
                                            'v' => '\v',
                                            // for now u, U and x are not supported because i'm lazy
                                            // TODO: IMPLEMENT unicode escapes
                                            _ => throw new InvalidArgumentsException(),
                                        }
                                    );
                                    escaped = false;
                                }
                                else
                                {
                                    sb.Append(c);
                                }

                                break;
                        }

                    break;
            }
        }

        switch (q)
        {
            case QuotationMarks.None:
            case QuotationMarks.PostDouble:
            case QuotationMarks.PostSingle:
                if (sb.Length > 0)
                    yield return sb.ToString();

                break;
            default:
                throw new InvalidArgumentsException();
        }

        yield break;

        stop:
        var remainder = args[i..];
        if (remainder.Length > 0)
            yield return remainder.Trim();
    }

    private enum QuotationMarks : byte
    {
        None,
        Double,
        PostDouble,
        Single,
        PostSingle,
    }
}
