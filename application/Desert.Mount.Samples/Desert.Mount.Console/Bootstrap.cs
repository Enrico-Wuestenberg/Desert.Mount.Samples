using System.Collections.ObjectModel;

namespace Desert.Mount.ConsoleApp
{
    public class Bootstrap
    {
        public void Run_List_T()
        {
            // Stärken: effektives Hinzufügen und paralleler Lesezugriff
            // Schwächen: Langsamer AddOrUpdate-Prozess O(n) in der Mitte,
            // paralleles Schreiben kann zu Problemen führen (Deadlock)  

            Console.WriteLine("List<T>");
            var list = new List<string>()
            { 
                "Tisch", "Stuhl", "Kommode", "Uhr", "Tapete"
            };

            list.ForEach(x => Console.WriteLine(x));
        }

        public void Run_LinkedList_T()
        {
            // Stärken: Wenn du häufig Elemente mitten in der Sequenz per
            // Knotenreferenz einfügst oder entfernst (O(1)) 
            // Schwächen: Random Access Zugriff -> Lieber List<T> verwenden

            Console.WriteLine("LinkedList<T>");
            // ### Grundlegendes Erstellen und Einfügen
            var ll = new LinkedList<int>();

            // Am Anfang/Ende einfügen (O(1))
            ll.AddFirst(10);
            ll.AddLast(20);

            // An einem bekannten Knoten davor/danach einfügen (O(1)) 

            LinkedListNode<int> n = ll.AddLast(30);
            ll.AddBefore(n, 25);
            ll.AddAfter(n, 35);

            // Ausgabe
            foreach (var x in ll)
                Console.WriteLine(x);

            // ### Suchen und an einer Position in der Mitte einfügen
            var ll2 = new LinkedList<string>();
            ll2.AddLast("A");
            ll2.AddLast("B");
            ll2.AddLast("C");

            // Knoten finden (O(n)), dann O(1) nach dem Knoten einfügen 

            var bNode = ll2.Find("B");
            if (bNode != null)
                ll2.AddAfter(bNode, "B+");

            // Vorwärts iterieren
            foreach (var s in ll2)
                Console.WriteLine(s);

            // Rückwärts iterieren über Knoten
            for (var node = ll2.Last; node != null; node = node.Previous)
                Console.WriteLine(node.Value);
        }

        public void Run_ObservableCollection_T()
        {
            // Stärken: Wird bei UI Anwendungen genutzt,
            // Benachrichtigung bei Änderungen über events;
            // kommt bei WPF oder MAUI zum Einsatz

            Console.WriteLine("ObservableCollection<T>");
            var collection = new ObservableCollection<int>()
            {
                1,2,3,4,5,6
            };

            collection.ToList().ForEach(x => Console.WriteLine(x));
        }

        public void Run_ImmutableList_T()
        {
            // Stärken: ImmutableList<T> glänzt, wenn du konsistente,
            // nebenläufige, versionierbare Zustände brauchst,
            // die von vielen Stellen gleichzeitig gelesen werden
            // – und Änderungen als neue, klar abgegrenzte Versionen modellierst.

            Console.WriteLine("ImmutableList<T>");
            var immu = new ImmutableListOrderbook();
            immu.Run();

            // Praxiserfahrungen: bevorzuge AddRange statt viele einzelne Add‑Aufrufe 
        }
    }
}
