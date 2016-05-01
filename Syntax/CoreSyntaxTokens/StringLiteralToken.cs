using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcmaScriptCompiler.Syntax
{
    public class StringLiteralToken : SyntaxToken
    {
        char _quoteCharacter;
        string _value;

        public StringLiteralToken(SyntaxNode parent, int relativePosition, string text, char quoteCharacter, string value)
            : base(parent, SyntaxKind.StringLiteralToken, relativePosition, text)
        {
            _quoteCharacter = quoteCharacter;
            _value = value;
        }

        public string Value
        {
            get
            {
                return _value;
            }
        }
    }
}
