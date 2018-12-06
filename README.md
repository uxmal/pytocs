# pytocs

[![Join the chat at https://gitter.im/uxmal/pytocs](https://badges.gitter.im/uxmal/pytocs/Lobby.svg)](https://gitter.im/uxmal/pytocs?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

Converts Python source to C#

pytocs is a command line tool I wrote as a hobby project to convert Python source code to C#. 
I've uploaded it here in case someone finds it useful.

## How to run pytocs

Just git clone the project, and use Visual Studio or MSBuild to compile the `pytocs.sln` file. If you're unable or unwilling to build pytocs from source, the latest continuous integration build
is available at [appveyor.com](https://ci.appveyor.com/project/uxmal/pytocs/build/artifacts). 

## Examples

To convert a Python file, hand it to `pytocs`:

    pytocs foo.py
	
To convert all files in a directory (recursively), use the `-r` parameter:

    pytocs -r 
	
The following python fragment:

```Python
# Some code below
def hello():
   print "Hello World";
```

Translates to:

```C#
public static class hello {

    public static object hello() {
	    Console.WriteLine("Hello World");
    }
}
```

A more ambitious sample:
```Python
class MyClass:
    # member function calling other function
    def calc_sum(self, x, y):
       return self.frobulate('+', x, y)

    # arithmetic and exceptions
    def frobulate(self, op, x, y):
        if op == '+':
            return x + y
        elif op == '-':
            return x - y
        else:
            raise ValueError("Unexpected argument %s" % op)

    # static method using for..in and enumerate, with tuple comprehension
    def walk_list(lst):
        for (i,strg) in lst.iterate():
            print "index: %d strg: %s\n" % (i, strg)
 
    # list comprehension, generating linQ output.
    def apply_map(mapfn, filterfn):
        return [mapfn(n) for n in lst if filterfn]
```
Translates to:
```C#
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
```

## Roadmap

The next big items on the list are:
* Take the types that are inferred and apply them to the code to get away from everything being `object`.
* Place local variable declarations at the statement that dominates all definitions of said variables.
