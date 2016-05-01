using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcmaScriptCompiler.Syntax
{
    public enum SyntaxKind
    {
        None = 0,
        Invalid = 1, /* NOTE: we should throw/log an error if this ever happens! */

        SyntaxTreeRoot,     // syntax tree root
        CompilationUnit,    // compilation unit (source code file)

        // simple punctuation
        OpenBraceToken,     // {
        CloseBraceToken,    // }
        OpenParenToken,     // (
        CloseParenToken,    // )
        OpenBracketToken,   // [
        CloseBracketToken,  // ]
        DotToken,           // .
        SemicolonToken,     // ;
        CommaToken,         // ,
        LessThanToken,      // <
        GreaterThanToken,   // >
        PlusToken,          // +
        MinusToken,         // -
        AsteriskToken,      // *
        PercentToken,       // %
        AmpersandToken,     // &
        BarToken,           // |
        CaretToken,         // ^
        ExclamationToken,   // !
        TildeToken,         // ~
        QuestionToken,      // ?
        ColonToken,         // :
        EqualsToken,        // =
        SlashToken,         // /

        // compound punctuation
        DotDotDotToken,                                 // ...
        LessThanEqualsToken,                            // <=
        LessThanLessThanToken,                          // <<
        LessThanLessThanEqualsToken,                    // <<=
        GreaterThanEqualsToken,                         // >=
        GreaterThanGreaterThanToken,                    // >>
        GreaterThanGreaterThanEqualsToken,              // >>=
        GreaterThanGreaterThanGreaterThanToken,         // >>>
        GreaterThanGreaterThanGreaterThanEqualsToken,   // >>>=
        EqualsEqualsToken,                              // ==
        EqualsEqualsEqualsToken,                        // ===
        EqualsGreaterThanToken,                         // =>
        ExclamationEqualsToken,                         // !=
        ExclamationEqualsEqualsToken,                   // !==
        PlusEqualsToken,                                // +=
        PlusPlusToken,                                  // ++
        MinusEqualsToken,                               // -=
        MinusMinusToken,                                // --
        AsteriskEqualsToken,                            // *=
        PercentEqualsToken,                             // %=
        AmpersandEqualsToken,                           // &=
        AmpersandAmpersandToken,                        // &&
        BarEqualsToken,                                 // |=
        BarBarToken,                                    // ||
        CaretEqualsToken,                               // ^=
        SlashEqualsToken,                               // /=

        // basic token types
        NullLiteralToken,
        BooleanLiteralToken,
        NumericLiteralToken,
        StringLiteralToken,
        IdentifierToken,
        KeywordToken,
        FutureReservedWordToken,

        // trivia token types
        SingleLineCommentTrivia,
        MultiLineCommentTrivia,
        EndOfLineTrivia,
        WhitespaceTrivia,
    }

    public class SyntaxKindPunctuationAsString
    {
        public static string ConvertToString(SyntaxKind kind)
        {
            switch (kind)
            {
                // simple punctuation
                case SyntaxKind.OpenBraceToken:
                    return "{";
                case SyntaxKind.CloseBraceToken:
                    return "}";
                case SyntaxKind.OpenParenToken:
                    return "(";
                case SyntaxKind.CloseParenToken:
                    return ")";
                case SyntaxKind.OpenBracketToken:
                    return "[";
                case SyntaxKind.CloseBracketToken:
                    return "]";
                case SyntaxKind.DotToken:
                    return ".";
                case SyntaxKind.SemicolonToken:
                    return ";";
                case SyntaxKind.CommaToken:
                    return ",";
                case SyntaxKind.LessThanToken:
                    return "<";
                case SyntaxKind.GreaterThanToken:
                    return ">";
                case SyntaxKind.PlusToken:
                    return "+";
                case SyntaxKind.MinusToken:
                    return "-";
                case SyntaxKind.AsteriskToken:
                    return "*";
                case SyntaxKind.PercentToken:
                    return "%";
                case SyntaxKind.AmpersandToken:
                    return "&";
                case SyntaxKind.BarToken:
                    return "|";
                case SyntaxKind.CaretToken:
                    return "^";
                case SyntaxKind.ExclamationToken:
                    return "!";
                case SyntaxKind.TildeToken:
                    return "~";
                case SyntaxKind.QuestionToken:
                    return "?";
                case SyntaxKind.ColonToken:
                    return ":";
                case SyntaxKind.EqualsToken:
                    return "=";
                case SyntaxKind.SlashToken:
                    return "/";
                // compound punctuation
                case SyntaxKind.DotDotDotToken:
                    return "...";
                case SyntaxKind.LessThanEqualsToken:
                    return "<=";
                case SyntaxKind.LessThanLessThanToken:
                    return "<<";
                case SyntaxKind.LessThanLessThanEqualsToken:
                    return "<<=";
                case SyntaxKind.GreaterThanEqualsToken:
                    return ">=";
                case SyntaxKind.GreaterThanGreaterThanToken:
                    return ">>";
                case SyntaxKind.GreaterThanGreaterThanEqualsToken:
                    return ">>=";
                case SyntaxKind.GreaterThanGreaterThanGreaterThanToken:
                    return ">>>";
                case SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken:
                    return ">>>=";
                case SyntaxKind.EqualsEqualsToken:
                    return "==";
                case SyntaxKind.EqualsEqualsEqualsToken:
                    return "===";
                case SyntaxKind.EqualsGreaterThanToken:
                    return "=>";
                case SyntaxKind.ExclamationEqualsToken:
                    return "!=";
                case SyntaxKind.ExclamationEqualsEqualsToken:
                    return "!==";
                case SyntaxKind.PlusEqualsToken:
                    return "+=";
                case SyntaxKind.PlusPlusToken:
                    return "++";
                case SyntaxKind.MinusEqualsToken:
                    return "-=";
                case SyntaxKind.MinusMinusToken:
                    return "--";
                case SyntaxKind.AsteriskEqualsToken:
                    return "*=";
                case SyntaxKind.PercentEqualsToken:
                    return "%=";
                case SyntaxKind.AmpersandEqualsToken:
                    return "&=";
                case SyntaxKind.AmpersandAmpersandToken:
                    return "&&";
                case SyntaxKind.BarEqualsToken:
                    return "|=";
                case SyntaxKind.BarBarToken:
                    return "||";
                case SyntaxKind.CaretEqualsToken:
                    return "^=";
                case SyntaxKind.SlashEqualsToken:
                    return "/=";
                default:
                    return "";
            }
        }
    }
}
