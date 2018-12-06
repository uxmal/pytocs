
using System;

using System.Linq;

public static class readme {
    
    public class MyClass {
        
        // member function calling other function
        public virtual object calc_sum(object x, object y) {
            return this.frobulate("+", x, y);
        }
        
        // arithmetic and exceptions
        public virtual object frobulate(object op, object x, object y) {
            if (op == "+") {
                return x + y;
            } else if (op == "-") {
                return x - y;
            } else {
                throw new ValueError(String.Format("Unexpected argument %s", op));
            }
        }
        
        // static method using for..in and enumerate, with tuple comprehension
        public static object walk_list(object lst) {
            foreach (var _tup_1 in lst.iterate()) {
                var i = _tup_1.Item1;
                var strg = _tup_1.Item2;
                Console.WriteLine(String.Format("index: %d strg: %s\n", i, strg));
            }
        }
        
        // list comprehension
        public static object apply_map(object mapfn, object filterfn) {
            return (from n in lst
                where filterfn
                select mapfn(n)).ToList();
        }
    }
}
