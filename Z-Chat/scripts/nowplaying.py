import System
from ZChat import *

userName = "stati0n"

def info():
	return "last.fm now playing", "0.1", "Alex", "displays your currently playing song from last.fm"

def input(window, target, inputText, e):
	if (inputText.StartsWith("/np")):
		zchat.MainOutputWindow.Output("fetch song from last.fm!")
		e.Handled = True

zchat.Input += input