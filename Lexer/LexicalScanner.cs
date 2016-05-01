using EcmaScriptCompiler.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcmaScriptCompiler.Lexer
{
    /* NOTE: the lexical analyzer builds a list of syntax tree items which represent strings, numbers, comments, identifiers, 
     *       whitespace, end of line characters, etc.  These are represented as syntax tokens and (whitespace/EOL) trivia. */
    public class LexicalScanner
    {
        const char CHARACTER_EOF = (char)0xFFFF;

        SyntaxNode _tree = null;
        string _sourceCode = "";
        int _currentPosition = 0;

        public LexicalScanner(string sourceCode)
        {
            _sourceCode = sourceCode;
        }

        /* this function analyzes the source code to create a basic list of tokens and trivia; 
         * in a full compiler, these results are typically then be processed by a syntax analyzer (tree builder) */
        public SyntaxNode Scan(SyntaxNode parent)
        {
            _currentPosition = 0;
            int sourceCodeLen = _sourceCode.Length;

            _tree = new SyntaxNode(parent, SyntaxKind.CompilationUnit, 0);

            while (_currentPosition < sourceCodeLen)
            {
                SyntaxTreeItem treeItem = ScanCurrentSyntaxTreeItemAndAdvance();
                _tree.Children.Add(treeItem);
            }

            return _tree;
        }

        SyntaxTreeItem ScanCurrentSyntaxTreeItemAndAdvance()
        {
            int currentTokenPosition = _currentPosition;
            string tokenOrTriviaText;

            // first, check for string literals
            char quoteCharacter;
            string stringLiteralValue;
            double numericLiteral;
            if (GetCurrentStringLiteralAndAdvance(out tokenOrTriviaText, out quoteCharacter, out stringLiteralValue))
            {
                return new StringLiteralToken(_tree, currentTokenPosition, tokenOrTriviaText, quoteCharacter, stringLiteralValue);
            }

            // next, check for identifiers
            string identifierValue;
            bool originalTextIncludesUnicodeEscapeSequence;
            if (GetCurrentIdentifierNameAndAdvance(out tokenOrTriviaText, out identifierValue, out originalTextIncludesUnicodeEscapeSequence))
            {
                /* IdentifierNames can be broken down into five categories:
                 * 1. Keywords -- ECMAScript keywords, cannot ever be used as tokens
                 * 2. FutureReservedWords -- reserved to be keywords in future versions of ECMAScript
                 * 3. NullLiterals -- the string 'null'
                 * 4. BooleanLiterals -- the strings 'true' and 'false'
                 * 5. Identifiers -- types, functions, variables, etc. -- string identifiers which are not otherwise reserved */
                 /* NOTE: the strings 'yield', 'let', 'static', 'await' and others are reserved in certain contexts; 
                  *       we will convert them from identifers to keywords/reserved words in those appropriate contexts. */
                 /* NOTE: the ECMAScript 6 spec indicates that reserved words cannot contain characters represented by unicode escape
                  *       sequence--but it does not say if those names are instead valid identifiers.  We are assuming they are errors. */
                switch (tokenOrTriviaText)
                {
                    case "null":
                        {
                            if (originalTextIncludesUnicodeEscapeSequence)
                                return new InvalidSyntaxToken(_tree, currentTokenPosition, tokenOrTriviaText);

                            return new NullLiteralToken(_tree, currentTokenPosition, tokenOrTriviaText);
                        }
                    case "true":
                    case "false":
                        {
                            if (originalTextIncludesUnicodeEscapeSequence)
                                return new InvalidSyntaxToken(_tree, currentTokenPosition, tokenOrTriviaText);

                            return new BooleanLiteralToken(_tree, currentTokenPosition, tokenOrTriviaText,
                                tokenOrTriviaText == "true" ? true : false);
                        }
                    // case "arguments": /* NOTE: can be used as an identifier in some semantics; deal with this in semantic analysis */
                    case "break":
                    case "case":
                    case "catch":
                    case "class":
                    case "const":
                    case "continue":
                    case "debugger":
                    case "default":
                    case "delete":
                    case "do":
                    case "else":
                    //case "eval": /* NOTE: can be used as an identifier in some semantics; deal with this in semantic analysis */
                    case "export":
                    case "extends":
                    case "finally":
                    case "for":
                    case "function":
                    case "if":
                    //case "implements": /* NOTE: can be used as an identifier in some semantics; deal with this in semantic analysis */
                    case "import":
                    case "in":
                    case "instanceof":
                    //case "interface": /* NOTE: can be used as an identifier in some semantics; deal with this in semantic analysis */
                    //case "let": /* NOTE: can be used as an identifier in some semantics; deal with this in semantic analysis */
                    case "new":
                    //case "package": /* NOTE: can be used as an identifier in some semantics; deal with this in semantic analysis */
                    //case "private": /* NOTE: can be used as an identifier in some semantics; deal with this in semantic analysis */
                    //case "protected": /* NOTE: can be used as an identifier in some semantics; deal with this in semantic analysis */
                    //case "public": /* NOTE: can be used as an identifier in some semantics; deal with this in semantic analysis */
                    case "return":
                    //case "static": /* NOTE: can be used as an identifier in some semantics; deal with this in semantic analysis */
                    case "super":
                    case "switch":
                    case "this":
                    case "throw":
                    case "try":
                    case "typeof":
                    case "var":
                    case "void":
                    case "while":
                    case "with":
                    //case "yield": /* NOTE: can be used as an identifier in some semantics; deal with this in semantic analysis */
                        {
                            if (originalTextIncludesUnicodeEscapeSequence)
                                return new InvalidSyntaxToken(_tree, currentTokenPosition, tokenOrTriviaText);

                            return new KeywordToken(_tree, currentTokenPosition, tokenOrTriviaText, identifierValue);
                        }
                    case "enum":
                    //case "await": /* NOTE: can be used as an identifier in some semantics; deal with this in semantic analysis */
                        {
                            if (originalTextIncludesUnicodeEscapeSequence)
                                return new InvalidSyntaxToken(_tree, currentTokenPosition, tokenOrTriviaText);

                            return new FutureReservedWordToken(_tree, currentTokenPosition, tokenOrTriviaText, identifierValue);
                        }
                    default:
                        {
                            // all other identifier names are valid identifiers.
                            return new IdentifierToken(_tree, currentTokenPosition, tokenOrTriviaText, identifierValue);
                        }
                }

            }

            // next, check for numeric literals
            /* TODO: should we be processing unicode '\u####' character substitutions for numeric literals? */
                if (GetCurrentNumericLiteralAndAdvance(out tokenOrTriviaText, out numericLiteral))
            {
                return new NumericLiteralToken(_tree, currentTokenPosition, tokenOrTriviaText, numericLiteral);
            }

            // next, check for comments.  technically this could be combined with whitespace--but we want to store a separate token
            bool isMultilineComment;
            if (GetCurrentCommentAndAdvance(out tokenOrTriviaText, out isMultilineComment))
            {
                if (isMultilineComment)
                {
                    return new MultiLineCommentTrivia(_tree, currentTokenPosition, tokenOrTriviaText);
                }
                else
                {
                    return new SingleLineCommentTrivia(_tree, currentTokenPosition, tokenOrTriviaText);
                }
            }

            // next, check for LineTerminatorSequence (LF, CR, LS, PS, CRLF)  these are separate from whitespace for auto-semicolon insertion, etc.
            if (GetCurrentLineTerminatorSequenceAndAdvance(out tokenOrTriviaText))
            {
                /* NOTE: In case of automatic semicolon insertion, this EndOfLineTrivia will become leading trivia to an
                 *       implied semicolon; in other scenarios it is effectively treated as whitespace. */
                return new EndOfLineTrivia(_tree, currentTokenPosition, tokenOrTriviaText);
            }

            // next, check for (non-line-terminator) white space trivia
            /* NOTE: white space is not considered trivia when it is part of a:
             *       - string literal
             *       - regular expression literal
             *       - template
             *       - template substitution tail */
            if (GetCurrentWhiteSpaceAndAdvance(out tokenOrTriviaText))
            {
                return new WhitespaceTrivia(_tree, currentTokenPosition, tokenOrTriviaText);
            }

            char character = GetCurrentCharAndAdvance();
            // finally, check for punctuation
            switch (character)
            {
                case '{':
                    return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.OpenBraceToken);
                case '}':
                    return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.CloseBraceToken);
                case '(':
                    return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.OpenParenToken);
                case ')':
                    return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.CloseParenToken);
                case '[':
                    return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.OpenBracketToken);
                case ']':
                    return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.CloseBracketToken);
                case '.':
                    if (PeekCurrentChar(0) == '.' && PeekCurrentChar(1) == '.')
                    {
                        AdvancePosition(2);
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.DotDotDotToken);
                    }
                    else
                    {
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.DotToken);
                    }
                case ';':
                    return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.SemicolonToken);
                case ',':
                    return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.CommaToken);
                case '<':
                    if (PeekCurrentChar() == '=')
                    {
                        AdvancePosition();
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.LessThanEqualsToken);
                    }
                    else if (PeekCurrentChar() == '<')
                    {
                        AdvancePosition();
                        if (PeekCurrentChar() == '=')
                        {
                            AdvancePosition();
                            return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.LessThanLessThanEqualsToken);
                        }
                        else
                        {
                            return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.LessThanLessThanToken);
                        }
                    }
                    else
                    {
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.LessThanToken);
                    }
                case '>':
                    if (PeekCurrentChar() == '=')
                    {
                        AdvancePosition();
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.GreaterThanEqualsToken);
                    }
                    else if (PeekCurrentChar() == '>')
                    {
                        AdvancePosition();
                        if (PeekCurrentChar() == '=')
                        {
                            AdvancePosition();
                            return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.GreaterThanGreaterThanEqualsToken);
                        }
                        else if (PeekCurrentChar() == '>')
                        {
                            AdvancePosition();
                            if (PeekCurrentChar() == '=')
                            {
                                AdvancePosition();
                                return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken);
                            }
                            else
                            {
                                return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.GreaterThanGreaterThanGreaterThanToken);
                            }
                        }
                        else
                        {
                            return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.GreaterThanGreaterThanToken);
                        }
                    }
                    else
                    {
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.GreaterThanToken);
                    }
                case '+':
                    if (PeekCurrentChar() == '=')
                    {
                        AdvancePosition();
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.PlusEqualsToken);
                    }
                    else if (PeekCurrentChar() == '+')
                    {
                        AdvancePosition();
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.PlusPlusToken);
                    }
                    else
                    {
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.PlusToken);
                    }
                case '-':
                    if (PeekCurrentChar() == '=')
                    {
                        AdvancePosition();
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.MinusEqualsToken);
                    }
                    else if (PeekCurrentChar() == '-')
                    {
                        AdvancePosition();
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.MinusMinusToken);
                    }
                    else
                    {
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.MinusToken);
                    }
                case '*':
                    if (PeekCurrentChar() == '=')
                    {
                        AdvancePosition();
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.AsteriskEqualsToken);
                    }
                    else
                    {
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.AsteriskToken);
                    }
                case '%':
                    if (PeekCurrentChar() == '=')
                    {
                        AdvancePosition();
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.PercentEqualsToken);
                    }
                    else
                    {
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.PercentToken);
                    }
                case '&':
                    if (PeekCurrentChar() == '=')
                    {
                        AdvancePosition();
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.AmpersandEqualsToken);
                    }
                    else if (PeekCurrentChar() == '&')
                    {
                        AdvancePosition();
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.AmpersandAmpersandToken);
                    }
                    else
                    {
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.AmpersandToken);
                    }
                case '|':
                    if (PeekCurrentChar() == '=')
                    {
                        AdvancePosition();
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.BarEqualsToken);
                    }
                    else if (PeekCurrentChar() == '|')
                    {
                        AdvancePosition();
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.BarBarToken);
                    }
                    else
                    {
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.BarToken);
                    }
                case '^':
                    if (PeekCurrentChar() == '=')
                    {
                        AdvancePosition();
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.CaretEqualsToken);
                    }
                    else
                    {
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.CaretToken);
                    }
                case '!':
                    if (PeekCurrentChar() == '=')
                    {
                        AdvancePosition();
                        if (PeekCurrentChar() == '=')
                        {
                            AdvancePosition();
                            return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.ExclamationEqualsEqualsToken);
                        }
                        else
                        {
                            return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.ExclamationEqualsToken);
                        }
                    }
                    else
                    {
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.ExclamationToken);
                    }
                case '~':
                    return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.TildeToken);
                case '?':
                    return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.QuestionToken);
                case ':':
                    return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.ColonToken);
                case '=':
                    if (PeekCurrentChar() == '=')
                    {
                        AdvancePosition();
                        if (PeekCurrentChar() == '=')
                        {
                            AdvancePosition();
                            return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.EqualsEqualsEqualsToken);
                        }
                        else
                        {
                            return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.EqualsEqualsToken);
                        }
                    }
                    else if (PeekCurrentChar() == '>')
                    {
                        AdvancePosition();
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.EqualsGreaterThanToken);
                    }
                    else
                    {
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.EqualsToken);
                    }
                case '/':
                    if (PeekCurrentChar() == '=')
                    {
                        AdvancePosition();
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.SlashEqualsToken);
                    }
                    else
                    {
                        return new PunctuatorToken(_tree, currentTokenPosition, SyntaxKind.SlashToken);
                    }
            }
            // if nothing matches, mark the token as invalid
            return new InvalidSyntaxToken(_tree, currentTokenPosition, character);
        }

        bool GetCurrentCommentAndAdvance(out string originalText, out bool isMultilineComment)
        {
            int originalPosition = _currentPosition; // store the original position so we can measure the total token width

            // default 'out' values
            originalText = "";
            isMultilineComment = false;

            // find out if this is a comment (either single-line or multi-line)
            if (PeekCurrentChar() != '/')
                return false;
            char character = PeekCurrentChar(1);
            if (character != '*' && character != '/')
                return false;

            StringBuilder originalTextBuilder = new StringBuilder();
            character = GetCurrentCharAndAdvance();
            originalTextBuilder.Append(character);

            // determine if this is a single-line comment or a multi-line comment
            character = GetCurrentCharAndAdvance();
            originalTextBuilder.Append(character);
            if (character == '*')
                isMultilineComment = true;

            // now, search for the end of the comment
            while (true)
            {
                character = PeekCurrentChar();
                if (isMultilineComment)
                {
                    if (character == '*' & PeekCurrentChar(1) == '/')
                    {
                        AdvancePosition();
                        originalTextBuilder.Append(character);
                        character = GetCurrentCharAndAdvance();
                        originalTextBuilder.Append(character);
                        break;
                    }
                    else if (character == CHARACTER_EOF)
                    {
                        // error; abort.
                        _currentPosition = originalPosition;
                        return false;
                    }
                    else
                    {
                        AdvancePosition();
                        originalTextBuilder.Append(character);
                    }
                }
                else // if (!isMultilineComment) /* i.e. if this is not a multi-line comment */
                {
                    if ((character == '\u000A' /* line feed */) ||
                        (character == '\u2028' /* line separator */) ||
                        (character == '\u2029' /* paragraph separator */) ||
                        (character == '\u000D' /* carriage return */) ||
                        (character == CHARACTER_EOF))
                    {
                        // end of comment
                        break;
                    }
                    else
                    {
                        AdvancePosition();
                        originalTextBuilder.Append(character);
                    }
                }
            }

            originalText = originalTextBuilder.ToString();
            return true;
        }

        bool GetCurrentLineTerminatorSequenceAndAdvance(out string originalText)
        {
            int originalPosition = _currentPosition; // store the original position so we can measure the total token width

            // default 'out' values
            originalText = "";

            StringBuilder originalTextBuilder = new StringBuilder();

            char character = PeekCurrentChar();
            switch (character)
            {
                case '\u000A': // line feed
                case '\u2028': // line separator
                case '\u2029': // paragraph separator
                    AdvancePosition();
                    originalTextBuilder.Append(character);
                    break;
                case '\u000D': // carriage return
                    AdvancePosition();
                    originalTextBuilder.Append(character);
                    character = PeekCurrentChar();
                    if (character == '\u000A') // carriage return + line feed
                    {
                        AdvancePosition();
                        originalTextBuilder.Append(character);
                    }
                    break;
                default:
                    return false;
            }

            originalText = originalTextBuilder.ToString();
            return true;
        }

        bool GetCurrentWhiteSpaceAndAdvance(out string originalText)
        {
            int originalPosition = _currentPosition; // store the original position so we can measure the total token width

            // default 'out' values
            originalText = "";

            StringBuilder originalTextBuilder = new StringBuilder();

            while (true)
            {
                char character = PeekCurrentChar();
                if ((character == '\u0009' /* tab */) ||
                    (character == '\u000B' /* vertical tab */) ||
                    (character == '\u000C' /* form feed */) ||
                    (character == '\u0020' /* space */) ||
                    (character == '\u00A0' /* no-break space */) ||
                    (character == '\uFEFF' /* zero width no-break space */) ||
                    (GetUnicodeCategory(character) == UnicodeCategory.SpaceSeparator))
                {
                    AdvancePosition();
                    originalTextBuilder.Append(character);
                }
                else
                {
                    // end of sequence (or no whitespace sequence found)
                    if (_currentPosition > originalPosition)
                    {
                        originalText = originalTextBuilder.ToString();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        bool GetCurrentNullLiteralAndAdvance(out string originalText)
        {
            StringBuilder originalTextBuilder = new StringBuilder();
            if ((PeekCurrentChar(0) == 'n') &&
                (PeekCurrentChar(1) == 'u') &&
                (PeekCurrentChar(2) == 'l') &&
                (PeekCurrentChar(3) == 'l'))
            {
                originalTextBuilder.Append(GetCurrentCharAndAdvance());
                originalTextBuilder.Append(GetCurrentCharAndAdvance());
                originalTextBuilder.Append(GetCurrentCharAndAdvance());
                originalTextBuilder.Append(GetCurrentCharAndAdvance());
                originalText = originalTextBuilder.ToString();
                return true;
            }

            originalText = "";
            return false;
        }

        bool GetCurrentBooleanLiteralAndAdvance(out string originalText)
        {
            StringBuilder originalTextBuilder = new StringBuilder();
            if ((PeekCurrentChar(0) == 't') &&
                (PeekCurrentChar(1) == 'r') &&
                (PeekCurrentChar(2) == 'u') &&
                (PeekCurrentChar(3) == 'e'))
            {
                originalTextBuilder.Append(GetCurrentCharAndAdvance());
                originalTextBuilder.Append(GetCurrentCharAndAdvance());
                originalTextBuilder.Append(GetCurrentCharAndAdvance());
                originalTextBuilder.Append(GetCurrentCharAndAdvance());
                originalText = originalTextBuilder.ToString();
                return true;
            }
            else if ((PeekCurrentChar(0) == 'f') &&
                (PeekCurrentChar(1) == 'a') &&
                (PeekCurrentChar(2) == 'l') &&
                (PeekCurrentChar(3) == 's') &&
                (PeekCurrentChar(4) == 'e'))
            {
                originalTextBuilder.Append(GetCurrentCharAndAdvance());
                originalTextBuilder.Append(GetCurrentCharAndAdvance());
                originalTextBuilder.Append(GetCurrentCharAndAdvance());
                originalTextBuilder.Append(GetCurrentCharAndAdvance());
                originalTextBuilder.Append(GetCurrentCharAndAdvance());
                originalText = originalTextBuilder.ToString();
                return true;
            }

            originalText = "";
            return false;
        }

        bool GetCurrentNumericLiteralAndAdvance(out string originalText, out double value)
        {
            int originalPosition = _currentPosition; // store the original position so we can measure the total token width...and in case our numberic literal is invalid and we need to revert state

            // default 'out' values
            originalText = "";
            value = 0;

            char character = PeekCurrentChar();
            // if the character is zero (and the second character is not .), first test to see if this is a binary/octal/hexadecimal representation of an integer
            // NOTE: a decimal number may start with zero--but only if the zero is the only digit before the decimal point.
            if (character == '0' && PeekCurrentChar(1) != '.')
            {
                character = PeekCurrentChar(1);
                switch (character)
                {
                    case 'b':
                    case 'B':
                        return GetCurrentNumberBaseIntegerNumericLiteralAndAdvance(2, out originalText, out value);
                    case 'o':
                    case 'O':
                        return GetCurrentNumberBaseIntegerNumericLiteralAndAdvance(8, out originalText, out value);
                    case 'x':
                    case 'X':
                        return GetCurrentNumberBaseIntegerNumericLiteralAndAdvance(16, out originalText, out value);
                    default:
                        // not a valid numeric literal; return false.
                        return false;
                }
            }
            else if (character == '.' && !char.IsDigit(PeekCurrentChar(1)))
            {
                // if the character is a dot followed by a non-digit, then this is not a numeric literal.
                return false;
            }
            else if (char.IsDigit(character) || character == '.')
            {
                StringBuilder originalTextBuilder = new StringBuilder();
                StringBuilder wholeNumberPartBuilder = new StringBuilder();
                StringBuilder fractionalPartBuilder = new StringBuilder();
                StringBuilder exponentPartBuilder = new StringBuilder();
                bool inFractionalPortion = false;
                bool inExponentPortion = false;

                AdvancePosition();
                originalTextBuilder.Append(character);
                // if we started out with a dot, then we're already past the whole number portion.
                if (character == '.')
                {
                    // if the number started with zero, then assume a zero
                    wholeNumberPartBuilder.Append('0');
                    inFractionalPortion = true;
                }
                else
                {
                    wholeNumberPartBuilder.Append(character);
                }

                while (true)
                {
                    // process each character
                    character = PeekCurrentChar();

                    // if we encountered a dot, move to the decimal portion.
                    if (character == '.' && !inFractionalPortion)
                    {
                        AdvancePosition();
                        originalTextBuilder.Append(character);
                        inFractionalPortion = true;
                    }
                    else
                    {
                        if (char.IsDigit(character))
                        {
                            AdvancePosition();
                            originalTextBuilder.Append(character);
                            if (inExponentPortion)
                            {
                                exponentPartBuilder.Append(character);
                            }
                            else if (inFractionalPortion)
                            {
                                fractionalPartBuilder.Append(character);
                            }
                            else
                            {
                                wholeNumberPartBuilder.Append(character);
                            }
                        }
                        else if ((character == 'e' || (character == 'E')) && !inExponentPortion)
                        {
                            bool validExponent = true;

                            AdvancePosition();
                            originalTextBuilder.Append(character);
                            inExponentPortion = true;
                            // if the next character is a plus or minus character, process it as the sign of the exponent.
                            character = PeekCurrentChar();
                            if (character == '+')
                            {
                                if (char.IsDigit(PeekCurrentChar(1)))
                                {
                                    AdvancePosition();
                                    originalTextBuilder.Append(character);
                                }
                                else
                                {
                                    validExponent = false;
                                }
                            }
                            else if (character == '-')
                            {
                                if (char.IsDigit(PeekCurrentChar(1)))
                                {
                                    AdvancePosition();
                                    originalTextBuilder.Append(character);
                                    exponentPartBuilder.Append('-');
                                }
                                else
                                {
                                    validExponent = false;
                                }
                            }
                            else if (char.IsDigit(character))
                            {
                                // exponent is valid; we'll process this character momentarily.
                            }
                            else
                            {
                                // not a valid character; exponent is not valid.
                                validExponent = false;
                            }

                            if (!validExponent)
                            {
                                // abort; the numeric literal is malformed.
                                _currentPosition = originalPosition;
                                return false;
                            }
                        }
                        else
                        {
                            /* we have reached the end of our numeric literal.  
                             * unless the following character is an IdentifierStart character (an invalid ending to our numeric literal), 
                             return the literal. */
                            if (IsIdentifierStartChar(character))
                            {
                                _currentPosition = originalPosition;
                                return false;
                            }
                            else
                            {
                                // process numeric literal whole/fractional/exponent portions and return our value.
                                /* step 1: if our numeric literal contains more than 20 signficant digits (digits in whole and fractional portions),
                                 *         then round up the 20th digit if necessary and delete all following digits */
                                bool roundUpSignificantPortion = false;
                                int shiftExponentLeftValue = 0;
                                if (wholeNumberPartBuilder.Length > 20)
                                {
                                    if (wholeNumberPartBuilder[20] >= '5' && wholeNumberPartBuilder[20] <= '9')
                                        roundUpSignificantPortion = true;
                                    shiftExponentLeftValue = wholeNumberPartBuilder.Length - 20;
                                    wholeNumberPartBuilder.Remove(20, shiftExponentLeftValue);
                                    fractionalPartBuilder.Clear();
                                }
                                else if (wholeNumberPartBuilder.Length + fractionalPartBuilder.Length > 20)
                                {
                                    int newFractionalSignificantDigits = 20 - wholeNumberPartBuilder.Length;
                                    if (fractionalPartBuilder[newFractionalSignificantDigits] >= '5' && fractionalPartBuilder[newFractionalSignificantDigits] <= '9')
                                        roundUpSignificantPortion = true;
                                    fractionalPartBuilder.Remove(newFractionalSignificantDigits, fractionalPartBuilder.Length - newFractionalSignificantDigits);
                                }
                                if (roundUpSignificantPortion)
                                {
                                    bool carryOne = roundUpSignificantPortion;
                                    for (int i = fractionalPartBuilder.Length - 1; i >= 0; i--)
                                    {
                                        if (fractionalPartBuilder[i] == '9')
                                        {
                                            fractionalPartBuilder[i] = '0';
                                            carryOne = true;
                                        }
                                        else
                                        {
                                            fractionalPartBuilder[i] = (char)((int)fractionalPartBuilder[i] + 1);
                                            carryOne = false;
                                        }

                                        if (!carryOne)
                                            break;
                                    }

                                    if (carryOne)
                                    {
                                        for (int i = wholeNumberPartBuilder.Length - 1; i >= 0; i--)
                                        {
                                            if (wholeNumberPartBuilder[i] == '9')
                                            {
                                                wholeNumberPartBuilder[i] = '0';
                                                carryOne = true;
                                            }
                                            else
                                            {
                                                wholeNumberPartBuilder[i] = (char)((int)wholeNumberPartBuilder[i] + 1);
                                                carryOne = false;
                                            }

                                            if (!carryOne)
                                                break;
                                        }
                                    }

                                    if (carryOne)
                                    {
                                        // NOTE: this is the edge case where our significant portion was 20 digits of '9's.
                                        wholeNumberPartBuilder = new StringBuilder("1" + wholeNumberPartBuilder.ToString());
                                        if (wholeNumberPartBuilder.Length + fractionalPartBuilder.Length > 20)
                                        {
                                            if (fractionalPartBuilder.Length > 0)
                                                fractionalPartBuilder.Remove(fractionalPartBuilder.Length - 1, 1);
                                            else
                                                wholeNumberPartBuilder.Remove(wholeNumberPartBuilder.Length - 1, 1);
                                        }
                                        carryOne = false;
                                    }
                                }

                                originalText = originalTextBuilder.ToString();
                                value = double.Parse(wholeNumberPartBuilder.ToString() +
                                    ((fractionalPartBuilder.Length > 0) ? "." + fractionalPartBuilder.ToString() : "") +
                                    ((exponentPartBuilder.Length > 0) ? "E" + (int.Parse(exponentPartBuilder.ToString()) - shiftExponentLeftValue).ToString() : ""));
                                return true;
                            }
                        }
                    }
                }
            }
            else
            {
                // next character is not a digit {0...9} and therefore cannot be the start of a numeric literal
                return false;
            }
        }

        bool GetCurrentNumberBaseIntegerNumericLiteralAndAdvance(int numberBase, out string originalText, out double value)
        {
            int originalPosition = _currentPosition; // store the original position so we can measure the total token width...and in case our numberic literal is invalid and we need to revert state

            // default 'out' values
            originalText = "";
            value = 0;

            // make sure that this literal starts with the required characters (starting with 0)
            char character = PeekCurrentChar();
            if (character != '0')
                return false;
            // and make sure that this is a hexadecimal numeric literal
            character = PeekCurrentChar(1);
            if (numberBase == 2)
            {
                if ((character == 'b' || character == 'B') == false)
                    return false;
            }
            else if (numberBase == 8)
            {
                if ((character == 'o' || character == 'O') == false)
                    return false;
            }
            else if (numberBase == 16)
            {
                if ((character == 'x' || character == 'X') == false)
                    return false;
            }
            else
            {
                // invalid number base
                return false;
            }
            // finally, make sure we have at least one actual binary digit
            character = PeekCurrentChar(2);
            if (numberBase == 2)
            {
                if (((character >= '0' && character <= '1')) == false)
                {
                    return false;
                }
            }
            else if (numberBase == 8)
            {
                if (((character >= '0' && character <= '7')) == false)
                {
                    return false;
                }
            }
            else if (numberBase == 16)
            {
                if ((
                    (character >= '0' && character <= '9') ||
                    (character >= 'a' && character <= 'f') ||
                    (character >= 'A' && character <= 'F')
                    ) == false)
                {
                    return false;
                }
            }

            StringBuilder originalTextBuilder = new StringBuilder();
            // append our leading '0{b,B,o,O,x,X}' value to the text string.
            originalTextBuilder.Append(GetCurrentCharAndAdvance());
            originalTextBuilder.Append(GetCurrentCharAndAdvance());

            value = 0;
            while (true)
            {
                bool characterIsValid = false;
                character = PeekCurrentChar();
                if (numberBase == 2)
                {
                    if ((character >= '0' && character <= '1'))
                    {
                        characterIsValid = true;
                        value *= 8;
                        value += (double)(int.Parse(character.ToString()));
                    }
                }
                else if (numberBase == 8)
                {
                    if ((character >= '0' && character <= '7'))
                    {
                        characterIsValid = true;
                        value *= 8;
                        value += (double)(int.Parse(character.ToString()));
                    }
                }
                else if (numberBase == 16)
                {
                    if ((character >= '0' && character <= '9') ||
                        (character >= 'a' && character <= 'f') ||
                        (character >= 'A' && character <= 'F'))
                    {
                        characterIsValid = true;
                        value *= 16;
                        value += (double)(int.Parse(character.ToString(), NumberStyles.HexNumber));
                    }
                }

                if (characterIsValid)
                {
                    originalTextBuilder.Append(character);
                    AdvancePosition();
                }
                else if (IsIdentifierStartChar(character))
                {
                    // invalid character!  ABORT!
                    _currentPosition = originalPosition;
                    return false;
                }
                else
                {
                    // end of numeric literal
                    originalText = originalTextBuilder.ToString();
                    return true;
                }
            }
        }

        bool GetCurrentStringLiteralAndAdvance(out string originalText, out char quoteCharacter, out string value)
        {
            int originalPosition = _currentPosition; // store the original position so we can measure the total token width

            char character = PeekCurrentChar();
            if (character == '\"' || character == '\'')
            {
                StringBuilder originalTextBuilder = new StringBuilder();

                AdvancePosition();
                originalTextBuilder.Append(character);
                quoteCharacter = character;

                StringBuilder literalStringBuilder = new StringBuilder();

                while (true)
                {
                    character = PeekCurrentChar();
                    if (character == quoteCharacter)
                    {
                        AdvancePosition();
                        originalTextBuilder.Append(character);
                        originalText = originalTextBuilder.ToString();
                        value = literalStringBuilder.ToString();
                        return true;
                    }

                    // parse the next character
                    bool characterIsLineContinuationSequence = false;
                    /* NOTE: at the end of this section, character must refer to the character we are appending to the StringBuilder */
                    character = PeekCurrentChar();
                    if (character == '\\')
                    {
                        if (PeekCurrentChar(1) == 'u')
                        {
                            // this represents a unicode character; interpret the unicode character value instead
                            string escapeEncodingText = "";
                            bool isUnicodeEscapeEncoded = GetCurrentUnicodeEncodedCharAndAdvance(out character, out escapeEncodingText);
                            if (isUnicodeEscapeEncoded)
                            {
                                originalTextBuilder.Append(escapeEncodingText);
                            }
                            else
                            {
                                // bad encoding.  do not interpret.  retrieve the current character again instead.
                                character = GetCurrentCharAndAdvance();
                                originalTextBuilder.Append(character);
                            }
                        }
                        else if (PeekCurrentChar(1) == 'x')
                        {
                            // this represents a hex-encoded character; interpret the hex-encoded character value instead
                            string escapeEncodingText = "";
                            bool isHexEscapeEncoded = GetCurrentHexEncodedCharAndAdvance(out character, out escapeEncodingText);
                            if (isHexEscapeEncoded)
                            {
                                originalTextBuilder.Append(escapeEncodingText);
                            }
                            else
                            {
                                // bad encoding.  do not interpret.  retrieve the current character again instead.
                                character = GetCurrentCharAndAdvance();
                                originalTextBuilder.Append(character);
                            }
                        }
                        else
                        {
                            originalTextBuilder.Append(character);

                            // process all other escape codes
                            AdvancePosition();
                            character = GetCurrentCharAndAdvance();
                            switch (character)
                            {
                                case '\u000A': // LF
                                case '\u2028': // LS
                                case '\u2029': // PS
                                    originalTextBuilder.Append(character);
                                    characterIsLineContinuationSequence = true;
                                    break;
                                case '\u000D': // CR
                                    originalTextBuilder.Append(character);
                                    characterIsLineContinuationSequence = true;
                                    /* '\' followed by CrLf is also a valid line continuation sequence; peek ahead and remove the LF if it is also present */
                                    if (PeekCurrentChar() == '\u000A')
                                    {
                                        character = GetCurrentCharAndAdvance();
                                        originalTextBuilder.Append(character);
                                    }
                                    break;
                                case '0':
                                    originalTextBuilder.Append(character);
                                    character = '\u0000'; // null character
                                    break;
                                case 'b':
                                    originalTextBuilder.Append(character);
                                    character = '\u0008'; // backspace
                                    break;
                                case 'f':
                                    originalTextBuilder.Append(character);
                                    character = '\u000C'; // form feed
                                    break;
                                case 'n':
                                    originalTextBuilder.Append(character);
                                    character = '\u000A'; // line feed
                                    break;
                                case 'r':
                                    originalTextBuilder.Append(character);
                                    character = '\u000D'; // carriage return
                                    break;
                                case 't':
                                    originalTextBuilder.Append(character);
                                    character = '\u0009'; // horizontal tab
                                    break;
                                case 'v':
                                    originalTextBuilder.Append(character);
                                    character = '\u000B'; // vertical tab
                                    break;
                                case '\"':
                                case '\'':
                                case '\\':
                                    originalTextBuilder.Append(character);
                                    // these characters can be represented as-is.
                                    break;
                                default:
                                    originalTextBuilder.Append(character);
                                    // if any other character follows a backslash, just pass it through.
                                    break;
                            }
                        }
                    }
                    else
                    {
                        // this is not an escape sequence; make sure it's a valid character.
                        originalTextBuilder.Append(character);
                        switch (character)
                        {
                            case '\u000A': // LF
                            case '\u2028': // LS
                            case '\u2029': // PS
                            case '\u000D': // CR
                                // invalid in a string sequence; abort.  /* NOTE: in a future implementation, perhaps warn the user? */
                                _currentPosition = originalPosition;
                                quoteCharacter = '\0'; // an arbitrary value; we have to set _something_
                                value = "";
                                originalText = "";
                                return false;
                            default:
                                // valid character; move position forward.
                                AdvancePosition();
                                break;
                        }
                    }
                    /* if our character was not a line continuation sequence, append it to our StringBuilder */
                    if (!characterIsLineContinuationSequence)
                    {
                        literalStringBuilder.Append(character);
                    }
                }
            }
            else
            {
                // the current characters do not represent a string literal; return false.
                quoteCharacter = '\0'; // an arbitrary value; we have to set _something_
                value = "";
                originalText = "";
                return false;
            }
        }

        bool PeekCurrentUnicodeEncodedChar(out char character, out string escapeEncodingText)
        {
            /* NOTE: The ECMAScript spec indicates:
             *       "It is a Syntax Error if the MV of HexDigits > 1114111."
             */

            character = '\0'; // dummy default character

            if (PeekCurrentChar() == '\\' && PeekCurrentChar(1) == 'u')
            {
                if (IsHexDigit(PeekCurrentChar(2)) &&
                    IsHexDigit(PeekCurrentChar(3)) &&
                    IsHexDigit(PeekCurrentChar(4)) &&
                    IsHexDigit(PeekCurrentChar(5)))
                {
                    character = (char)
                        (
                        ((int.Parse(PeekCurrentChar(2).ToString(), NumberStyles.HexNumber)) << 12) +
                        ((int.Parse(PeekCurrentChar(3).ToString(), NumberStyles.HexNumber)) << 8) +
                        ((int.Parse(PeekCurrentChar(4).ToString(), NumberStyles.HexNumber)) << 4) +
                         (int.Parse(PeekCurrentChar(5).ToString(), NumberStyles.HexNumber))
                        );

                    if ((int)character <= 1114111)
                    {
                        return (PeekCurrentString(6, out escapeEncodingText));
                    }
                }
                else if ((PeekCurrentChar(2) == '{') &&
                    IsHexDigit(PeekCurrentChar(3)) &&
                    IsHexDigit(PeekCurrentChar(4)) &&
                    IsHexDigit(PeekCurrentChar(5)) &&
                    IsHexDigit(PeekCurrentChar(6)) &&
                    (PeekCurrentChar(7) == '}'))
                {
                    character = (char)
                        (
                        ((int.Parse(PeekCurrentChar(3).ToString(), NumberStyles.HexNumber)) << 12) +
                        ((int.Parse(PeekCurrentChar(4).ToString(), NumberStyles.HexNumber)) << 8) +
                        ((int.Parse(PeekCurrentChar(5).ToString(), NumberStyles.HexNumber)) << 4) +
                         (int.Parse(PeekCurrentChar(6).ToString(), NumberStyles.HexNumber))
                        );

                    if ((int)character <= 1114111)
                    {
                        return (PeekCurrentString(6, out escapeEncodingText));
                    }
                }
            }

            escapeEncodingText = "";
            return false;
        }

        bool GetCurrentUnicodeEncodedCharAndAdvance(out char character, out string escapeEncodingText)
        {
            if (PeekCurrentUnicodeEncodedChar(out character, out escapeEncodingText) == true)
            {
                AdvancePosition(escapeEncodingText.Length);
                return true;
            }

            return false;
        }

        bool PeekCurrentHexEncodedChar(out char character, out string escapeEncodingText)
        {
            character = '\0'; // dummy default character

            if (PeekCurrentChar() == '\\' && PeekCurrentChar(1) == 'x')
            {
                if (IsHexDigit(PeekCurrentChar(2)) &&
                    IsHexDigit(PeekCurrentChar(3)))
                {
                    character = (char)
                        (
                        ((int.Parse(PeekCurrentChar(2).ToString(), NumberStyles.HexNumber)) << 4) +
                         (int.Parse(PeekCurrentChar(3).ToString(), NumberStyles.HexNumber))
                        );

                    return (PeekCurrentString(4, out escapeEncodingText));
                }
            }

            escapeEncodingText = "";
            return false;
        }

        bool GetCurrentHexEncodedCharAndAdvance(out char character, out string escapeEncodingText)
        {
            if (PeekCurrentHexEncodedChar(out character, out escapeEncodingText) == true)
            {
                AdvancePosition(escapeEncodingText.Length);
                return true;
            }

            return false;
        }

        bool GetCurrentIdentifierNameAndAdvance(out string originalText, out string value, out bool originalTextIncludesUnicodeEscapeSequence)
        {
            int originalPosition = _currentPosition; // store the original position so we can measure the total token width

            // default 'out' values
            originalText = "";
            value = "";
            originalTextIncludesUnicodeEscapeSequence = false;

            StringBuilder originalTextBuilder = new StringBuilder();

            char character = PeekCurrentChar();
            string escapeEncodingText = "";
            bool isUnicodeEncodedChar = false;
            int charsToAdvance;
            if (character == '\\')
            {
                isUnicodeEncodedChar = PeekCurrentUnicodeEncodedChar(out character, out escapeEncodingText);
                if (!isUnicodeEncodedChar)
                {
                    // text at current position starts with a backslash...but the backslash is not unicode encoding.  not a valid identifier.
                    return false;
                }
                else
                {
                    charsToAdvance = escapeEncodingText.Length;
                }
            }
            else
            {
                charsToAdvance = 1;
            }
            if (!IsIdentifierStartChar(character))
            {
                // this is not an identifier
                return false;
            }
            else
            {
                if (isUnicodeEncodedChar)
                {
                    originalTextBuilder.Append(escapeEncodingText);
                    originalTextIncludesUnicodeEscapeSequence = true;
                }
                else
                {
                    originalTextBuilder.Append(character);
                }
            }

            StringBuilder identifierBuilder = new StringBuilder();
            AdvancePosition(charsToAdvance);
            identifierBuilder.Append(character);

            while (true)
            {
                character = PeekCurrentChar();
                isUnicodeEncodedChar = false;
                if (character == '\\')
                {
                    escapeEncodingText = "";
                    isUnicodeEncodedChar = PeekCurrentUnicodeEncodedChar(out character, out escapeEncodingText);
                    if (!isUnicodeEncodedChar)
                    {
                        // text at current position starts with a backslash...but the backslash is not unicode encoding.  not a valid identifier.
                        _currentPosition = originalPosition;
                        return false;
                    }
                    else
                    {
                        charsToAdvance = escapeEncodingText.Length;
                    }
                }
                else
                {
                    charsToAdvance = 1;
                }
                if (IsIdentifierContinueChar(character))
                {
                    AdvancePosition(charsToAdvance);
                    identifierBuilder.Append(character);

                    if (isUnicodeEncodedChar)
                    {
                        originalTextBuilder.Append(escapeEncodingText);
                        originalTextIncludesUnicodeEscapeSequence = true;
                    }
                    else
                    {
                        originalTextBuilder.Append(character);
                    }
                }
                else
                {
                    // end of identifier
                    originalText = originalTextBuilder.ToString();
                    value = identifierBuilder.ToString();
                    return true;
                }
            }
        }

        bool IsHexDigit(char c)
        {
            return ((c >= '0' && c <= '9') ||
                    (c >= 'A' && c <= 'F') ||
                    (c >= 'a' && c <= 'f'));
        }

        string CharToString(char character)
        {
            return new string(character, 1);
        }

        char GetCurrentCharAndAdvance()
        {
            char character = PeekCurrentChar();
            AdvancePosition();
            return character;
        }

        void AdvancePosition()
        {
            if (_currentPosition < _sourceCode.Length)
                _currentPosition++;
        }

        void AdvancePosition(int offset)
        {
            if (offset > 0)
            {
                if (_currentPosition + offset - 1 < _sourceCode.Length)
                    _currentPosition += offset;
                else
                    _currentPosition = _sourceCode.Length;
            }
            else
            {
                if (_currentPosition + offset >= 0)
                    _currentPosition += offset;
                else
                    _currentPosition = 0;
            }
        }

        char PeekCurrentChar()
        {
            return PeekCurrentChar(0);
        }

        char PeekCurrentChar(int offset)
        {
            char character = CHARACTER_EOF;
            if (_currentPosition + offset < _sourceCode.Length)
                character = _sourceCode[_currentPosition + offset];
            return character;
        }

        bool PeekCurrentString(int length, out string value)
        {
            if (_currentPosition + length <= _sourceCode.Length)
            {
                value = _sourceCode.Substring(_currentPosition, length);
                return true;
            }
            else
            {
                value = "";
                return false;
            }
        }

        bool IsIdentifierStartChar(char c)
        {
            switch (c)
            {
                case '$':
                case '_':
                    return true;
            }
            return IsUnicodeIdentifierStartChar(c);
        }

        bool IsIdentifierContinueChar(char c)
        {
            switch (c)
            {
                case '$':
                case '_':
                case '\u200C': /* zero width non-joiner */
                case '\u200D': /* zero width joiner */
                    return true;
            }
            return IsUnicodeIdentifierContinueChar(c);
        }

        bool IsUnicodeIdentifierStartChar(char c)
        {
            /* Resources to determine which characters qualify as ID_Start characters:
             * http://www.unicode.org/reports/tr31/ 
             * Also see Other_ID_Start notes and resources, below */

            bool returnValue = false;

            // all characters in the following sets can be Unicode ID_Start by default (unless excluded in the follow-up list)
            switch (GetUnicodeCategory(c))
            {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.OtherLetter:
                case UnicodeCategory.LetterNumber:
                    returnValue = true;
                    break;
                default:
                    returnValue = false;
                    break;
            }

            if (returnValue)
            {
                // if the char has qualified, make sure it's not on the excluded lists (pattern syntax and pattern white space chars)
                /* Resources which identify the Pattern_Syntax and Pattern_White_Space characters:
                 * http://www.unicode.org/reports/tr31/ 
                 * http://www.unicode.org/Public/UNIDATA/PropList.txt */
                if (IsUnicodePatternSyntaxChar(c))
                    returnValue = false;
                else if (IsUnicodePatternWhiteSpaceChar(c))
                    returnValue = false;
            }
            else //if (!returnValue)
            {
                // if the char has not yet qualified, check the Other_ID_Start (legacy allowed characters) list
                switch ((int)c)
                {
                    /* Other_ID_Start characters */
                    /* Resources which identify the Other_ID_Start character lsit:
                     * http://www.unicode.org/Public/UNIDATA/PropList.txt 
                     * http://www.unicode.org/reports/tr44/ */
                    case 0x2118:
                    case 0x212E:
                    case 0x309B:
                    case 0x309C:
                        returnValue = true;
                        break;
                }
            }

            return returnValue;
        }

        bool IsUnicodeIdentifierContinueChar(char c)
        {
            /* Resources to determine which characters qualify as ID_Continue characters:
             * http://www.unicode.org/reports/tr31/ 
             * Also see Other_ID_Continue notes and resources, below */

            bool returnValue = false;

            // all characters in the following sets can be Unicode ID_Continue by default (unless excluded in the follow-up list)
            switch (GetUnicodeCategory(c))
            {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.OtherLetter:
                case UnicodeCategory.LetterNumber:
                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.SpacingCombiningMark:
                case UnicodeCategory.DecimalDigitNumber:
                case UnicodeCategory.ConnectorPunctuation:
                    returnValue = true;
                    break;
                default:
                    returnValue = false;
                    break;
            }

            if (returnValue)
            {
                // if the char has qualified, make sure it's not on the excluded lists (pattern syntax and pattern white space chars)
                /* Resources which identify the Pattern_Syntax and Pattern_White_Space characters:
                 * http://www.unicode.org/reports/tr31/ 
                 * http://www.unicode.org/Public/UNIDATA/PropList.txt */
                if (IsUnicodePatternSyntaxChar(c))
                    returnValue = false;
                else if (IsUnicodePatternWhiteSpaceChar(c))
                    returnValue = false;
            }
            else //if (!returnValue)
            {
                // if the char has not yet qualified, check the Other_ID_Continue (legacy allowed characters) list
                switch ((int)c)
                {
                    /* Other_ID_Continue characters */
                    /* Resources which identify the Other_ID_Continue character lsit:
                     * http://www.unicode.org/Public/UNIDATA/PropList.txt 
                     * http://www.unicode.org/reports/tr44/ */
                    case 0x00B7:
                    case 0x0387:
                    case 0x1369:
                    case 0x136A:
                    case 0x136B:
                    case 0x136C:
                    case 0x136D:
                    case 0x136E:
                    case 0x136F:
                    case 0x1370:
                    case 0x1371:
                    case 0x19DA:
                        returnValue = true;
                        break;
                }
            }

            return returnValue;
        }

        bool IsUnicodePatternSyntaxChar(char c)
        {
            /* Resource: http://www.unicode.org/Public/UNIDATA/PropList.txt */

            /* NOTE: there are 2760 code points for pattern syntax characters, mostly in ranges.  So we use an if/elseif construct instead of a switch */

            int charAsInt = (int)c;
            if ((charAsInt >= 0x0021 && charAsInt <= 0x002F) ||
               (charAsInt >= 0x003A && charAsInt <= 0x0040) ||
               (charAsInt >= 0x005B && charAsInt <= 0x005E) ||
               (charAsInt == 0x0060) ||
               (charAsInt >= 0x007B && charAsInt <= 0x007E) ||
               (charAsInt >= 0x00A1 && charAsInt <= 0x00A7) ||
               (charAsInt == 0x00A9) ||
               (charAsInt >= 0x00AB && charAsInt <= 0x00AC) ||
               (charAsInt == 0x00AE) ||
               (charAsInt >= 0x00B0 && charAsInt <= 0x00B1) ||
               (charAsInt == 0x00B6) ||
               (charAsInt == 0x00BB) ||
               (charAsInt == 0x00BF) ||
               (charAsInt == 0x00D7) ||
               (charAsInt == 0x00F7) ||
               (charAsInt >= 0x2010 && charAsInt <= 0x2027) ||
               (charAsInt >= 0x2030 && charAsInt <= 0x203E) ||
               (charAsInt >= 0x2041 && charAsInt <= 0x2053) ||
               (charAsInt >= 0x2055 && charAsInt <= 0x205E) ||
               (charAsInt >= 0x2190 && charAsInt <= 0x245F) ||
               (charAsInt >= 0x2500 && charAsInt <= 0x2775) ||
               (charAsInt >= 0x2794 && charAsInt <= 0x2BFF) ||
               (charAsInt >= 0x2E00 && charAsInt <= 0x2E7F) ||
               (charAsInt >= 0x3001 && charAsInt <= 0x3003) ||
               (charAsInt >= 0x3008 && charAsInt <= 0x3020) ||
               (charAsInt == 0x3030) ||
               (charAsInt >= 0xFD3E && charAsInt <= 0XFD3F) ||
               (charAsInt >= 0xFE45 && charAsInt <= 0xFE46))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        bool IsUnicodePatternWhiteSpaceChar(char c)
        {
            /* Resource: http://www.unicode.org/Public/UNIDATA/PropList.txt */

            switch ((int)c)
            {
                case 0x0009:
                case 0x000A:
                case 0x000B:
                case 0x000C:
                case 0x000D:
                case 0x0020:
                case 0x0085:
                case 0x200E:
                case 0x200F:
                case 0x2028:
                case 0x2029:
                    return true;
                default:
                    return false;
            }
        }

        UnicodeCategory GetUnicodeCategory(char c)
        {
            ///* TODO: only enable the following code if the built-in function returns incorrect categories for ASCII characters {0x80 - 0xFF} */
            //if (c <= 0x00FF)
            //{
            //    return GetUnicodeCategoryAsciiOrLatin1(c);
            //}

            return CharUnicodeInfo.GetUnicodeCategory(c);
        }

        UnicodeCategory GetUnicodeCategoryAsciiOrLatin1(char c)
        {
            switch ((int)c)
            {
                case 0x00AA:
                case 0x00BA:
                    return UnicodeCategory.LowercaseLetter; // 1
                case 0x00AD:
                    return UnicodeCategory.DashPunctuation; // 19
                case 0x00A7:
                case 0x00B6:
                    return UnicodeCategory.OtherSymbol; // 28
                default:
                    return CharUnicodeInfo.GetUnicodeCategory(c);
            }

            /* NOTE: the above characters are the mapping that is being corrected; we can also correct the full mapping (below) if necessary */
            //switch ((int)c)
            //{
            //    case 0x0041:
            //    case 0x0042:
            //    case 0x0043:
            //    case 0x0044:
            //    case 0x0045:
            //    case 0x0046:
            //    case 0x0047:
            //    case 0x0048:
            //    case 0x0049:
            //    case 0x004A:
            //    case 0x004B:
            //    case 0x004C:
            //    case 0x004D:
            //    case 0x004E:
            //    case 0x004F:
            //    case 0x0050:
            //    case 0x0051:
            //    case 0x0052:
            //    case 0x0053:
            //    case 0x0054:
            //    case 0x0055:
            //    case 0x0056:
            //    case 0x0057:
            //    case 0x0058:
            //    case 0x0059:
            //    case 0x005A:
            //    case 0x00C0:
            //    case 0x00C1:
            //    case 0x00C2:
            //    case 0x00C3:
            //    case 0x00C4:
            //    case 0x00C5:
            //    case 0x00C6:
            //    case 0x00C7:
            //    case 0x00C8:
            //    case 0x00C9:
            //    case 0x00CA:
            //    case 0x00CB:
            //    case 0x00CC:
            //    case 0x00CD:
            //    case 0x00CE:
            //    case 0x00CF:
            //    case 0x00D0:
            //    case 0x00D1:
            //    case 0x00D2:
            //    case 0x00D3:
            //    case 0x00D4:
            //    case 0x00D5:
            //    case 0x00D6:
            //    case 0x00D8:
            //    case 0x00D9:
            //    case 0x00DA:
            //    case 0x00DB:
            //    case 0x00DC:
            //    case 0x00DD:
            //    case 0x00DE:
            //        return UnicodeCategory.UppercaseLetter; // 0
            //    case 0x0061:
            //    case 0x0062:
            //    case 0x0063:
            //    case 0x0064:
            //    case 0x0065:
            //    case 0x0066:
            //    case 0x0067:
            //    case 0x0068:
            //    case 0x0069:
            //    case 0x006A:
            //    case 0x006B:
            //    case 0x006C:
            //    case 0x006D:
            //    case 0x006E:
            //    case 0x006F:
            //    case 0x0070:
            //    case 0x0071:
            //    case 0x0072:
            //    case 0x0073:
            //    case 0x0074:
            //    case 0x0075:
            //    case 0x0076:
            //    case 0x0077:
            //    case 0x0078:
            //    case 0x0079:
            //    case 0x007A:
            //    case 0x00AA:
            //    case 0x00B5:
            //    case 0x00BA:
            //    case 0x00DF:
            //    case 0x00E0:
            //    case 0x00E1:
            //    case 0x00E2:
            //    case 0x00E3:
            //    case 0x00E4:
            //    case 0x00E5:
            //    case 0x00E6:
            //    case 0x00E7:
            //    case 0x00E8:
            //    case 0x00E9:
            //    case 0x00EA:
            //    case 0x00EB:
            //    case 0x00EC:
            //    case 0x00ED:
            //    case 0x00EE:
            //    case 0x00EF:
            //    case 0x00F0:
            //    case 0x00F1:
            //    case 0x00F2:
            //    case 0x00F3:
            //    case 0x00F4:
            //    case 0x00F5:
            //    case 0x00F6:
            //    case 0x00F8:
            //    case 0x00F9:
            //    case 0x00FA:
            //    case 0x00FB:
            //    case 0x00FC:
            //    case 0x00FD:
            //    case 0x00FE:
            //    case 0x00FF:
            //        return UnicodeCategory.LowercaseLetter; // 1
            //    case 0x0030:
            //    case 0x0031:
            //    case 0x0032:
            //    case 0x0033:
            //    case 0x0034:
            //    case 0x0035:
            //    case 0x0036:
            //    case 0x0037:
            //    case 0x0038:
            //    case 0x0039:
            //        return UnicodeCategory.DecimalDigitNumber; // 8
            //    case 0x00B2:
            //    case 0x00B3:
            //    case 0x00B9:
            //    case 0x00BC:
            //    case 0x00BD:
            //    case 0x00BE:
            //        return UnicodeCategory.OtherNumber; // 10
            //    case 0x0020:
            //    case 0x00A0:
            //        return UnicodeCategory.SpaceSeparator; // 11
            //    case 0x0000:
            //    case 0x0001:
            //    case 0x0002:
            //    case 0x0003:
            //    case 0x0004:
            //    case 0x0005:
            //    case 0x0006:
            //    case 0x0007:
            //    case 0x0008:
            //    case 0x0009:
            //    case 0x000A:
            //    case 0x000B:
            //    case 0x000C:
            //    case 0x000D:
            //    case 0x000E:
            //    case 0x000F:
            //    case 0x0010:
            //    case 0x0011:
            //    case 0x0012:
            //    case 0x0013:
            //    case 0x0014:
            //    case 0x0015:
            //    case 0x0016:
            //    case 0x0017:
            //    case 0x0018:
            //    case 0x0019:
            //    case 0x001A:
            //    case 0x001B:
            //    case 0x001C:
            //    case 0x001D:
            //    case 0x001E:
            //    case 0x001F:
            //    case 0x007F:
            //    case 0x0080:
            //    case 0x0081:
            //    case 0x0082:
            //    case 0x0083:
            //    case 0x0084:
            //    case 0x0085:
            //    case 0x0086:
            //    case 0x0087:
            //    case 0x0088:
            //    case 0x0089:
            //    case 0x008A:
            //    case 0x008B:
            //    case 0x008C:
            //    case 0x008D:
            //    case 0x008E:
            //    case 0x008F:
            //    case 0x0090:
            //    case 0x0091:
            //    case 0x0092:
            //    case 0x0093:
            //    case 0x0094:
            //    case 0x0095:
            //    case 0x0096:
            //    case 0x0097:
            //    case 0x0098:
            //    case 0x0099:
            //    case 0x009A:
            //    case 0x009B:
            //    case 0x009C:
            //    case 0x009D:
            //    case 0x009E:
            //    case 0x009F:
            //        return UnicodeCategory.Control; // 14
            //    case 0x005F:
            //        return UnicodeCategory.ConnectorPunctuation; // 18
            //    case 0x002D:
            //    case 0x00AD:
            //        return UnicodeCategory.DashPunctuation; // 19
            //    case 0x0028:
            //    case 0x005B:
            //    case 0x007B:
            //        return UnicodeCategory.OpenPunctuation; // 20
            //    case 0x0029:
            //    case 0x005D:
            //    case 0x007D:
            //        return UnicodeCategory.ClosePunctuation; // 21
            //    case 0x00AB:
            //        return UnicodeCategory.InitialQuotePunctuation; // 22
            //    case 0x00BB:
            //        return UnicodeCategory.FinalQuotePunctuation; //23
            //    case 0x0021:
            //    case 0x0022:
            //    case 0x0023:
            //    case 0x0025:
            //    case 0x0026:
            //    case 0x0027:
            //    case 0x002A:
            //    case 0x002C:
            //    case 0x002E:
            //    case 0x002F:
            //    case 0x003A:
            //    case 0x003B:
            //    case 0x003F:
            //    case 0x0040:
            //    case 0x005C:
            //    case 0x00A1:
            //    case 0x00B7:
            //    case 0x00BF:
            //        return UnicodeCategory.OtherPunctuation; // 24
            //    case 0x002B:
            //    case 0x003C:
            //    case 0x003D:
            //    case 0x003E:
            //    case 0x007C:
            //    case 0x007E:
            //    case 0x00AC:
            //    case 0x00B1:
            //    case 0x00D7:
            //    case 0x00F7:
            //        return UnicodeCategory.MathSymbol; // 25
            //    case 0x0024:
            //    case 0x00A2:
            //    case 0x00A3:
            //    case 0x00A4:
            //    case 0x00A5:
            //        return UnicodeCategory.CurrencySymbol; // 26
            //    case 0x005E:
            //    case 0x0060:
            //    case 0x00A8:
            //    case 0x00AF:
            //    case 0x00B4:
            //    case 0x00B8:
            //        return UnicodeCategory.ModifierSymbol; // 27
            //    case 0x00A6:
            //    case 0x00A7:
            //    case 0x00A9:
            //    case 0x00AE:
            //    case 0x00B0:
            //    case 0x00B6:
            //        return UnicodeCategory.OtherSymbol; // 28
            //    default:
            //        // if the caller accidentally sent us a non-ascii, non-latin1 character, gracefully degrade by returning the correct globalization value
            //        return CharUnicodeInfo.GetUnicodeCategory(c);
            //}
        }

    }
}
