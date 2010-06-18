import System
import clr
clr.AddReference('System.Xml.Linq')
clr.AddReference('Meebey.SmartIrc4net')
import Meebey
import System.Xml.Linq
from ZChat import *

userName = "stati0n"

def info():
	return "last.fm now playing", "0.1", "Alex", "displays your currently playing song from last.fm"

def downloadComplete(sender, e):
  target = ""
  
  if str(e.UserState.GetType()) == "ZChat.ChannelWindow":
    target = e.UserState.Channel
  else:
    target = e.UserState.QueriedUser
  
  doc = System.Xml.Linq.XDocument.Parse(e.Result)
  for track in doc.Descendants("track"):
    action = "is listening to " + track.Element("name").Value + " by " + track.Element("artist").Value
    zchat.IRC.SendMessage(Meebey.SmartIrc4net.SendType.Action, target, action)
    e.UserState.Output([ColorTextPair(zchat.Options.TextFore, "* "), ColorTextPair(zchat.Options.TextFore, zchat.IRC.Nickname)], [ColorTextPair(zchat.Options.TextFore, action)])
    break

def input(window, target, inputText, e):
  if (inputText.StartsWith("/np")):
    client = System.Net.WebClient()
    client.Encoding = System.Text.Encoding.UTF8
    client.DownloadStringCompleted += downloadComplete
    client.DownloadStringAsync(System.Uri("http://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user=" + userName + "&api_key=638e9e076d239d8202be0387769d1da9&limit=1"), window)
    e.Handled = True

zchat.Input += input