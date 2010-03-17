import clr
import System

def info():
	return "timer test", "0.1", "Alex", "prints a number every few seconds"

def WaitNPrint():
  System.Threading.Thread.CurrentThread.Join(2000)
  print 3
  
while 1==1:
  WaitNPrint()