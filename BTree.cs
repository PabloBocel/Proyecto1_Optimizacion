using Proyecto1_Optimizacion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proyecto1_Optimizacion
{
    public class BTree
    {
        private BTreeNode root;
        private int t; // Grado mínimo


        public BTree(int t)
        {
            this.t = t;
            root = null;
        }
        public List<Articulo> SearchByName(string name)
        {
            List<Articulo> results = new List<Articulo>();
            SearchByName(root, name, results);
            return results;
        }

        private void SearchByName(BTreeNode node, string name, List<Articulo> results)
        {
            if (node == null)
            {
                return;
            }

            foreach (var key in node.Keys)
            {
                if (key.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(key);
                }
            }

            if (!node.IsLeaf)
            {
                foreach (var child in node.Children)
                {
                    SearchByName(child, name, results);
                }
            }
        }


        public void Insert(Articulo articulo)
        {
            if (root == null)
            {
                root = new BTreeNode(t, true);
                root.Keys.Add(articulo);
            }
            else
            {
                if (root.Keys.Count == 2 * t - 1)
                {
                    BTreeNode newNode = new BTreeNode(t, false);
                    newNode.Children.Add(root);
                    SplitChild(newNode, 0, root);
                    InsertNonFull(newNode, articulo);
                    root = newNode;
                }
                else
                {
                    InsertNonFull(root, articulo);
                }
            }
        }

        private void InsertNonFull(BTreeNode node, Articulo articulo)
        {
            int i = node.Keys.Count - 1;

            if (node.IsLeaf)
            {
                node.Keys.Add(null);
                while (i >= 0 && string.Compare(node.Keys[i].ISBN, articulo.ISBN) > 0)
                {
                    node.Keys[i + 1] = node.Keys[i];
                    i--;
                }
                node.Keys[i + 1] = articulo;
            }
            else
            {
                while (i >= 0 && string.Compare(node.Keys[i].ISBN, articulo.ISBN) > 0)
                {
                    i--;
                }
                i++;
                if (node.Children[i].Keys.Count == 2 * t - 1)
                {
                    SplitChild(node, i, node.Children[i]);
                    if (string.Compare(node.Keys[i].ISBN, articulo.ISBN) < 0)
                    {
                        i++;
                    }
                }
                InsertNonFull(node.Children[i], articulo);
            }
        }

        private void SplitChild(BTreeNode parent, int index, BTreeNode fullChild)
        {
            BTreeNode newNode = new BTreeNode(t, fullChild.IsLeaf);
            parent.Children.Insert(index + 1, newNode);
            parent.Keys.Insert(index, fullChild.Keys[t - 1]);

            // Mover las claves y los hijos del nodo lleno al nuevo nodo
            for (int j = 0; j < t - 1; j++)
            {
                newNode.Keys.Add(fullChild.Keys[t + j]);
            }

            if (!fullChild.IsLeaf)
            {
                for (int j = 0; j < t; j++)
                {
                    newNode.Children.Add(fullChild.Children[t + j]);
                }
            }

            // Eliminar las claves y los hijos del nodo lleno que se movieron al nuevo nodo
            fullChild.Keys.RemoveRange(t - 1, fullChild.Keys.Count - (t - 1));
            if (!fullChild.IsLeaf)
            {
                fullChild.Children.RemoveRange(t, fullChild.Children.Count - t);
            }
        }

        public void Delete(string isbn)
        {
            if (root != null)
            {
                Delete(root, isbn);
                if (root.Keys.Count == 0)
                {
                    if (root.IsLeaf)
                    {
                        root = null;
                    }
                    else
                    {
                        root = root.Children[0];
                    }
                }
            }
        }

        private void Delete(BTreeNode node, string isbn)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node), "El nodo no puede ser null.");
            }

            int index = node.Keys.FindIndex(k => k.ISBN == isbn);

            if (index != -1)
            {
                if (node.IsLeaf)
                {
                    node.Keys.RemoveAt(index);
                }
                else
                {
                    BTreeNode pred = (index >= 0 && index < node.Children.Count) ? node.Children[index] : null;
                    BTreeNode succ = (index + 1 < node.Children.Count) ? node.Children[index + 1] : null;

                    if (pred != null && pred.Keys.Count >= t)
                    {
                        Articulo predecessor = GetPredecessor(pred);
                        node.Keys[index] = predecessor;
                        Delete(pred, predecessor.ISBN);
                    }
                    else if (succ != null && succ.Keys.Count >= t)
                    {
                        Articulo successor = GetSuccessor(succ);
                        node.Keys[index] = successor;
                        Delete(succ, successor.ISBN);
                    }
                    else
                    {
                        if (succ != null)
                        {
                            Merge(node, index);
                            Delete(node.Children[index], isbn);
                        }
                        else
                        {
                            throw new InvalidOperationException("No successor node available for merge operation.");
                        }
                    }
                }
            }
            else
            {
                if (node.IsLeaf)
                {
                    return;
                }

                bool flag = (index == node.Keys.Count);

                // Ensure index is within bounds
                if (index < 0 || index >= node.Children.Count)
                {
                    return;
                }

                if (node.Children[index].Keys.Count < t)
                {
                    Fill(node, index);
                }

                if (flag && index > node.Keys.Count)
                {
                    Delete(node.Children[index - 1], isbn);
                }
                else
                {
                    Delete(node.Children[index], isbn);
                }
            }
        }


        private Articulo GetPredecessor(BTreeNode node)
        {
            while (!node.IsLeaf)
            {
                node = node.Children[node.Keys.Count];
            }
            return node.Keys[node.Keys.Count - 1];
        }

        private Articulo GetSuccessor(BTreeNode node)
        {
            while (!node.IsLeaf)
            {
                node = node.Children[0];
            }
            return node.Keys[0];
        }

        private void Fill(BTreeNode node, int index)
        {
            if (index < 0 || index >= node.Children.Count)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range for node.Children");
            }

            if (index > 0 && node.Children[index - 1].Keys.Count >= t)
            {
                BorrowFromPrev(node, index);
            }
            else if (index < node.Keys.Count && node.Children[index + 1].Keys.Count >= t)
            {
                BorrowFromNext(node, index);
            }
            else
            {
                if (index < node.Keys.Count)
                {
                    Merge(node, index);
                }
                else
                {
                    if (index - 1 >= 0)
                    {
                        Merge(node, index - 1);
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException("index", "Index is out of range for merge operation.");
                    }
                }
            }
        }

        private void BorrowFromPrev(BTreeNode node, int index)
        {
            BTreeNode child = node.Children[index];
            BTreeNode sibling = node.Children[index - 1];

            child.Keys.Insert(0, node.Keys[index - 1]);

            if (!child.IsLeaf)
            {
                child.Children.Insert(0, sibling.Children[sibling.Children.Count - 1]);
            }

            node.Keys[index - 1] = sibling.Keys[sibling.Keys.Count - 1];
            sibling.Keys.RemoveAt(sibling.Keys.Count - 1);

            if (!child.IsLeaf)
            {
                sibling.Children.RemoveAt(sibling.Children.Count - 1);
            }
        }

        private void BorrowFromNext(BTreeNode node, int index)
        {
            BTreeNode child = node.Children[index];
            BTreeNode sibling = node.Children[index + 1];

            child.Keys.Add(node.Keys[index]);

            if (!child.IsLeaf)
            {
                child.Children.Add(sibling.Children[0]);
                sibling.Children.RemoveAt(0);
            }

            node.Keys[index] = sibling.Keys[0];
            sibling.Keys.RemoveAt(0);
        }

        private void Merge(BTreeNode node, int index)
        {
            if (index < 0 || index >= node.Children.Count - 1)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range for merge operation.");
            }

            BTreeNode child = node.Children[index];
            BTreeNode sibling = node.Children[index + 1];

            child.Keys.Add(node.Keys[index]);

            for (int i = 0; i < sibling.Keys.Count; i++)
            {
                child.Keys.Add(sibling.Keys[i]);
            }

            if (!child.IsLeaf)
            {
                for (int i = 0; i <= sibling.Keys.Count; i++)
                {
                    child.Children.Add(sibling.Children[i]);
                }
            }

            node.Keys.RemoveAt(index);
            node.Children.RemoveAt(index + 1);
        }


        public Articulo Search(string isbn)
        {
            return Search(root, isbn);
        }

        private Articulo Search(BTreeNode node, string isbn)
        {
            if (node == null)
            {
                Console.WriteLine("Nodo es null.");
                return null;
            }

            int i = 0;
            while (i < node.Keys.Count && string.Compare(isbn, node.Keys[i].ISBN) > 0)
            {
                i++;
            }

            if (i < node.Keys.Count && isbn == node.Keys[i].ISBN)
            {
                return node.Keys[i];
            }

            if (node.IsLeaf)
            {
                return null;
            }

            if (i < node.Children.Count && node.Children[i] != null)
            {
                return Search(node.Children[i], isbn);
            }
            else
            {
                Console.WriteLine("El índice o el nodo hijo es null.");
                return null;
            }
        }



        public void Update(string isbn, Articulo updatedArticulo)
        {
            var article = Search(isbn);
            if (article != null)
            {
                if (!string.IsNullOrEmpty(updatedArticulo.Author))
                {
                    article.Author = updatedArticulo.Author;
                }
                if (!string.IsNullOrEmpty(updatedArticulo.Category))
                {
                    article.Author = updatedArticulo.Author;
                }
                if (updatedArticulo.Price.HasValue)
                {
                    article.Price = updatedArticulo.Price.Value;
                }
                if (updatedArticulo.quantity.HasValue)
                {
                    article.quantity = updatedArticulo.quantity.Value;
                }
            }
        }


    }

}
