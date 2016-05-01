using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcmaScriptCompiler.Syntax
{
    public class SyntaxNode : SyntaxTreeItem //, IEnumerable<SyntaxTreeItem>
    {
        protected List<SyntaxTreeItem> _children = new List<SyntaxTreeItem>();

        public SyntaxNode(SyntaxTreeItem parent, SyntaxKind kind, int relativePosition)
            : base(parent, kind, relativePosition)
        {
        }

        public List<SyntaxTreeItem> Children
        {
            get
            {
                return _children;
            }
        }

        public override string Text
        {
            get
            {
                StringBuilder textBuilder = new StringBuilder();
                foreach (SyntaxTreeItem treeItem in _children)
                {
                    textBuilder.Append(treeItem.Text);
                }
                return textBuilder.ToString();
            }
        }

        public override int Width
        {
            get
            {
                int width = 0;
                foreach (SyntaxTreeItem treeItem in _children)
                {
                    width += treeItem.Width;
                }
                return width;
            }
        }

        //public IEnumerator<SyntaxTreeItem> GetEnumerator()
        //{
        //    return _children.GetEnumerator();
        //}

        //IEnumerator IEnumerable.GetEnumerator()
        //{
        //    return _children.GetEnumerator();
        //}
    }
}
