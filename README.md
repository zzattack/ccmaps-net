= CNCMaps Renderer

* http://github.com/zzattack/ccmaps-net

This project provides a tool to render battle maps of the most popular Westwood RTS games Red Alert 2 
and Tiberian Sun, including their expansions Yuri's Revenge and FireStorm.

== Status
The project has reached maturity and will know no big feature additions, but bugs might be fixed if reported.
I am toying with the idea of building a webservice out of the tool, though, but that would probably
appear under a different project

== Installation
Installation should be as simple as running the installer or extracting the latest zip release.
The NSIS installer cannot be used on non-Windows OS but the files inside the zip-archive run fine
under Mono.
The program requires several .mix files from the original games. These include:
* ra2.mix
* language.mix
* theme.mix


== Development
Working on CNCMaps should be as easy as checking out the source. All dependencies, on Windows, are provided
within the source tree. On linux you need the libmesaos package which may be available by your package manager.
Alternatively you can built it from http://www.mesa3d.org/download.html

Previous code repositories can be found at 
* http://code.google.com/p/ccmaps (old)
* http://sf.net/projects/ccmaps (even older)
which include a fully functional but not bug-free C++ version of this program.
These locations are no longer maintained.

== Contact
I am the main and only contributor to this project. You can contact me at frank@zzattack.org.

== License
(The MIT License)

Copyright (c) 2007-2012 Frank Razenberg

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
