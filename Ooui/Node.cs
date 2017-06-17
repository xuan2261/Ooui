using System;
using System.Collections.Generic;
using System.Linq;

namespace Ooui
{
    public abstract class Node : EventTarget
    {
        readonly List<Node> children = new List<Node> ();

        public IReadOnlyList<Node> Children {
            get {
                lock (children) {
                    return new List<Node> (children).AsReadOnly ();
                }
            }
        }

        public virtual string Text {
            get { return String.Join ("", from c in Children select c.Text); }
            set {
                ReplaceAll (new TextNode (value ?? ""));
            }
        }

        protected Node (string tagName)
            : base (tagName)
        {            
        }

        public override EventTarget GetElementById (string id)
        {
            if (id == Id) return this;
            foreach (var c in Children) {
                if (c is Element e) {
                    var r = e.GetElementById (id);
                    if (r != null)
                        return r;
                }
            }
            return null;
        }

        public Node AppendChild (Node newChild)
        {
            return InsertBefore (newChild, null);
        }

        public Node InsertBefore (Node newChild, Node referenceChild)
        {
            if (newChild == null)
                return null;
            lock (children) {
                if (referenceChild == null) {
                    children.Add (newChild);
                }
                else {
                    var index = children.IndexOf (referenceChild);
                    if (index < 0) {
                        throw new ArgumentException ("Reference must be a child of this element", nameof (referenceChild));
                    }
                    children.Insert (index, newChild);
                }
            }
            newChild.MessageSent += HandleChildMessageSent;
            SendCall ("insertBefore", newChild, referenceChild);
            return newChild;
        }

        public Node RemoveChild (Node child)
        {
            if (child == null)
                return null;
            lock (children) {
                if (!children.Remove (child)) {
                    throw new ArgumentException ("Child not contained in this element", nameof(child));
                }
            }
            child.MessageSent -= HandleChildMessageSent;
            SendCall ("removeChild", child);
            return child;
        }

        protected void ReplaceAll (Node newNode)
        {
            var toRemove = new List<Node> ();
            lock (children) {
                toRemove.AddRange (children);
                children.Clear ();
            }
            foreach (var child in toRemove) {
                child.MessageSent -= HandleChildMessageSent;
                SendCall ("removeChild", child);
            }
            InsertBefore (newNode, null);
        }

        void HandleChildMessageSent (Message message)
        {
            Send (message);
        }

        protected override void SaveStateMessageIfNeeded (Message message)
        {
            switch (message.MessageType) {
                case MessageType.Call when 
                    message.Key == "insertBefore" ||
                    message.Key == "removeChild":
                    SaveStateMessage (message);
                    break;
                default:
                    base.SaveStateMessageIfNeeded (message);
                    break;
            }
        }
    }
}
