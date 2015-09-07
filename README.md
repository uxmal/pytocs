# pytocs
Converts Python source to C#

pytocs is a hobby project I wrote to convert Python source code to C#. 
I've uploaded it here in case someone finds it useful.

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

