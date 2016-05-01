using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcmaScriptCompiler.Syntax
{
    public class SyntaxTrivia : SyntaxTreeItem
    {
        string _text = ""; // the original source text

        public SyntaxTrivia(SyntaxTreeItem parent, SyntaxKind kind, int relativePosition, string text)
            : base(parent, kind, relativePosition)
        {
            _text = text;
        }

        public override string Text
        {
            get
            {
                return _text;
            }
        }

        public override int Width
        {
            get
            {
                return _text.Length;
            }
        }

    }
}
