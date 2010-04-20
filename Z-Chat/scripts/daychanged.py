import System

def info():
  return "Day Changed Notification", "0.9", "Alex", "Prints a day changed notification to the main window at midnight."
  
def dayElapsed(sender, args):
  t.Interval = 86400000.0
  zchat.MainOutputWindow.Output("Day changed to " + System.DateTime.Today.ToShortDateString())
  
t = System.Timers.Timer()
t.Interval = (System.DateTime.Today + System.TimeSpan.FromDays(1) - System.DateTime.Now).TotalMilliseconds
t.Elapsed += dayElapsed
t.Start()