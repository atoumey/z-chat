import System

def info():
  return "chat logging script", "0.1", "Alex", "Logs channel messages to a file with the channel name."

def logChanMsg(sender, args):
  with open(args.Data.Channel + '.txt', 'a') as f:
    f.write(System.DateTime.Now.ToString() + ' <' + args.Data.Nick + '> ' + args.Data.Message + '\n')
  
def logSentMsg(type, target, message):
  with open(target + '.txt', 'a') as f:
    f.write(System.DateTime.Now.ToString() + ' <' + zchat.IRC.Nickname + '> ' + message + '\n')
  
zchat.IRC.OnChannelMessage += logChanMsg
zchat.OnSentMessage += logSentMsg