import System
from ZChat import *

def info():
	return "test script", "0.1", "Alex", "Does nothing, just a test."

def handler(irc, args):
	zchat.MainOutputWindow.Output("connected!")
	
zchat.IRC.OnConnected += handler