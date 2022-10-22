# pytocs HOWTO

This documents explains how to use `pytocs` to translate Python code to C#. `Pytocs` exists as command-line application and as a graphical user interface (GUI). In order to use `pytocs` you need to understand how command line programs work (e.g. understading what the `PATH` environment variable is), and some familiary with programming. In particular you will need to know Python, C#, and understand what the .NET Framework and .NET Core are.

## Getting pytocs
You can either download either an official release, a continuous integration (CI) build, or compile `pytocs` yourself.

### Dowloading pre-built software
`pytocs` has its official releases posted at https://github.com/uxmal/pytocs/releases. Download the `zip`-archive to a computer, unpack it with an appropriate unzip tool, and copy the unpacked files somewhere on your `PATH`. Alternatively, extend your `PATH` environment variable to include the directory you unpacked the files to.

### Downloading a CI build
The `pytocs` project uses AppVeyor as its CI provider: it can be found at https://ci.appveyor.com/project/uxmal/pytocs. Every time a change set is pushed to the GitHub git repository, AppVeyor clones it and rebuilds the project. The resulting outputs are cached in what AppVeyor calls `Artifacts`. The most recent build artifacts can be downloaded from https://ci.appveyor.com/project/uxmal/pytocs/build/artifacts. The CI build artifacts are four ZIP files, whose exact names will be different each time AppVeyor rebuilds:
```
bin\pytocs-cli-net6.0-2.0.0.0-4f21223b4c.zip
bin\pytocs-gui-net6.0-1.0.0-4f21223b4c.zip
```
The ZIP files are named by client type, GUI or command line interface. Download the appropriate ZIP file, unpack it with an appropriate unzip tool, and copy the unpacked files somewhere on your `PATH`. Alternatively, extend your `PATH` environment variable to include the directory you unpacked the files to.

### Compiling from the command line
To compile pytocs, you will need to have `git` and either Visual Studio or the .NET SDK installed. To obtain the `pytocs` source, clone the git repository on GitHub from the command line:
```
c:\projects> git clone https://github.com/uxmal/pytocs
```
Then compile the solution, which is located in the `src` directory. If you prefer `msbuild`:
```
c:\projects> cd pytocs\src
c:\projects\pytocs\src> msbuild
```
If you prefer building with the .NET SDK, use the `dotnet build` command:
```
c:\projects> cd pytocs\src
c:\projects\pytocs\src> dotnet build
```
The compiled executable will be located in an appropriate subdirectory of `src/bin`. 

### Compiling with Visual Studio
If you use Visual Studio you can clone the `pytocs` solution from https://github.com/uxmal/pytocs inside the Visual Studio GUI. After cloning, open the Visual Studio solution file (src/pytocs.sln). Use the Visual Studio `Build` command to compile the project. 

## Running pytocs

Before running `pytocs`, you need one or more Python source files, perhaps located in a directory `c:\example\projects`:
```
c:\example\projects> dir
 Volume in drive C is HAXX
 Volume Serial Number is 4242-4242

 Directory of c:\example\projects

02/18/2019  12:33 AM    <DIR>          .
02/18/2019  12:33 AM    <DIR>          ..
02/18/2019  12:32 AM                29 test.py
02/18/2019  12:33 AM                31 test2.py
```

### Running pytocs as a command line tool
The command line tool currently supports the following modes of operation.

Translate a single Python file:
```
c:\example\projects> pytocs test.py
c:\example\projects> dir test.py*
 Volume in drive C is HAXX
 Volume Serial Number is 4242-4242

 Directory of c:\example\projects

02/18/2019  12:32 AM                29 test.py
02/18/2019  12:57 AM               115 test.py.cs
               2 File(s)            144 bytes
```
Notice how the translated C# file is written into the same directory as the Python file.

Translate a directory of Python files (recursively):
```
c:\example\projects> pytocs -r c:\example\projects
c:\example\projects> dir *.py*
 Volume in drive C is HAXX
 Volume Serial Number is 4242-4242

 Directory of c:\example\projects

02/18/2019  12:32 AM                29 test.py
02/18/2019  12:57 AM               115 test.py.cs
02/18/2019  12:33 AM                31 test2.py
02/18/2019  12:33 AM               110 test2.py.cs
               4 File(s)            285 bytes
```
The `-r` (recursive) flag instructs `pytocs` to treat all files in the specified directory as well as any subdirectories.

## How to use Pytocs
Since Python and C# have fundamental differences that make it impossible to provide a fully automatic translation service, human post processing will likely always be needed to "massage" the code `pytocs` outputs into compileable C# code. When the source Python is under active development or maintenance, any changes to the output of `pytocs` will be overwritten next time the tool is run. Thus it becomes important to use a workflow to track the upstream changes and apply them to the translated C# code.

### Suggested git workflow
Assume there is a git repository containing Python source code, cloned on your computer. (The following examples assume you're using `git` as your source control system; similar workflows can be used for Mercurial, SVN, or any other SCC that supports branching). Assume further that the Python code is in the `master` branch; your repo may have different naming conventions.

```
master 
 +-- foo.py
```

Create a branch off `master` called `pytocs`. This branch will be used only for translation from Python to C#; no manual conversion should be carried out in the `pytocs` branch.
```
master
+-- foo.py
pytocs
+-- foo.py
```
Now run `pytocs` on the file(s) in the `pytocs` branch. This generates C# file(s). Commit the C#
files to `pytocs` branch.
```
master
+-- foo.py
pytocs
+-- foo.py
    foo.py.cs
```
Unfortunately, the file `foo.py.cs` will likely not compile successfully. We now create another branch `cs-port` off `pytocs`. In this branch the user can modify the C# files so that they compile correctly.
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
Suppose now the upstream modifies the original Python file. You can checkout the `pytocs` branch and pull the changes directly into that branch. Then run `pytocs` in that branch. The new C# code simply overwrites the old code. Commit the changes in the `pytocs` branch:
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
Finally, merge the `pytocs` branch into the `cs-port`. This may cause merge conflicts depending on the upstream changes. You will need to fix these before committing the merged code:
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
In general the flow goes as follows: the `master` branch keeps changing, being mutated by Python users who likely know nothing about C#. The `pytocs` branch tracks the `master` branch blindly. In fact, you could setup a `git` hook to automatically run `pytocs` after each commit to `master`. Changes to `pytocs` may have merge conflicts with `cs-port`, so such merges should be done judiciously at the discretion of a programmer.
