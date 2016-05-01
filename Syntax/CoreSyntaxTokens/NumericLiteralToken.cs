using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcmaScriptCompiler.Syntax
{
    public class NumericLiteralToken : SyntaxToken
    {
        double _value;

        public NumericLiteralToken(SyntaxNode parent, int relativePosition, string text, double value)
            : base(parent, SyntaxKind.NumericLiteralToken, relativePosition, text)
        {
            _value = value;
        }

        public double Value
        {
            get
            {
                return _value;
            }
        }
    }
}
