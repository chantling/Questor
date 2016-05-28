using System;
using System.Collections;
using System.Windows.Forms;

namespace QuestorManager
{
    public class ListViewColumnSort : IComparer
    {
        public enum TipoCompare
        {
            Cadena,
            Numero,
            Fecha
        }

        public int ColumnIndex;

        public TipoCompare CompararPor;
        public SortOrder Sorting = SortOrder.Ascending;

        public ListViewColumnSort()
        {
        }

        public ListViewColumnSort(int columna)
        {
            ColumnIndex = columna;
        }

        public int Compare(Object a, Object b)
        {
            int menor = -1, mayor = 1;

            //
            if (Sorting == SortOrder.None)
                return 0;

            var s1 = ((ListViewItem) a).SubItems[ColumnIndex].Text;
            var s2 = ((ListViewItem) b).SubItems[ColumnIndex].Text;

            if (Sorting == SortOrder.Descending)
            {
                menor = 1;
                mayor = -1;
            }

            //
            switch (CompararPor)
            {
                case TipoCompare.Fecha:
                    try
                    {
                        var f1 = DateTime.Parse(s1);
                        var f2 = DateTime.Parse(s2);

                        //
                        if (f1 < f2)
                            return menor;

                        if (f1 == f2)
                            return 0;

                        return mayor;
                    }
                    catch
                    {
                        return String.Compare(s1, s2, StringComparison.OrdinalIgnoreCase)*mayor;
                    }

                case TipoCompare.Numero:
                    try
                    {
                        var n1 = decimal.Parse(s1);
                        var n2 = decimal.Parse(s2);
                        if (n1 < n2)
                            return menor;

                        if (n1 == n2)
                            return 0;

                        return mayor;
                    }
                    catch
                    {
                        return String.Compare(s1, s2, StringComparison.OrdinalIgnoreCase)*mayor;
                    }

                default:

                    return String.Compare(s1, s2, StringComparison.OrdinalIgnoreCase)*mayor;
            }
        }
    }
}