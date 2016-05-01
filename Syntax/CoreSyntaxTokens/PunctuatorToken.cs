using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcmaScriptCompiler.Syntax
{
    public class PunctuatorToken : SyntaxToken
    {
        SyntaxKind _value;

        public PunctuatorToken(SyntaxNode parent, int relativePosition, SyntaxKind value)
            : base(parent, value, relativePosition, SyntaxKindPunctuationAsString.ConvertToString(value))
        {
            _value = value;
        }

        public SyntaxKind Value
        {
            get
            {
                return _value;
            }
        }
    }
}
