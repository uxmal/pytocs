pytocs HOWTO
==
Pytocs is a command line tool, currently supporting the following modes of operation.

Translate a single Python file:
```
pytocs <filename.py>
```
Translate a directory of Python files (recursively):
```
pytocs -r <directory-name>
pytocs -r .
```
The latter translates the current directory.


Typical workflow
==
Since Python and C# have fundamental differences that make it very difficult if not impossible to 
provide a fully automatic translation service, human post processing will likely always be needed to
massage the output code into compileable C# code. When the source Python is under active development
or maintenance, it becomes important to use a workflow to track the upstream changes and apply them
to the translated C# code.

Assume there is a git repository containing Python source code, cloned on your computer. (The following 
examples assume you're using `git` as your source control system). Assume further that the Python code
is in the `master` branch.

```
master 
 +-- foo.py
```

Create a branch off `master` called `pytocs`. This branch will be used only for translation from
Python to C#; no manual conversion should be carried out in the `pytocs` branch.
```
master
+-- foo.py
pytocs
+-- foo.py
```
Now run `pytocs` on the file(s) in the `pytocs` branch. This generates C# file(s). Commit the C#
files.
```
master
+-- foo.py
pytocs
+-- foo.py
    foo.py.cs
```
Unfortunately, the file `foo.py.cs` will likely not compile successfully. We now create another
branch `cs-port` off `pytocs`. In this branch the user can modify the C# files so that they 
compile correctly.
```
master
+-- foo.py
pytocs
+-- foo.py
    foo.py.cs
cs-port
+-- foo.py
    foo.py.cs   <- may be modified to make it compileable.
```
Suppose now the upstream modifies the original Python file. You can checkout the `pytocs` branch
and pull the changes directly into that branch. Then run `pytocs` in that branch. The new C# code
simply overwrites the old code. Commit the changes in the `pytocs` branch:
```
master
+-- foo.py
pytocs
+-- foo.py (modified)
    foo.py.cs (regenerated)
cs-port
+-- foo.py
    foo.py.cs
```
Finally, merge the `pytocs` branch into the `cs-port`. This may cause merge conflicts depending
on the upstream changes. You will need to fix these before committing the merged code:
```
master
+-- foo.py
pytocs
+-- foo.py (modified)
    foo.py.cs (regenerated)
cs-port
+-- foo.py    (modified)
    foo.py.cs (regenerated / merged)
```
In general the flow goes like:
```
master => pytocs => cs-port
```
