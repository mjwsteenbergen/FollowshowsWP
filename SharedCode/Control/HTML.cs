using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedCode
{
    class HTML
    {

        /// <summary>
        /// Returns the first child of the node
        /// </summary>
        /// <param name="node">An HTML node</param>
        /// <returns></returns>
        public static HtmlNode getChild(HtmlNode node)
        {
            return getChild(node, 0);
        }

        /// <summary>
        /// Returns the i'th child
        /// </summary>
        /// <param name="node">HTML Node</param>
        /// <param name="i">th child</param>
        /// <returns></returns>
        public static HtmlNode getChild(HtmlNode node, int i)
        {
            if (node != null)
            {
                HtmlNode[] res = node.ChildNodes.ToArray<HtmlNode>();
                if (res.Length > i - 1)
                    return node.ChildNodes.ToArray<HtmlNode>()[i];
            }
            return null;
        }

        /// <summary>
        /// Returns a node in the immediate decendants of the node which holds the attribute-value pair
        /// </summary>
        /// <param name="node">an HTML Node</param>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static HtmlNode getChild(HtmlNode node, string attribute, string value)
        {
            if (node == null || attribute == null || value == null)
                return null;
            foreach (HtmlNode child in node.ChildNodes)
            {
                if (child.Attributes[attribute] != null)
                {
                    if (child.Attributes[attribute].Value == value)
                        return child;
                }
            }
            throw new Exception("The class,value combination was not found");
            //return null;
        }

        /// <summary>
        /// Returns the node if anywhere in the decendants holds a attribute-value pair
        /// </summary>
        /// <param name="col"></param>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static HtmlNode getChild(IEnumerable<HtmlNode> col, string attribute, string value)
        {
            if (col == null || attribute == null || value == null) return null;
            foreach (HtmlNode child in col)
            {
                if (child.Attributes[attribute] != null)
                {
                    if (child.Attributes[attribute].Value == value)
                        return child;
                }
                HtmlNode node = getChild(child.ChildNodes, attribute, value);
                if (node != null)
                    return node;
            }
            return null;
        }

        /// <summary>
        /// Returns a node with the attribute, if it is somewhere in the decendants tree
        /// </summary>
        /// <param name="col"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static string getAttribute(IEnumerable<HtmlNode> col, string attribute)
        {
            if (col == null || attribute == null) return null;
            foreach (HtmlNode child in col)
            {
                if (child.Attributes[attribute] != null)
                {
                    return child.Attributes[attribute].Value;
                }
                string node = getAttribute(child.ChildNodes, attribute);
                if (node != null)
                    return node;
            }
            return null;
        }

        public static string getAttribute(HtmlNode node, string attribute)
        {
            if (node.Attributes[attribute] != null)
            {
                return node.Attributes[attribute].Value;
            }
            return null;
        }
    }
}
