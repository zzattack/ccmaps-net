CNCMaps Renderer
================
Project page:

* http://github.com/zzattack/ccmaps-net

Download:
* https://spysat.cc/downloads/

This project provides a tool to render battle maps of the most popular Westwood RTS games Red Alert 2 and Tiberian Sun, including their expansions Yuri's Revenge and Firestorm.

Status
------
The project has reached maturity and will know no big feature additions, but bugs might be fixed if reported. A map portal built around this renderer runs at https://spysat.cc, where maps can be uploaded, rendered and browsed.

Installation
------------
Run the installer or extract the latest zip release. Releases are published self-contained, so no separate .NET runtime install is needed.
The program requires several .mix files from the original games. These include:

* ra2.mix
* language.mix
* theme.mix
(or their TS equivalents)

If the original game is installed the location of these files will be found in the registry, otherwise you can specify their location on the command line.

Voxel rendering is performed by a built-in software rasterizer, so no GPU, OpenGL drivers or other graphics dependencies are required. This maximizes compatibility on systems without capable GPU drivers, such as remote desktop sessions or headless servers.

Usage
-----
Instructions on the command line application can be found by invoking `cncmaps -h`. Alternatively the CNCMaps GUI program can be used which contains descriptions of every option available and interactively updates the corresponding command line.

Development
-----------
Working on CNCMaps requires the .NET 10 SDK (or newer). Check out the source and run `dotnet build CNCMaps.slnx`; NuGet restores the dependencies. The console renderer is cross-platform (`dotnet publish CNCMaps.Renderer -r linux-x64 --self-contained` for Linux); the GUI targets Windows.

Older code repositories which contain a fully functional but not bug-free C++ version of this program can be found at 

* http://code.google.com/p/ccmaps (old)
* http://sf.net/projects/ccmaps (even older)

The project is no longer maintained at either of these locations.

Contact
-------
I am the main and only contributor to this project but am very willing to accept patches or collaborate on implementing support of new features or fixes. You can contact me at frank@zzattack.org.

Acknowledgments
---------------
Special thanks go out to authors of several modding tools that have greatly benefited the community as well as development of this program:

* Olaf van der Spek for XCC and XWIS
* The OpenRA project
* All contributors to [ModEnc](http://modenc.renegadeprojects.com), especially the people from \#renproj@Freenode

License
-------
The license below applies only to those parts of the program that do not contain
a conflicting license placed at the top of the source file. These include parts
from the [OpenRA](http://github.com/OpenRA/OpenRA/) and [XCC](https://code.google.com/p/xcc/) projects,
which are licenced under the GPL v3.

(The MIT License)

Copyright (c) 2007-2026 Frank Razenberg

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the 'Software'), to deal in
the Software without restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the
Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED 'AS IS', WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
