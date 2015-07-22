#if DEBUG
using Pytocs.CodeModel;
using Pytocs.Syntax;
using Pytocs.Translate;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Acceptance
{
    [TestFixture]
    public class ParserAcceptanceTests
    {
        private string XlatModule(string pyModule)
        {
            var rdr = new StringReader(pyModule);
            var lex = new Syntax.Lexer("foo.py", rdr);
            var par = new Syntax.Parser("foo.py", lex);
            var stm = par.Parse(); ;
            var unt = new CodeCompileUnit();
            var gen = new CodeGenerator(unt, "test", "testModule");
            var xlt = new ModuleTranslator(gen);
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

        [Test]
        public void Accept1()
        {
            Assert.IsTrue(Object.Equals(null, null));
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
		# Ask user input
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

	#menuitem = gtk.MenuItem()
	#menuitem.show()
	#menu.prepend(menuitem)

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
                object names;
                object refactoring;
                names = this.refactoring_factory.refactorings.keys();
                names.sort();
                foreach (var name in names) {
                    refactoring = this.refactoring_factory.refactorings[name];
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
                object selection;
                object term;
                term = this.model.get_term();
                selection = this.model.get_selection();
                if (refactoring.applicable(term, selection)) {
                    menuitem.set_state(gtk.STATE_NORMAL);
                } else {
                    menuitem.set_state(gtk.STATE_INSENSITIVE);
                }
            }
            
            public virtual object on_menuitem_activate(object menuitem, object refactoring) {
                object args;
                // Ask user input
                args = refactoring.input(this.model.get_term(), this.model.get_selection());
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
                object menuitem;
                this.model = model;
                foreach (var viewfactory in this.viewfactories) {
                    menuitem = gtk.MenuItem(viewfactory.get_name());
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
            object menu;
            object menuitem;
            object viewmenu;
            menu = gtk.Menu();
            //menuitem = gtk.MenuItem()
            //menuitem.show()
            //menu.prepend(menuitem)
            menuitem = gtk.MenuItem(""View"");
            viewmenu = ViewMenu(model);
            menuitem.set_submenu(viewmenu);
            menuitem.show();
            menu.prepend(menuitem);
            return menu;
        }
    }
}
";
            Console.WriteLine(s);
            Assert.AreEqual(sExp, s);
        }
    }
}
#endif