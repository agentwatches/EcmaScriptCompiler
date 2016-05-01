using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcmaScriptCompiler.Syntax
{
    public class KeywordToken : SyntaxToken
    {
        string _value;

        public KeywordToken(SyntaxNode parent, int relativePosition, string text, string value)
            : base(parent, SyntaxKind.KeywordToken, relativePosition, text)
        {
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
