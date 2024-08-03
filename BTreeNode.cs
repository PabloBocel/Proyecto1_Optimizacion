using Proyecto1_Optimizacion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proyecto1_Optimizacion
{
    public class BTreeNode
    {
        public int T { get; set; } // Grado mínimo
        public List<Articulo> Keys { get; set; }
        public List<BTreeNode> Children { get; set; }
        public bool IsLeaf { get; set; }

        public BTreeNode(bool isLeaf)
        {
            Keys = new List<Articulo>();
            Children = new List<BTreeNode>();
            IsLeaf = isLeaf;
        }
        public BTreeNode(int t, bool isLeaf)
        {
            T = t;
            IsLeaf = isLeaf;
            Keys = new List<Articulo>();
            Children = new List<BTreeNode>();
        }

        // Insertar un nuevo artículo en el nodo no completo
        public void InsertNonFull(Articulo articulo)
        {
            int i = Keys.Count - 1;

            if (IsLeaf)
            {
                Keys.Add(null);
                while (i >= 0 && string.Compare(Keys[i].ISBN, articulo.ISBN) > 0)
                {
                    Keys[i + 1] = Keys[i];
                    i--;
                }
                Keys[i + 1] = articulo;
            }
            else
            {
                while (i >= 0 && string.Compare(Keys[i].ISBN, articulo.ISBN) > 0)
                {
                    i--;
                }
                i++;
                if (Children[i].Keys.Count == 2 * T - 1)
                {
                    SplitChild(i, Children[i]);
                    if (string.Compare(Keys[i].ISBN, articulo.ISBN) < 0)
                    {
                        i++;
                    }
                }
                Children[i].InsertNonFull(articulo);
            }
        }

        // Dividir el hijo y
        public void SplitChild(int i, BTreeNode y)
        {
            BTreeNode z = new BTreeNode(y.T, y.IsLeaf);
            for (int j = 0; j < T - 1; j++)
            {
                z.Keys.Add(y.Keys[j + T]);
            }
            if (!y.IsLeaf)
            {
                for (int j = 0; j < T; j++)
                {
                    z.Children.Add(y.Children[j + T]);
                }
            }
            y.Keys.RemoveRange(T - 1, T);
            y.Children.RemoveRange(T, T);

            Children.Insert(i + 1, z);
            Keys.Insert(i, y.Keys[T - 1]);
        }

        // Buscar un artículo en el nodo
        public Articulo Search(string isbn)
        {
            int i = 0;
            while (i < Keys.Count && string.Compare(isbn, Keys[i].ISBN) > 0)
            {
                i++;
            }
            if (i < Keys.Count && isbn == Keys[i].ISBN)
            {
                return Keys[i];
            }
            if (IsLeaf)
            {
                return null;
            }
            return Children[i].Search(isbn);
        }

        // Eliminar un artículo del nodo
        public void Delete(string isbn)
        {
            int idx = FindKey(isbn);
            if (idx < Keys.Count && Keys[idx].ISBN == isbn)
            {
                if (IsLeaf)
                {
                    RemoveFromLeaf(idx);
                }
                else
                {
                    RemoveFromNonLeaf(idx);
                }
            }
            else
            {
                if (IsLeaf)
                {
                    return;
                }

                bool flag = (idx == Keys.Count);

                if (Children[idx].Keys.Count < T)
                {
                    Fill(idx);
                }

                if (flag && idx > Keys.Count)
                {
                    Children[idx - 1].Delete(isbn);
                }
                else
                {
                    Children[idx].Delete(isbn);
                }
            }
        }

        // Encontrar la clave en el nodo
        private int FindKey(string isbn)
        {
            int idx = 0;
            while (idx < Keys.Count && string.Compare(Keys[idx].ISBN, isbn) < 0)
            {
                idx++;
            }
            return idx;
        }

        // Eliminar un artículo del nodo hoja
        private void RemoveFromLeaf(int idx)
        {
            Keys.RemoveAt(idx);
        }

        // Eliminar un artículo del nodo no hoja
        private void RemoveFromNonLeaf(int idx)
        {
            string isbn = Keys[idx].ISBN;

            if (Children[idx].Keys.Count >= T)
            {
                Articulo pred = GetPredecessor(idx);
                Keys[idx] = pred;
                Children[idx].Delete(pred.ISBN);
            }
            else if (Children[idx + 1].Keys.Count >= T)
            {
                Articulo succ = GetSuccessor(idx);
                Keys[idx] = succ;
                Children[idx + 1].Delete(succ.ISBN);
            }
            else
            {
                Merge(idx);
                Children[idx].Delete(isbn);
            }
        }

        // Obtener el predecesor del artículo
        private Articulo GetPredecessor(int idx)
        {
            BTreeNode cur = Children[idx];
            while (!cur.IsLeaf)
            {
                cur = cur.Children[cur.Keys.Count];
            }
            return cur.Keys[cur.Keys.Count - 1];
        }

        // Obtener el sucesor del artículo
        private Articulo GetSuccessor(int idx)
        {
            BTreeNode cur = Children[idx + 1];
            while (!cur.IsLeaf)
            {
                cur = cur.Children[0];
            }
            return cur.Keys[0];
        }

        // Rellenar el nodo
        private void Fill(int idx)
        {
            if (idx != 0 && Children[idx - 1].Keys.Count >= T)
            {
                BorrowFromPrev(idx);
            }
            else if (idx != Keys.Count && Children[idx + 1].Keys.Count >= T)
            {
                BorrowFromNext(idx);
            }
            else
            {
                if (idx != Keys.Count)
                {
                    Merge(idx);
                }
                else
                {
                    Merge(idx - 1);
                }
            }
        }

        // Pedir prestado del nodo anterior
        private void BorrowFromPrev(int idx)
        {
            BTreeNode child = Children[idx];
            BTreeNode sibling = Children[idx - 1];

            for (int i = child.Keys.Count - 1; i >= 0; i--)
            {
                child.Keys[i + 1] = child.Keys[i];
            }

            if (!child.IsLeaf)
            {
                for (int i = child.Children.Count - 1; i >= 0; i--)
                {
                    child.Children[i + 1] = child.Children[i];
                }
            }

            child.Keys[0] = Keys[idx - 1];

            if (!child.IsLeaf)
            {
                child.Children[0] = sibling.Children[sibling.Keys.Count];
            }

            Keys[idx - 1] = sibling.Keys[sibling.Keys.Count - 1];
            sibling.Keys.RemoveAt(sibling.Keys.Count - 1);
            sibling.Children.RemoveAt(sibling.Children.Count - 1);

            child.Keys.Insert(0, sibling.Keys[sibling.Keys.Count - 1]);
            Keys[idx - 1] = sibling.Keys[sibling.Keys.Count - 1];
            sibling.Keys.RemoveAt(sibling.Keys.Count - 1);
        }

        // Pedir prestado del nodo siguiente
        private void BorrowFromNext(int idx)
        {
            BTreeNode child = Children[idx];
            BTreeNode sibling = Children[idx + 1];

            child.Keys.Add(Keys[idx]);

            if (!child.IsLeaf)
            {
                child.Children.Add(sibling.Children[0]);
                sibling.Children.RemoveAt(0);
            }

            Keys[idx] = sibling.Keys[0];
            sibling.Keys.RemoveAt(0);
        }

        // Fusionar los nodos
        private void Merge(int idx)
        {
            BTreeNode child = Children[idx];
            BTreeNode sibling = Children[idx + 1];

            child.Keys.Add(Keys[idx]);

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

            Keys.RemoveAt(idx);
            Children.RemoveAt(idx + 1);
        }
    }
}
