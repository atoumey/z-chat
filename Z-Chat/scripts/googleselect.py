import clr
clr.AddReference('PresentationFramework')
import System
from ZChat import *
from System.Diagnostics import *
from System.Windows import *
from System.Windows.Controls import *
from System.Windows.Input import *
from System.Collections.Generic import *

def info():
	return "google search", "0.1", "Alex", "Highlight text, right click and choose google"

def googleSearch(menuItem, args):
	#ApplicationCommands.Copy.Execute(None, menuItem.Tag)
  Process.Start(ProcessStartInfo("http://www.google.com/search?q=" + menuItem.Tag.Selection.Text))
	
def createContextMenu(window):
	window.Document.ContextMenu = ContextMenu()
	items = List[MenuItem]()
	
	item = MenuItem()
	item.Header = "Google"
	item.Click += googleSearch
	item.Tag = window.DocumentScrollViewer
	items.Add(item)
	
	copyItem = MenuItem()
	copyItem.Header = "Copy"
	copyItem.Command = ApplicationCommands.Copy
	items.Add(copyItem)
	
	selectAllItem = MenuItem()
	selectAllItem.Header = "Select All"
	selectAllItem.Command = ApplicationCommands.SelectAll
	items.Add(selectAllItem)
	
	window.Document.ContextMenu.ItemsSource = items
	
def windowsChanged(coll, args):
	if args.NewItems != None:
		for window in args.NewItems:
			createContextMenu(window)
			
	if args.OldItems != None:
		for window in args.OldItems:
			window.Document.ContextMenu.Items[0].Click -= googleSearch

zchat.channelWindows.CollectionChanged += windowsChanged
zchat.queryWindows.CollectionChanged += windowsChanged

for window in zchat.channelWindows:
	createContextMenu(window)
  
for window in zchat.queryWindows:
  createContextMenu(window)