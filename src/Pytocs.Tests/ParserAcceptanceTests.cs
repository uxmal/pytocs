#region License
//  Copyright 2015-2021 John Källén
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
#endregion

#if DEBUG
using Pytocs.Core.CodeModel;
using Pytocs.Core.Syntax;
using Pytocs.Core.Translate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Pytocs.Core.TypeInference;
using Pytocs.Core.Types;
using Xunit;
using System.Diagnostics;

namespace Pytocs.Acceptance
{
    public class ParserAcceptanceTests
    {
        private static readonly string nl = Environment.NewLine;

        public ParserAcceptanceTests()
        {
        }

        private string XlatModule(string pyModule)
        {
            var rdr = new StringReader(pyModule);
            var lex = new Lexer("foo.py", rdr);
            var par = new Parser("foo.py", lex);
            var stm = par.Parse();
            var unt = new CodeCompileUnit();
            var gen = new CodeGenerator(unt, "test", "testModule");
            var types = new TypeReferenceTranslator(new Dictionary<Node, DataType>());
            var xlt = new ModuleTranslator(types, gen);
            xlt.Translate(stm);

            var pvd = new CSharpCodeProvider();
            var writer = new StringWriter();
            pvd.GenerateCodeFromCompileUnit(
                unt,
                writer,
            new CodeGeneratorOptions
            {
            });
            return writer.ToString();
        }

        [Fact(DisplayName = nameof(Accept1))]
        public void Accept1()
        {
            var s = XlatModule(
        @"'''Refactor and view menus.'''

import gtk

import refactoring

from ui import inputter


class RefactorMenu(gtk.Menu):

	refactoring_factory = refactoring.Factory()

	def __init__(self, model):
		gtk.Menu.__init__(self)
		self.model = model
		self.populate()

	def populate(self):
		names = self.refactoring_factory.refactorings.keys()
		names.sort()
		for name in names:
			refactoring = self.refactoring_factory.refactorings[name]
			menuitem = gtk.MenuItem(refactoring.name())
			menuitem.connect(""realize"", self.on_menuitem_realize, refactoring)
			menuitem.connect(""activate"", self.on_menuitem_activate, refactoring)
			menuitem.show()
			self.append(menuitem)

		menuitem = gtk.SeparatorMenuItem()
		menuitem.show()
		self.append(menuitem)

		menuitem = gtk.MenuItem(""Reload All Refactorings"")
		menuitem.connect(""activate"", self.on_reload_activate)
		menuitem.show()
		self.append(menuitem)

	def on_menuitem_realize(self, menuitem, refactoring):
		term = self.model.get_term()
		selection = self.model.get_selection()
		if refactoring.applicable(term, selection):
			menuitem.set_state(gtk.STATE_NORMAL)
		else:
			menuitem.set_state(gtk.STATE_INSENSITIVE)

	def on_menuitem_activate(self, menuitem, refactoring):
" +
"		# Ask user input" + @"
		args = refactoring.input(
			self.model.get_term(),
			self.model.get_selection(),
		)

		self.model.apply_refactoring(refactoring, args)

	def on_reload_activate(self, dummy):
		print ""Reloading all refactorings...""
		for menuitem in self.get_children():
			self.remove(menuitem)
		self.refactoring_factory.load()
		self.populate()


import termview
import dotview


class ViewMenu(gtk.Menu):

	viewfactories = [
		termview.TermViewFactory(),
		dotview.CfgViewFactory(),
	]

	def __init__(self, model):
		gtk.Menu.__init__(self)
		self.model = model

		for viewfactory in self.viewfactories:
			menuitem = gtk.MenuItem(viewfactory.get_name())
			menuitem.connect(""realize"", self.on_menuitem_realize, viewfactory)
			menuitem.connect(""activate"", self.on_menuitem_activate, viewfactory)
			menuitem.show()
			self.append(menuitem)

	def on_menuitem_realize(self, menuitem, viewfactory):
		if viewfactory.can_create(self.model):
			menuitem.set_state(gtk.STATE_NORMAL)
		else:
			menuitem.set_state(gtk.STATE_INSENSITIVE)

	def on_menuitem_activate(self, menuitem, viewfactory):
		viewfactory.create(self.model)


def PopupMenu(model):

	menu = gtk.Menu()

" +
"	#menuitem = gtk.MenuItem()" + nl + 
"	#menuitem.show()" + nl + 
"	#menu.prepend(menuitem)" + nl + 
@"
	menuitem = gtk.MenuItem(""View"")
	viewmenu = ViewMenu(model)
	menuitem.set_submenu(viewmenu)
	menuitem.show()
	menu.prepend(menuitem)

	return menu
");

            var sExp =
@"// Refactor and view menus.
namespace test {
    
    using gtk;
    
    using refactoring;
    
    using inputter = ui.inputter;
    
    using termview;
    
    using dotview;
    
    using System.Collections.Generic;
    
    public static class testModule {
        
        public class RefactorMenu
            : gtk.Menu {
            
            public object refactoring_factory = refactoring.Factory();
            
            public RefactorMenu(object model) {
                this.model = model;
                this.populate();
            }
            
            public virtual object populate() {
                object menuitem;
                var names = this.refactoring_factory.refactorings.keys();
                names.sort();
                foreach (var name in names) {
                    var refactoring = this.refactoring_factory.refactorings[name];
                    menuitem = gtk.MenuItem(refactoring.name());
                    menuitem.connect(""realize"", this.on_menuitem_realize, refactoring);
                    menuitem.connect(""activate"", this.on_menuitem_activate, refactoring);
                    menuitem.show();
                    this.append(menuitem);
                }
                menuitem = gtk.SeparatorMenuItem();
                menuitem.show();
                this.append(menuitem);
                menuitem = gtk.MenuItem(""Reload All Refactorings"");
                menuitem.connect(""activate"", this.on_reload_activate);
                menuitem.show();
                this.append(menuitem);
            }
            
            public virtual object on_menuitem_realize(object menuitem, object refactoring) {
                var term = this.model.get_term();
                var selection = this.model.get_selection();
                if (refactoring.applicable(term, selection)) {
                    menuitem.set_state(gtk.STATE_NORMAL);
                } else {
                    menuitem.set_state(gtk.STATE_INSENSITIVE);
                }
            }
            
            public virtual object on_menuitem_activate(object menuitem, object refactoring) {
                // Ask user input
                var args = refactoring.input(this.model.get_term(), this.model.get_selection());
                this.model.apply_refactoring(refactoring, args);
            }
            
            public virtual object on_reload_activate(object dummy) {
                Console.WriteLine(""Reloading all refactorings..."");
                foreach (var menuitem in this.get_children()) {
                    this.remove(menuitem);
                }
                this.refactoring_factory.load();
                this.populate();
            }
        }
        
        public class ViewMenu
            : gtk.Menu {
            
            public object viewfactories = new List<object> {
                termview.TermViewFactory(),
                dotview.CfgViewFactory()
            };
            
            public ViewMenu(object model) {
                this.model = model;
                foreach (var viewfactory in this.viewfactories) {
                    var menuitem = gtk.MenuItem(viewfactory.get_name());
                    menuitem.connect(""realize"", this.on_menuitem_realize, viewfactory);
                    menuitem.connect(""activate"", this.on_menuitem_activate, viewfactory);
                    menuitem.show();
                    this.append(menuitem);
                }
            }
            
            public virtual object on_menuitem_realize(object menuitem, object viewfactory) {
                if (viewfactory.can_create(this.model)) {
                    menuitem.set_state(gtk.STATE_NORMAL);
                } else {
                    menuitem.set_state(gtk.STATE_INSENSITIVE);
                }
            }
            
            public virtual object on_menuitem_activate(object menuitem, object viewfactory) {
                viewfactory.create(this.model);
            }
        }
        
        public static object PopupMenu(object model) {
            var menu = gtk.Menu();
            //menuitem = gtk.MenuItem()
            //menuitem.show()
            //menu.prepend(menuitem)
            var menuitem = gtk.MenuItem(""View"");
            var viewmenu = ViewMenu(model);
            menuitem.set_submenu(viewmenu);
            menuitem.show();
            menu.prepend(menuitem);
            return menu;
        }
    }
}
";
            Console.WriteLine(s);
            Assert.Equal(sExp, s);
        }

        [Fact(DisplayName = nameof(Accept2))]
        public void Accept2()
        {
            var s = XlatModule(
@"
class foo:
    def widen(self, others):
        return self._combine(others)

" + @"### HEAP MANAGEMENT

    def get_max_sinkhole(self, length):
        '''
        Find a sinkhole which is large enough to support `length` bytes.

        This uses first - fit.The first sinkhole(ordered in descending order by their address)
        which can hold `length` bytes is chosen.If there are more than `length` bytes in the
        sinkhole, a new sinkhole is created representing the remaining bytes while the old
        sinkhole is removed.
        '''

        ordered_sinks = sorted(list(self.sinkholes), key =operator.itemgetter(0), reverse = True)

");
            var sExp =
            #region Expected
@"namespace test {
    
    using System.Linq;
    
    using System.Collections.Generic;
    
    public static class testModule {
        
        public class foo {
            
            public virtual object widen(object others) {
                return this._combine(others);
                //## HEAP MANAGEMENT
            }
            
            // 
            //         Find a sinkhole which is large enough to support `length` bytes.
            // 
            //         This uses first - fit.The first sinkhole(ordered in descending order by their address)
            //         which can hold `length` bytes is chosen.If there are more than `length` bytes in the
            //         sinkhole, a new sinkhole is created representing the remaining bytes while the old
            //         sinkhole is removed.
            //         
            public virtual object get_max_sinkhole(object length) {
                var ordered_sinks = this.sinkholes.ToList().OrderByDescending(@operator.itemgetter(0)).ToList();
            }
        }
    }
}
";
            #endregion

            Assert.Equal(sExp, s);
        }
    }
}
#endif