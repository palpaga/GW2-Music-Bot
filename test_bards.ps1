$client = New-Object System.Net.WebClient
$client.Headers.Add('User-Agent', 'Mozilla/5.0')
$json = $client.DownloadString('http://api.bardsguild.life/?key=0Tk-seyqLFwn5qCH2YzrYA&find=zelda')
Write-Host $json.Substring(0, 500)
