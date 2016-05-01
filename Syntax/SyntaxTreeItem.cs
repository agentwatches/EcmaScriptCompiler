using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/* NOTE: this is the base class for all syntax tree node types */
namespace EcmaScriptCompiler.Syntax
{
    public abstract class SyntaxTreeItem
    {
        protected int _relativePositionToParent = 0;
        protected SyntaxTreeItem _parent = null;
        protected SyntaxKind _kind = SyntaxKind.None;

        protected SyntaxTreeItem(SyntaxTreeItem parent, SyntaxKind kind, int relativePositionToParent)
        {
            _parent = parent;
            _kind = kind;
            _relativePositionToParent = relativePositionToParent;
        }

        public SyntaxTreeItem Parent
        {
            get
            {
                return _parent;
            }
        }

        public SyntaxKind Kind
        {
            get
            {
                return _kind;
            }
        }

        public int GetPosition()
        {
            int position = _relativePositionToParent;
            if (_parent != null)
                position += _parent.GetPosition();
            return position;
        }

        public abstract int Width { get; }

        public abstract string Text { get; }

        public override string ToString()
        {
            return _kind.ToString();
        }
    }
}
