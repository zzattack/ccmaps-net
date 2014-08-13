alias mapsDir return X:\ra2maps\Firestorm\Official Multiplayer\
;alias mapsDir return X:\ra2maps\Red Alert 2\
alias renderProg return C:\Users\Frank\Desktop\workspace\ccmaps-net\CNCMaps.Renderer\bin\Debug\CNCMaps.Renderer.exe

alias create_map_images {
  if ($window(@renderQueue)) { clear $v1 }
  else { window -d @renderQueue }
  noop $findfile($mapsDir, *.map;&.mmx;*.mpr;*.yrm;*.yro, 0, 9, aline 5 @renderQueue $gettok($1-,$calc(1 + $numtok($mapsDir, $asc(\))) -,$asc(\)))

  filter -cpzwwt 1 1 @renderQueue @renderQueue

  %renderIdx = 0
  %renderLength = $findfilen
  %renderSuccess = 0
  %renderFail = 0
  %stop = $false
  if ($hget(renderCommands))   hfree $v1

  renderNext
}

alias renderNext {
  if (%renderIdx >= %renderLength) {
    aline -s 3 @renderQueue Finished rendering! Success: %renderSuccess $+ , failure: %renderFail ( $+ $ceil($calc(%renderSuccess / %renderLength * 100)) $+ $(%,0) $+ )
  }
  else if (%stop) {
    aline -s 6 @renderQueue Aborted rendering! Success: %renderSuccess $+ , failure: %renderFail $+ , skipped: $calc(%renderLength - %renderIdx)  
  }
  else {
    inc %renderIdx   
    var %line = $line(@renderQueue, %renderIdx)
    var %mapname = $gettok(%line, -2, $asc(\))
    %cmd = -i $qt($mapsDir $+ %line) -z +(800,0) -j -p $getOptions($mapsDir $+ %line) -o $qt(%mapname)
    hadd -m renderCommands %renderIdx %cmd
    cline 7 @renderQueue %renderIdx
    sline @renderQueue $iif($calc(%renderIdx + 20) > %renderLength, %renderLength, $calc(%renderIdx + 20))

    execWithCallback renderCallback $renderProg %cmd
  }
}

alias renderCallback {
  var %result = $com($1).result
  .comclose $1
  if (%result == 0) {
    inc %renderSuccess
    cline 3 @renderQueue %renderIdx
  }

  else {
    inc %renderFail
    cline 4 @renderQueue %renderIdx
  }

  renderNext
}

alias stop { %stop = $true }

alias getOptions {
  if (\Twisted Insurrection\ isin $1-) return -M $qt(C:\Westwood\Twisted Insurrection\modconfig.xml)
  else if (\DTA\ isin $1-) return -M $qt(C:\Westwood\DTA\modconfig.xml)
  else if (\Yuri's Revenge\ isin $1-) return -Y
  else if (\Red Alert 2\ isin $1-) return -y
  else if (\Firestorm\ isin $1-) return -T
  else if (\Tiberian Sun\ isin $1-) return -t
}

alias execWithCallback {
  var %wsh = $+(wsh.,$ticks,$rand(0,99999))
  .comopen %wsh wscript.shell
  noop $comcall(%wsh, $1, run, 1, bstr*, $2-, int, 0, bool, 1))
}

on ^*:hotlink:*:@renderQueue:{
  var %idx = $gettok($hotlinepos, 2, 32)
  if ($hget(renderCommands, %idx)) return
  halt
}

on *:hotlink:*:@renderQueue:{
  var %idx = $gettok($hotlinepos, 2, 32)
  var %cmd = $hget(renderCommands, %idx)
  if ($input(Copy command $qt(%cmd) to clipboard?,yvs,@renderQueue,Copy command?) == $yes) {
    clipboard  - %cmd
  }
}
