param([ValidateSet('listen','speak')][string]$Mode)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Speech
if($Mode -eq 'listen'){
  $culture=[Globalization.CultureInfo]::GetCultureInfo('es-ES')
  $r=New-Object System.Speech.Recognition.SpeechRecognitionEngine($culture)
  try{
    $r.LoadGrammar((New-Object System.Speech.Recognition.DictationGrammar))
    $r.SetInputToDefaultAudioDevice()
    $r.InitialSilenceTimeout=[TimeSpan]::FromSeconds(8)
    $r.EndSilenceTimeout=[TimeSpan]::FromMilliseconds(850)
    $r.EndSilenceTimeoutAmbiguous=[TimeSpan]::FromMilliseconds(1100)
    $result=$r.Recognize([TimeSpan]::FromSeconds(10))
    if($result -and $result.Text){
      [Console]::WriteLine([Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($result.Text)))
    }
  }finally{$r.Dispose()}
}else{
  $s=New-Object System.Speech.Synthesis.SpeechSynthesizer
  try{
    $s.SelectVoice('Microsoft Helena Desktop')
    $s.Rate=0
    $s.Volume=85
    $encoded=[Console]::In.ReadToEnd()
    $message=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($encoded.Trim()))
    $s.Speak($message)
  }finally{$s.Dispose()}
}
